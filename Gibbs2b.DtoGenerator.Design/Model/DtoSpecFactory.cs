using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using Gibbs2b.DtoGenerator.Annotation;
using Gibbs2b.DtoGenerator.Typescript;
using Microsoft.EntityFrameworkCore;

namespace Gibbs2b.DtoGenerator.Model;

public abstract class DtoSpecFactory
{
    public ICollection<DtoModelSpec> Models { get; } = new List<DtoModelSpec>();

    public SolutionSpec Solution => Project.Solution;

    public ProjectSpec Project { get; set; } = null!;

    public DtoOptions Options { get; set; } = new();

    public bool IsView
    {
        get => Options.IsView;
        set => Options.IsView = value;
    }

    public bool CopyFromModel
    {
        get => Options.CopyFromModel;
        set => Options.CopyFromModel = value;
    }

    public bool CopyToModel
    {
        get => Options.CopyToModel;
        set => Options.CopyToModel = value;
    }

    public bool DefaultPythonProject
    {
        get => Options.PythonProjects.Any(p => p == "default");
        set => Options.PythonProjects.Add("default");
    }

    public bool DefaultTypescriptProject
    {
        get => Options.TypescriptProjects.Any(p => p == "default");
        set => Options.TypescriptProjects.Add("default");
    }

    public string DefaultTypescriptProjectName
    {
        set => Options.TypescriptProjects.Add(value);
    }

    public NameSpec ViewInfix { get; set; } = null!;
    public NameSpec DtoName { get; set; } = null!;

    public abstract void OnCreateSpec();

    public DtoSpec CreateSpec()
    {
        if (Solution == null)
            throw new ArgumentNullException();

        DtoName = new NameSpec { CapitalCase = GetType().Name };
        ViewInfix = DtoName;

        OnCreateSpec();

        return new DtoSpec(Project, this)
        {
            Models = Models,
            Name = DtoName,
            Namespace = new NamespaceSpec(GetType().Namespace!),
            Options = Options,
            Factory = this,
        };
    }

    private IEnumerable<DtoPropertySpec> CreateProperties(NewExpression newExpression, ModelSpec model, DtoModelSpec parent)
    {
        var names = newExpression.Arguments
            .Select(a => ((MemberExpression) a).Member.Name)
            .ToHashSet();
        foreach (var argument in newExpression.Arguments)
        {
            var prop = new DtoPropertySpec((MemberExpression) argument, model, IsView);
            yield return prop;

            var id = prop.Property.Id;
            if (!IsView || id == null || names.Contains(id.Name.CapitalCase))
                continue;

            yield return new DtoPropertySpec(id, parent);
        }
    }

    public DtoModelSpec<TModel> AddView<TModel>(Expression<Func<TModel, object>> properties,
        Expression<Func<TModel, object>> dtoFields)
    {
        return AddView<TModel>(properties, null, dtoFields);
    }

    public DtoModelSpec<TModel> AddView<TModel>(Expression<Func<TModel, object>> properties, Action<DtoModelSpec<TModel>>? configure,
        Expression<Func<TModel, object>> dtoFields)
    {
        var dto = AddModel(properties);
        var newExpression = (NewExpression) dtoFields.Body;

        var enabled = newExpression.Arguments
            .Select(a => ((MemberExpression) a).Member.Name)
            .ToHashSet();

        foreach (var prop in dto.Properties)
        {
            if (!enabled.Contains(prop.PropertyName.CapitalCase) || prop.Property.TypeNameType == TypeNameEnum.TsVector)
                prop.Options = new PropertyOptions(prop.Options)
                {
                    JsonIgnore = JsonIgnoreCondition.Always,
                };
        }

        configure?.Invoke(dto);
        return dto;
    }

    public DtoModelSpec<TModel> AddModel<TModel>(Expression<Func<TModel, object>> properties, Action<DtoModelSpec<TModel>>? configure = null)
    {
        var model = Project.GetModel<TModel>();
        if (model == null)
            throw new ArgumentException($"Not a model: {typeof(TModel)}");

        var newExpression = (NewExpression) properties.Body;

        var attr = typeof(TModel).GetCustomAttribute<GenModelAttribute>()!;
        var dto = new DtoModelSpec<TModel>(null)
        {
            Model = model,
            DtoName = model.Name.RemoveSuffix("View").Append("Dto"),
            DbViewName = IsView ? $"Dto{Project.GetViewPrefix(typeof(TModel).Namespace!)?.Infix}_{ViewInfix}_{model.Name}" : null,
            TableName = attr.TableName,
        };
        dto.Properties = CreateProperties(newExpression, model, dto).ToList();

        if (dto.DbViewName is { Length: > 62 })
        {
            dto.DbViewName = $"{dto.DbViewName[..62]}~";
        }

        Models.Add(dto);

        configure?.Invoke(dto);

        return dto;
    }
}

public class DtoModelSpec<TModel> : DtoModelSpec
{
    public DtoPropertySpec Property<T>(Expression<Func<TModel, T>> field)
    {
        var expr = ((MemberExpression) field.Body).Member.Name;
        try
        {
            return Properties.Single(p => p.PropertyName.CapitalCase == expr);
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException($"Could not find {DtoName}.{expr}");
        }
    }

    public void IgnoreJson<TField>(Expression<Func<TModel, TField>> field)
    {
        var options = Property(field).Options;
        options.JsonIgnore = JsonIgnoreCondition.Always;
    }

    public DtoModelSpec(Type? type) : base(type)
    {
    }
}