using System.Reflection;
using System.Text.Json.Serialization;
using Gibbs2b.DtoGenerator.Annotation;
using Gibbs2b.DtoGenerator.Design.Config;

namespace Gibbs2b.DtoGenerator.Model;

public class TsDtoSpec
{
    public Type Type { get; }
    public ProjectSpec Project { get; }
    public TsDtoModelSpec[] RootModels { get; }

    public Dictionary<Type, TsDtoModelSpec> Models { get; }

    public NameSpec DtoName { get; set; }

    public string? TsProjectName { get; set; }

    public TypescriptProjectSpec? TsProject => TsProjectName == null
        ? Project.FindTypescriptProjectByNamespace(new NamespaceSpec(Type.Namespace!))
        : Project.FindTypescriptProjectByName(TsProjectName);

    internal List<Type> NestedTypes { get; }

    public TsDtoSpec(Type type, ProjectSpec project)
    {
        Type = type;
        Project = project;
        DtoName = new NameSpec(type.Name);

        TsProjectName = type.GetCustomAttribute<GenTsDtoAttribute>()?.Project ??
                        type.GetCustomAttribute<GenTsDtoModelAttribute>()?.ProjectName;

        NestedTypes = type
            .GetNestedTypes()
            .ToList();

        RootModels = NestedTypes
            .Where(t => t.GetCustomAttribute<GenTsDtoModelAttribute>() != null)
            .Select(t => new TsDtoModelSpec(t, this))
            .ToArray();
        Models = RootModels.ToDictionary(m => m.Type);
    }
}