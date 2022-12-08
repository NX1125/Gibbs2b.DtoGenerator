using System.Reflection;
using System.Text.Json.Serialization;
using Gibbs2b.DtoGenerator.Annotation;

namespace Gibbs2b.DtoGenerator.Model;

public class TsDtoSpec
{
    public Type Type { get; }
    public ProjectSpec Project { get; }
    public TsDtoModelSpec[] RootModels { get; }

    public TsDtoModelSpec[] Models { get; }

    public NameSpec DtoName { get; set; }

    public string TsPath => Path.Combine(Project.FindTypescriptProjectByNamespace(new NamespaceSpec(Type.Namespace!))!.Path, $"{DtoName.KebabCase}.dto.gen.ts");

    public TsDtoSpec(Type type, ProjectSpec project)
    {
        Type = type;
        Project = project;
        DtoName = new NameSpec(type.Name);

        RootModels = type
            .GetNestedTypes()
            .Where(t => t.GetCustomAttribute<GenTsDtoModelAttribute>() != null)
            .Select(t => new TsDtoModelSpec(t, this))
            .ToArray();

        var models = RootModels
            .ToDictionary(m => m.Type);

        foreach (var model in RootModels)
        {
            FindModels(model, models);
        }

        Models = models.Values.ToArray();
    }

    private void FindModels(TsDtoModelSpec model, IDictionary<Type, TsDtoModelSpec> specs)
    {
        model.LoadTsProperties();
        var props = model.Properties
            .Where(p => p.TypeNameType is TypeNameEnum.Model or TypeNameEnum.Unknown && p.Options.JsonIgnore != JsonIgnoreCondition.Always)
            .Select(p => p.BaseType)
            .ToArray();
        FindModels(props, specs);
    }

    private void FindModels(IEnumerable<Type> types, IDictionary<Type, TsDtoModelSpec> specs)
    {
        // create models for types that are directly or indirectly by a model
        foreach (var type in types)
        {
            if (specs.ContainsKey(type))
                continue;

            var model = new TsDtoModelSpec(type, this);
            specs[type] = model;
            FindModels(model, specs);
        }
    }
}