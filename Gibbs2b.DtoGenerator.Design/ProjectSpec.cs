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
    public string SourcePath { get; set; }

    public NamespaceSpec Name { get; set; }

    [JsonIgnore]
    public FileInfo CsprojPath => new(Path.Combine($"{SourcePath}", $"{Name}.csproj"));

    [JsonIgnore]
    public IList<DtoSpecFactory> DtoSpecFactories { get; } = new List<DtoSpecFactory>();

    public IList<ModelSpec> Models { get; set; } = new List<ModelSpec>();
    public IList<EnumSpec> Enums { get; set; } = new List<EnumSpec>();
    public IList<DtoSpec> Dto { get; set; } = new List<DtoSpec>();
    public IList<ModelSpec> TypescriptInterfaces { get; set; } = new List<ModelSpec>();

    public IEnumerable<DtoSpec> Views => Dto.Where(d => d.IsView);

    public string ContextPath { get; set; }
    public NamespaceSpec ContextNamespace { get; set; }
    public NameSpec ContextName { get; set; }

    [JsonIgnore]
    public SolutionSpec Solution { get; internal set; }

    public ViewNamespacePrefix[] Prefixes { get; set; } = Array.Empty<ViewNamespacePrefix>();

    public ProjectSpec(Action<string[]> main) : this(main.Method.Module.Assembly, false)
    {
    }

    public ProjectSpec(Assembly assembly, bool contextEnabled = true)
    {
        Name = new NamespaceSpec(assembly);

        foreach (var type in assembly.GetTypes())
        {
            var model = type.GetCustomAttribute<GenModelAttribute>();
            if (model != null)
            {
                Models.Add(new ModelSpec(type)
                {
                    Parent = this,
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
                // Context = (DbContext) Activator.CreateInstance(type)!;
                ContextNamespace = new NamespaceSpec(type);
                ContextPath = Path.Combine(Path.Combine(ContextNamespace.Namespace
                    .Remove(0, Name.Namespace.Length)
                    .Split('.')), type.Name);
                ContextName = new NameSpec { CapitalCase = type.Name };

                // ((IGenDbContext) Context).InternalDtoIgnore = true;
            }
        }
    }

    public ProjectSpec()
    {
    }

    public void CreateDtoSpecs()
    {
        if (Solution == null)
            throw new ArgumentNullException();

        foreach (var factory in DtoSpecFactories
                     .OrderByDescending(f => f.IsView))
        {
            factory.Project = this;

            var dto = factory.CreateSpec();

            Dto.Add(dto);
        }
    }

    public ViewNamespacePrefix? GetViewPrefix(string ns)
    {
        return Prefixes.SingleOrDefault(prefix => ns.StartsWith($"{prefix.Namespace}."));
    }

    public void CreateSchema()
    {
        if (Solution == null)
            throw new NullReferenceException();

        CreateDtoSpecs();

        foreach (var model in Models)
        {
            model.CreateSchema();
        }
    }

    public TypescriptProjectSpec? GetTypescriptProject(string name)
    {
        return Solution.TypescriptProjects
            .SingleOrDefault(p => p.Name == name);
    }
}