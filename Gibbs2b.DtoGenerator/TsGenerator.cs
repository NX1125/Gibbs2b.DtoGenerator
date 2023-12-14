using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Gibbs2b.DtoGenerator.Model;

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
            foreach (var e in _solution.Projects
                         .SelectMany(p => p.Enums)
                         .OrderBy(p => p.Name.CapitalCase))
            {
                StartFiles(project.Paths.Select(path => Path.Combine(path, $"{e.Name.KebabCase}.enum.gen.ts")));

                WriteLine("// <auto-generated />");

                WriteLine();
                WriteLine($"enum {e.Name} {{");

                List<int> values;
                try
                {
                    values = e.Type.GetEnumValues()
                        .Cast<int>()
                        .ToList();
                }
                catch (InvalidCastException)
                {
                    throw new InvalidOperationException(
                        $"Enum {e.Name} has values that are not integers. " +
                        $"Please use explicit values for all enum values. {e.Type.FullName}");
                }

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
                WriteLine();
                WriteLine($"export default {e.Name}");
                CommitFiles();

                if (!e.TsArrayEnabled) continue;

                StartFiles(project.Paths.Select(path => Path.Combine(path, $"{e.Name.KebabCase}-names.enum.gen.ts")));

                WriteLine("// <auto-generated />");

                WriteLine($"import {e.Name} from './{e.Name.KebabCase}.enum.gen'");
                WriteLine();
                WriteLine($"const {e.Name}Names: {e.Name}[] = [");
                IncreaseIndent();
                foreach (var pair in pairs)
                {
                    WriteLine($"{e.Name}.{pair.Key},");
                }

                DecreaseIndent();
                WriteLine(']');
                WriteLine();
                WriteLine($"export default {e.Name}Names");

                CommitFiles();
                StartFiles(project.Paths.Select(path =>
                    Path.Combine(path, $"{e.Name.KebabCase}-name-set.enum.gen.ts")));

                WriteLine("// <auto-generated />");

                WriteLine($"import {e.Name} from './{e.Name.KebabCase}.enum.gen'");
                WriteLine($"import Names from './{e.Name.KebabCase}-names.enum.gen'");
                WriteLine();
                WriteLine($"const {e.Name}NameSet: Set<{e.Name}> = new Set(Names)");
                WriteLine($"export default {e.Name}NameSet");

                CommitFiles();
            }
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
                var imports = new Dictionary<string, ISet<string>>
                {
                    { "./model", new HashSet<string>() },
                };
                foreach (var model in dto.Models)
                {
                    foreach (var property in model.TsProperties)
                    {
                        var opaque = property.OpaqueModel;
                        if (opaque != null)
                        {
                            imports[opaque.ImportFrom].Add(opaque.Name);
                        }
                        else if (property.TypeNameType == TypeNameEnum.Enum &&
                                 property.Options.JsonIgnore != JsonIgnoreCondition.Always)
                        {
                            var name = new NameSpec(property.EnumName);
                            imports[$"./{name.KebabCase}.enum.gen"] = new HashSet<string> { property.EnumName };
                        }
                    }
                }

                foreach (var group in imports.OrderBy(g => g.Key))
                {
                    if (group.Value.Count <= 0)
                        continue;

                    if (group.Key.EndsWith(".enum.gen"))
                    {
                        WriteLine($"import type {group.Value.First()} from '{group.Key}'");
                        continue;
                    }

                    WriteLine("import {");
                    foreach (var i in group.Value.OrderBy(i => i.ToLower()))
                    {
                        WriteLine($"{i},");
                    }

                    WriteLine($"}} from '{group.Key}'");
                }

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

                        var opaque = property.OpaqueModel;

                        var type = opaque != null
                            ? opaque.Name
                            : property.TypeNameType switch
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
                                _ => throw new ArgumentOutOfRangeException(property.Name.CapitalCase,
                                    property.TypeNameType, null),
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
                            EnumerableType.Dictionary => $"{{ [key: string]: {type} }}",
                            _ => type,
                        };

                        var optional = "";

                        if (property.Options.IsNullable ||
                            property.Options.JsonIgnore is JsonIgnoreCondition.WhenWritingDefault
                                or JsonIgnoreCondition.WhenWritingNull)
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

        GenerateHandlers();
    }

    public void GenerateHandlers()
    {
        _logger.LogInformation("Generating typescript handlers ({} controllers)",
            _solution.Project.Controllers.Count);

        var project = _solution.Project;
        foreach (var ts in _solution.TypescriptProjects)
        {
            StartFiles(ts.Paths.Select(path => Path.Combine(path, "handlers.gen.ts")));

            // generate interface with all handlers
            WriteLine("// <auto-generated />");
            WriteLine();

            // import AxiosResponse
            WriteLine("import { AxiosResponse, AxiosRequestConfig } from 'axios'");

            // import each query and response
            foreach (var controller in project.Controllers)
            {
                if (controller.TypescriptProject != ts)
                    continue;

                foreach (var handler in controller.Handlers)
                {
                    var query = handler.Query;
                    var response = handler.Response;

                    if (query == null && response == null)
                        continue;

                    if (query?.Dto == response?.Dto)
                    {
                        WriteLine(
                            $"import {{ {query!.DtoName}, {response!.DtoName} }} from './{query.Dto.DtoName.KebabCase}.dto.gen'");
                    }
                    else
                    {
                        if (query != null)
                            WriteLine($"import {{ {query.DtoName} }} from './{query.Dto.DtoName.KebabCase}.dto.gen'");
                        if (response != null)
                            WriteLine(
                                $"import {{ {response.DtoName} }} from './{response.Dto.DtoName.KebabCase}.dto.gen'");
                    }
                }
            }

            // interface with all handlers
            WriteLine();
            WriteLine("export interface GeneratedAPI {");
            foreach (var controller in project.Controllers)
            {
                if (controller.TypescriptProject != ts)
                {
                    continue;
                }
                foreach (var handler in controller.Handlers)
                {
                    var query = handler.Query;
                    var response = handler.Response;

                    var queryName = query!.DtoName;
                    var responseName = response!.DtoName;

                    WriteLine($"{handler.Name.CamelCase}(request: {queryName}, signal?: AbortSignal, config?: AxiosRequestConfig): Promise<AxiosResponse<{responseName}>>");
                }
            }

            WriteLine("}");
            WriteLine();

            var groups = project.Controllers
                .Where(c => c.TypescriptProject == ts)
                .SelectMany(c => c.Handlers)
                .GroupBy(h => h.IsPost)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key,
                    g => g.ToList());

            if (!groups.ContainsKey(true))
                groups[true] = new List<HandlerSpec>();
            if (!groups.ContainsKey(false))
                groups[false] = new List<HandlerSpec>();

            // handler name to its route, grouped by method
            foreach (var group in groups)
            {
                var method = group.Key ? "POST" : "GET";
                WriteLine($"export const {method}Handlers: {{");
                WriteLine("[key in keyof GeneratedAPI]?: string");
                WriteLine("} = {");
                foreach (var handler in group.Value)
                {
                    WriteLine($"{handler.Name.CamelCase}: '{handler.Route}',");
                }

                WriteLine("}");
                WriteLine();
            }

            CommitFiles();
        }
    }
}