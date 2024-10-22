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

    public Assembly[] Assemblies { get; }

    public ProjectSpec(Assembly[] assemblies, SolutionSpec solution)
    {
        Solution = solution;

        Assemblies = assemblies;
        Name = new NamespaceSpec(assemblies[0]);
    }

    public void Load(ILogger logger)
    {
        Logger = logger;

        List<(Type, Type)> forwards = new();

        foreach (var type in Assemblies.SelectMany(a => a.GetTypes()))
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

                if (parent != null)
                    continue;

                parent = type.GetCustomAttribute<GenTsDtoModelAttribute>()!.ParentType;

                if (parent != null)
                {
                    forwards.Add((type, parent));
                }
                else
                {
                    TsDto.Add(type, new(type, this));
                }
            }
        }

        foreach (var (type, parent) in forwards)
        {
            TsDto[parent].AddModel(type);
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

        Controllers = Assemblies
            .SelectMany(a => a.GetTypes())
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

                model = new(type, dto);
                dto.AddModel(model);
                model.LoadTsProperties();
                return model;
            }

            parent = parent.DeclaringType;
        }

        var attr = type.GetCustomAttribute<GenTsDtoModelAttribute>();
        if (attr != null)
        {
            // A lone model
            var dto = attr.ParentType != null
                ? TsDto[attr.ParentType]
                : new TsDtoSpec(type, this);
            if (dto.Models.TryGetValue(type, out var model))
                return model;

            model = new(type, dto);
            if (attr.ParentType == null)
            {
                TsDto.Add(type, dto);
            }

            dto.AddModel(model);

            model.LoadTsProperties();
            return model;
        }

        return null;
    }
}