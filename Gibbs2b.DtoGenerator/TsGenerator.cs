using System.Text.Json.Serialization;
using Gibbs2b.DtoGenerator.Model;
using Microsoft.Extensions.Logging;

namespace Gibbs2b.DtoGenerator;

public class TsGenerator : AbstractGenerator
{
    private readonly SolutionSpec _solution;
    private readonly ILogger<TsGenerator> _logger;

    public TsGenerator(
        SolutionSpec solution,
        ILogger<TsGenerator> logger)
    {
        _solution = solution;
        _logger = logger;

        IndentStep = "  ";
    }

    public void Generate()
    {
        _logger.LogInformation("Generating enums");

        foreach (var project in _solution.TypescriptProjects)
        {
            var paths = project.Paths
                .Select(path => Path.Combine(path, "enum.gen.ts"));

            StartFiles(paths);

            WriteLine("// <auto-generated />");

            foreach (var e in _solution.Projects
                         .SelectMany(p => p.Enums)
                         .OrderBy(p => p.Name.CapitalCase))
            {
                WriteLine();
                WriteLine($"export enum {e.Name} {{");

                var values = e.Type.GetEnumValues()
                    .Cast<int>()
                    .ToList();

                IDictionary<string, string> pairs = e.IsJsonName
                    ? e.Type
                        .GetEnumNames()
                        .ToDictionary(v => v, v => $"'{v}'")
                    : e.Type
                        .GetEnumNames()
                        .Zip(values)
                        .ToDictionary(v => v.First, v => v.Second.ToString());

                foreach (var value in pairs)
                {
                    WriteLine($"{value.Key} = {value.Value},");
                }

                WriteLine('}');
                if (e.TsArrayEnabled)
                {
                    WriteLine();
                    WriteLine($"export const {e.Name}Names: {e.Name}[] = [");
                    IncreaseIndent();
                    foreach (var pair in pairs)
                    {
                        WriteLine($"{e.Name}.{pair.Key},");
                    }

                    DecreaseIndent();
                    WriteLine(']');
                    WriteLine();
                    WriteLine($"export const {e.Name}NameSet: Set<{e.Name}> = new Set({e.Name}Names)");
                }

                WriteLine();
            }

            CommitFiles();
        }

        _logger.LogInformation("Generating typescript DTOs");

        foreach (var project in _solution.Projects)
        {
            foreach (var dto in project.TsDto)
            {
                _logger.LogInformation("Generating {}", dto.TsPaths);
                StartFiles(dto.TsPaths);

                WriteLine("// <auto-generated />");
                WriteLine();
                WriteLine("import {");
                var enumImports = new HashSet<string>();
                foreach (var model in dto.Models)
                {
                    foreach (var property in model.Properties)
                    {
                        if (property.TypeNameType == TypeNameEnum.Enum && property.Options.JsonIgnore != JsonIgnoreCondition.Always)
                            enumImports.Add(property.EnumName);
                    }
                }

                foreach (var i in enumImports.OrderBy(i => i.ToLower()))
                {
                    WriteLine($"{i},");
                }

                WriteLine("} from './enum.gen'");
                WriteLine();

                foreach (var model in dto.Models)
                {
                    WriteLine($"export interface {model.DtoName} {{");

                    foreach (var property in model.TsProperties)
                    {
                        if (property.Options.JsonIgnore == JsonIgnoreCondition.Always)
                            continue;

                        if (property.Options.Obsolete)
                            WriteLine("/** @deprecated */");

                        var type = property.TypeNameType switch
                        {
                            TypeNameEnum.Int => "number",
                            TypeNameEnum.Long => "number",
                            TypeNameEnum.Float => "number",
                            TypeNameEnum.Double => "number",
                            TypeNameEnum.Decimal => "number",
                            TypeNameEnum.Bool => "boolean",
                            TypeNameEnum.String => "string",
                            TypeNameEnum.DateTime => "string",
                            TypeNameEnum.Guid => "string",
                            TypeNameEnum.Model => property.TsTypeModel!.DtoName,
                            TypeNameEnum.Enum => property.EnumName,
                            _ => throw new ArgumentOutOfRangeException(property.Name.CapitalCase, property.TypeNameType, null),
                        };

                        if (property.Options.IsNullableItem)
                            type = $"({type} | null | undefined)";

                        var suffix = string.Concat(Enumerable.Repeat("[]", property.Options.EnumerableDimension));

                        type = property.EnumerableType switch
                        {
                            EnumerableType.Enumerable => $"{type}{suffix}",
                            EnumerableType.Collection => $"{type}{suffix}",
                            EnumerableType.Array => $"{type}{suffix}",
                            EnumerableType.List => $"{type}{suffix}",
                            _ => type,
                        };

                        var optional = "";

                        if (property.Options.IsNullable ||
                            property.Options.JsonIgnore is JsonIgnoreCondition.WhenWritingDefault or JsonIgnoreCondition.WhenWritingNull)
                        {
                            type = $"{type} | null | undefined";
                            optional = "?";
                        }

                        switch (property.TypeNameType)
                        {
                            case TypeNameEnum.Bool:
                                if (model.NullableBool)
                                    optional = "?";
                                break;
                        }

                        WriteLine($"{property.Name.CamelCase}{optional}: {type}");
                    }

                    WriteLine('}');
                    WriteLine();
                }

                CommitFiles();
            }
        }
    }
}