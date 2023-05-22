using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Gibbs2b.DtoGenerator.Annotation;
using Gibbs2b.DtoGenerator.Model;
using Gibbs2b.DtoGenerator.Typescript;
using Microsoft.CodeAnalysis;

namespace Gibbs2b.DtoGenerator;

public class ProjectSpec
{
    public string SourcePath { get; set; } = null!;

    public NamespaceSpec Name { get; set; } = null!;

    public FileInfo CsprojPath => new(Path.Combine($"{SourcePath}", $"{Name}.csproj"));

    public IList<DtoSpecFactory> DtoSpecFactories { get; } = new List<DtoSpecFactory>();

    public IList<ModelSpec> Models { get; set; } = new List<ModelSpec>();
    public IList<EnumSpec> Enums { get; set; } = new List<EnumSpec>();
    public IList<DtoSpec> Dto { get; set; } = new List<DtoSpec>();
    public IList<TsDtoSpec> TsDto { get; set; } = new List<TsDtoSpec>();

    public IEnumerable<DtoSpec> Views => Dto.Where(d => d.IsView);

    public string ContextPath { get; set; } = null!;
    public NamespaceSpec ContextNamespace { get; set; } = null!;
    public NameSpec ContextName { get; set; } = null!;

    public SolutionSpec Solution { get; internal set; } = null!;

    public ViewNamespacePrefix[] Prefixes { get; set; } = Array.Empty<ViewNamespacePrefix>();

    public ProjectSpec(Action<string[]> main, SolutionSpec solution) : this(main.Method.Module.Assembly, solution)
    {
    }

    public ProjectSpec(Assembly assembly, SolutionSpec solution)
    {
        Solution = solution;

        Name = new NamespaceSpec(assembly);

        foreach (var type in assembly.GetTypes())
        {
            var model = type.GetCustomAttribute<GenModelAttribute>();
            if (model != null)
            {
                Models.Add(new ModelSpec(type)
                {
                    Project = this,
                    NotMapped = model.NotMapped,
                });
            }
            else if (type.GetCustomAttribute<GenEnumAttribute>() != null)
            {
                Enums.Add(new EnumSpec(type));
            }
            else if (type.IsSubclassOf(typeof(DtoSpecFactory)))
            {
                var factory = (DtoSpecFactory) Activator.CreateInstance(type)!;
                factory.Project = this;
                DtoSpecFactories.Add(factory);
            }
            else if (type.IsSubclassOf(typeof(DbContext)))
            {
                ContextNamespace = new NamespaceSpec(type);
                ContextPath = Path.Combine(Path.Combine(ContextNamespace.Namespace
                    .Remove(0, Name.Namespace.Length)
                    .Split('.')), type.Name);
                ContextName = new(type.Name);
            }
            else if (type.GetCustomAttribute<GenTsDtoAttribute>() != null)
            {
                TsDto.Add(new TsDtoSpec(type, this));
            }
        }

        foreach (var model in Models)
        {
            model.LoadProperties();
        }

        foreach (var dto in TsDto)
        {
            foreach (var model in dto.Models)
            {
                model.LoadTsProperties();
            }
        }

        // TODO: Primary keys
        // TODO: Foreign keys

        foreach (var factory in DtoSpecFactories
                     .OrderByDescending(f => f.IsView))
        {
            var dto = factory.CreateSpec();
            Dto.Add(dto);
        }
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
            .SingleOrDefault(p => p.DefaultNamespaces
                .Any(ns.StartsWith));
    }

    public TypescriptProjectSpec? FindTypescriptProjectByName(string name)
    {
        return Solution.TypescriptProjects
            .SingleOrDefault(p => p.Name == name);
    }

    public ModelSpec? GetModel<TModel>()
    {
        return GetModel(typeof(TModel));
    }

    public ModelSpec? GetModel(Type type)
    {
        return Models
            .FirstOrDefault(model => model.Type == type);
    }

    public EnumSpec? GetEnum(Type type)
    {
        return Enums
            .FirstOrDefault(model => model.Type == type);
    }
}