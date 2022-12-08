using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Gibbs2b.DtoGenerator.Model;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Gibbs2b.DtoGenerator;

public class Generator
{
    private SolutionSpec _solution;
    private readonly TsGenerator _tsGenerator;
    private readonly ILogger<Generator> _logger;

    public Generator(SolutionSpec solution,
        TsGenerator tsGenerator,
        ILogger<Generator> logger)
    {
        _solution = solution;
        _tsGenerator = tsGenerator;
        _logger = logger;
    }

    public void Run(string? path, string[] args)
    {
        _solution.Path = path ?? Directory.GetCurrentDirectory();

        if (args.Contains("--cs"))
            GenerateCs();
        if (args.Contains("--py"))
            GeneratePy();
        if (args.Contains("--sql"))
            GenerateSql();
        if (args.Contains("--ts") || args.Contains("--typescript"))
            GenerateTs();
    }

    private void GenerateSql()
    {
    }

    private void GenerateCs()
    {
        GenerateDtoCs();
        GenerateContextCs();
    }

    private IEnumerable<NamespaceSpec> SortUsing(IEnumerable<NamespaceSpec> uses, NamespaceSpec current)
    {
        return uses
            .Distinct()
            .OrderBy(u => u.Parts[0].Equals(current.Parts[0])
                ? 3
                : u.Parts[0].CapitalCase switch
                {
                    "System" => 0,
                    "Microsoft" => 1,
                    _ => 2,
                })
            .ThenBy(u => u.Namespace);
    }

    private void GenerateDtoCs()
    {
    }

    private void GeneratePy()
    {
        foreach (var project in _solution.PythonProjects)
        {
            Directory.CreateDirectory(project.Value.Path);
            File.Create(Path.Combine(project.Value.Path, "__init__.py"));

            using StreamWriter writer = new(Path.Combine(project.Value.Path, "model_gen.py"));

            writer.WriteLine("import enum");

            foreach (var p in _solution.Projects)
            {
                foreach (var @enum in p.Enums)
                {
                    writer.WriteLine();
                    writer.WriteLine();
                    writer.WriteLine($"class {@enum.Name}(enum.Enum):");
                    foreach (var value in @enum.Values)
                    {
                        var name = value;

                        if (name == "None")
                            name = "None_";

                        writer.WriteLine($"    {name} = \"{value}\"");
                    }
                }
            }
        }

        foreach (var project in _solution.Projects)
        {
            foreach (var dto in project.Dto)
            {
                var relativePath = $"{dto.Name.SnakeCaseName}.py";
                foreach (var targetName in dto.Options.PythonProjects)
                {
                    var path = Path.Combine(_solution.PythonProjects[targetName].Path, relativePath);
                    _logger.LogDebug("Generating {}", path);
                    using StreamWriter writer = new(path);

                    writer.WriteLine("# <auto-generated />");
                    writer.WriteLine();

                    var imports = new HashSet<string> { "dataclasses" };

                    foreach (var model in dto.Models)
                    {
                        foreach (var property in model.Properties)
                        {
                            switch (property.Property.TypeNameType)
                            {
                                case TypeNameEnum.DateTime:
                                    imports.Add("datetime");
                                    break;
                                case TypeNameEnum.Guid:
                                    imports.Add("uuid");
                                    imports.Add("typing");
                                    break;
                            }

                            if (property.Options.IsNullable)
                                imports.Add("typing");
                        }
                    }

                    foreach (var ns in imports.OrderBy(i => i))
                    {
                        writer.WriteLine($"import {ns}");
                    }

                    foreach (var model in dto.Models)
                    {
                        writer.WriteLine();
                        writer.WriteLine();
                        writer.WriteLine("@dataclasses.dataclass");
                        writer.WriteLine($"class {model.DtoName.CapitalCase}:");

                        var optionalLines = new List<string>();

                        foreach (var property in model.Properties)
                        {
                            var type = property.Property.TypeNameType switch
                            {
                                TypeNameEnum.Int => "int",
                                TypeNameEnum.Long => "int",
                                TypeNameEnum.Float => "float",
                                TypeNameEnum.Double => "float",
                                TypeNameEnum.Decimal => "float",
                                TypeNameEnum.Bool => "bool",
                                TypeNameEnum.String => "str",
                                TypeNameEnum.DateTime => "datetime.datetime",
                                TypeNameEnum.Guid => "typing.Union[str, uuid.UUID]",
                                TypeNameEnum.Model => property.TypeModel!.DtoName.CapitalCase,
                                TypeNameEnum.Enum => property.Property.EnumTypeSpec!.Name.CapitalCase,
                                _ => throw new ArgumentOutOfRangeException(),
                            };

                            type = property.Options.EnumerableType switch
                            {
                                EnumerableType.None => type,
                                EnumerableType.Enumerable => $"list[{type}]",
                                EnumerableType.Collection => $"list[{type}]",
                                EnumerableType.Array => $"list[{type}]",
                                EnumerableType.List => $"list[{type}]",
                                _ => throw new ArgumentOutOfRangeException(),
                            };

                            if (property.Options.IsNullable)
                                type = $"typing.Optional[{type}]";

                            if (property.Options.IsNullable)
                            {
                                optionalLines.Add($"    {property.PropertyName.SnakeCaseName}: {type} = None");
                            }
                            else
                            {
                                writer.WriteLine($"    {property.PropertyName.SnakeCaseName}: {type}");
                            }
                        }

                        foreach (var line in optionalLines)
                        {
                            writer.WriteLine(line);
                        }

                        writer.WriteLine();
                        writer.WriteLine("    @classmethod");
                        writer.WriteLine("    def from_json(cls, data: dict):");
                        writer.WriteLine("        return cls(");

                        foreach (var property in model.Properties)
                        {
                            var access = $"data.get('{property.PropertyName.CamelCase}')";
                            var unsafeAccess = $"data['{property.PropertyName.CamelCase}']";
                            var value = property.Options.IsNullable
                                ? access
                                : unsafeAccess;

                            var needsIfNotNull = property.Options.IsNullable;

                            switch (property.Property.TypeNameType)
                            {
                                case TypeNameEnum.DateTime:
                                    value = $"datetime.datetime.fromisoformat({unsafeAccess})";
                                    break;
                                case TypeNameEnum.Guid:
                                    value = $"uuid.UUID({unsafeAccess})";
                                    break;
                                case TypeNameEnum.Model:
                                    value = $"{property.TypeModel!.DtoName}.from_json({unsafeAccess})";
                                    break;
                                case TypeNameEnum.Enum:
                                    value = $"{property.TypeModel!.DtoName}({unsafeAccess})";
                                    break;
                                default:
                                    needsIfNotNull = false;
                                    break;
                            }

                            if (needsIfNotNull)
                            {
                                value = $"{value} if {access} is not None else None";
                            }

                            writer.WriteLine($"            {property.PropertyName.SnakeCaseName}={value},");
                        }

                        writer.WriteLine("        )");

                        writer.WriteLine();
                        writer.WriteLine("    def to_json(self):");
                        writer.WriteLine("        return {");

                        foreach (var property in model.Properties)
                        {
                            var access = $"self.{property.PropertyName.SnakeCaseName}";
                            var value = access;
                            var needsIfNotNull = true;

                            switch (property.Property.TypeNameType)
                            {
                                case TypeNameEnum.DateTime:
                                    value = $"{value}.isoformat()";
                                    break;
                                case TypeNameEnum.Guid:
                                    value = $"str({value})";
                                    break;
                                case TypeNameEnum.Model:
                                    value = $"{value}.to_json()";
                                    break;
                                case TypeNameEnum.Enum:
                                    value = $"{value}.value";
                                    break;
                                default:
                                    needsIfNotNull = false;
                                    break;
                            }

                            if (property.Options.IsNullable && needsIfNotNull)
                            {
                                value = $"{value} if {access} is not None else None";
                            }

                            writer.WriteLine($"            '{property.PropertyName.CamelCase}': {value},");
                        }

                        writer.WriteLine("        }");
                    }
                }
            }
        }
    }

    private void GenerateContextCs()
    {
    }

    private void GenerateTs()
    {
        _tsGenerator.Generate();
    }

    public static void Run(Func<SolutionSpec> solution, string[] args)
    {
        var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(c => { c.AddJsonFile("appsettings.json"); })
            .ConfigureServices(services =>
            {
                services.AddSingleton(_ => solution());
                services.AddSingleton<TsGenerator>();
                services.AddSingleton<Generator>();
            })
            .Build();

        host.Services
            .GetRequiredService<Generator>()
            .Run(args[0], args);
    }
}