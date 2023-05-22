using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Gibbs2b.DtoGenerator.Model;

public class DtoPropertySpec : IPropertySpec, ITypescriptProperty
{
    private PropertyOptions? _options;

    [JsonIgnore]
    public PropertySpec Property { get; set; }

    public string DtoTsName => TypeModel!.TsName;
    public NameSpec PropertyName { get; set; }

    [JsonIgnore]
    public DtoModelSpec Parent { get; internal set; } = null!;

    [JsonIgnore]
    public SolutionSpec Solution => Parent.Solution;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PropertyOptions Options
    {
        get => _options ?? Property.Options;
        set => _options = value;
    }

    [JsonIgnore]
    public TypeNameEnum TypeNameType
    {
        get => Property.TypeNameType;
        set => throw new NotImplementedException();
    }

    [JsonIgnore]
    public EnumerableType EnumerableType
    {
        get => Options.EnumerableType;
        set => Options.EnumerableType = value;
    }

    public string? EnumName => Property.EnumName;

    [JsonIgnore]
    public DtoModelSpec? TypeModel
    {
        get
        {
            if (Property.TypeNameType != TypeNameEnum.Model)
                return null;

            var model = Property.TypeModel;
            if (model == null)
                throw new InvalidOperationException($"Missing model for field {Parent.DtoName}.{PropertyName}");

            var dto = Parent.Parent.FindDtoByModel(model);

            if (dto == null)
                throw new InvalidOperationException($"Missing dto model {model.Name} for field {Parent.DtoName}.{PropertyName}");

            return dto;
        }
    }

    public DtoPropertySpec(MemberExpression prop, ModelSpec model, bool isView) : this(prop.Member, model, isView)
    {
    }

    public DtoPropertySpec(MemberInfo prop, ModelSpec model, bool isView)
    {
        PropertyName = new(prop.Name);
        Property = model.FindProperty(prop)!;

        _options = new PropertyOptions(Property.Options)
        {
            EnumerableType = !isView && Property.TypeNameType == TypeNameEnum.Model
                ? Property.Options.EnumerableType switch
                {
                    EnumerableType.Collection => EnumerableType.Enumerable,
                    EnumerableType.Array => EnumerableType.Enumerable,
                    EnumerableType.List => EnumerableType.Enumerable,
                    _ => Property.Options.EnumerableType,
                }
                : Property.Options.EnumerableType,
        };
    }

    public DtoPropertySpec(PropertySpec prop, DtoModelSpec parent)
    {
        PropertyName = prop.Name;
        Property = prop;
        Parent = parent;
    }
}