using System.Reflection;
using Gibbs2b.DtoGenerator.Annotation;
using Gibbs2b.DtoGenerator.Model;
using Microsoft.Extensions.Logging;

namespace Gibbs2b.DtoGenerator.Design.Config;

public class ProjectSpec
{
    public static ILogger Logger { get; private set; }

    public string SourcePath { get; set; } = null!;

    public NamespaceSpec Name { get; set; } = null!;

    public FileInfo CsprojPath => new(Path.Combine($"{SourcePath}", $"{Name}.csproj"));

    public Dictionary<Type, TsDtoSpec> TsDto { get; } = new();

    public Dictionary<Type, TsDtoOpaqueModelSpec> TsOpaqueModels { get; } = new();

    public SolutionSpec Solution { get; internal set; } = null!;

    public Dictionary<Type, EnumSpec> Enums { get; } = new();

    /// <summary>
    /// Used for database views.
    /// </summary>
    public ViewNamespacePrefix[] Prefixes { get; set; } = Array.Empty<ViewNamespacePrefix>();

    public ICollection<ControllerSpec> Controllers { get; private set; }

    public List<TsTypeSpec.LazyTypeSpec> LazyTypeSpecs { get; } = new();

    public Assembly Assembly { get; }

    public ProjectSpec(Action<string[]> main, SolutionSpec solution) : this(main.Method.Module.Assembly, solution)
    {
    }

    public ProjectSpec(Assembly assembly, SolutionSpec solution)
    {
        Solution = solution;

        Assembly = assembly;
        Name = new NamespaceSpec(assembly);
    }

    public void Load(ILogger logger)
    {
        Logger = logger;

        foreach (var type in Assembly.GetTypes())
        {
            if (type.GetCustomAttribute<GenEnumAttribute>() != null)
            {
                Enums.Add(type, new(type));
            }
            else if (type.GetCustomAttribute<GenTsDtoAttribute>() != null)
            {
                TsDto.Add(type, new(type, this));
            }
            else if (type.GetCustomAttribute<GenTsDtoOpaqueModelAttribute>() != null)
            {
                TsOpaqueModels.Add(type, new(type, this));
            }
            else if (type.GetCustomAttribute<GenTsDtoModelAttribute>() != null)
            {
                var parent = type;
                while (parent != null)
                {
                    if (parent.GetCustomAttribute<GenTsDtoAttribute>() != null)
                    {
                        break;
                    }

                    parent = parent.DeclaringType;
                }

                if (parent == null)
                    TsDto.Add(type, new(type, this));
            }
        }

        var rootModels = TsDto.Values
            .SelectMany(dto => dto.Models.Values)
            .ToArray();

        foreach (var model in rootModels)
        {
            model.LoadTsProperties();
        }

        // needs to be with iterator as the Solve method can add more lazy types
        for (var i = 0; i < LazyTypeSpecs.Count; i++)
        {
            var lazy = LazyTypeSpecs[i];
            Logger.LogInformation("Solving lazy type {0}", lazy.Type);
            lazy.Solve();
        }

        Controllers = Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<GenControllerAttribute>() != null)
            .Select(t => new ControllerSpec(t, this, t.GetCustomAttribute<GenControllerAttribute>()!))
            .ToArray();
    }

    public ViewNamespacePrefix? GetViewPrefix(string ns)
    {
        return Prefixes.SingleOrDefault(prefix => ns.StartsWith($"{prefix.Namespace}."));
    }

    public TypescriptProjectSpec? GetTypescriptProject(string name)
    {
        return Solution.TypescriptProjects
            .SingleOrDefault(p => p.Name == name);
    }

    public TypescriptProjectSpec? FindTypescriptProjectByNamespace(NamespaceSpec ns)
    {
        return Solution.TypescriptProjects
            .SingleOrDefault(p => p.Namespaces
                .Any(ns.StartsWith));
    }

    public TypescriptProjectSpec? FindTypescriptProjectByNamespace(string ns)
    {
        return FindTypescriptProjectByNamespace(new NamespaceSpec(ns));
    }

    public TypescriptProjectSpec? FindTypescriptProjectByName(string name)
    {
        return Solution.TypescriptProjects
            .SingleOrDefault(p => p.Name == name);
    }

    public EnumSpec? GetOrLoadEnum(Type type)
    {
        if (Enums.TryGetValue(type, out var spec))
            return spec;

        if (type.IsEnum)
        {
            Enums.Add(type, new(type));
            return Enums[type];
        }

        return null;
    }

    public TsDtoModelSpec? GetModel(Type type)
    {
        throw new NotImplementedException();
    }

    public TsDtoModelSpec? GetOrLoadModel(Type type)
    {
        // find parent dto group
        var parent = type;
        while (parent != null)
        {
            if (TsDto.TryGetValue(parent, out var dto))
            {
                if (dto.Models.TryGetValue(type, out var model))
                    return model;

                model = dto.Models[type] = new(type, dto);
                model.LoadTsProperties();
                return model;
            }

            parent = parent.DeclaringType;
        }

        if (type.GetCustomAttribute<GenTsDtoModelAttribute>() != null)
        {
            // A lone model
            var dto = new TsDtoSpec(type, this);
            var model = dto.Models[type] = new(type, dto);
            TsDto.Add(type, dto);
            model.LoadTsProperties();
            return model;
        }

        return null;
    }
}