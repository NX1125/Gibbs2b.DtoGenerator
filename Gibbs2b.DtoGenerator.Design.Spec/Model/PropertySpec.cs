using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Gibbs2b.DtoGenerator.Design.Config;

namespace Gibbs2b.DtoGenerator.Model;

public class PropertySpec : IPropertySpec, ITypescriptProperty
{
    private TypeNameEnum? _typeNameType;
    public NameSpec Name { get; set; }

    public string TypeName => BaseType.Name;

    public PropertyInfo PropertyInfo { get; set; }

    public PropertyOptions Options { get; set; } = new();

    public ModelSpec ParentModel { get; }

    public SolutionSpec Solution => ParentModel.Solution;
    public ProjectSpec Project => ParentModel.Project;

    public ModelSpec? TypeModel => Project.GetModel(PropertyInfo.PropertyType);

    public EnumSpec? EnumTypeSpec => Project.GetEnum(BaseType);

    public NameSpec PropertyName => Name;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TypeNameEnum TypeNameType
    {
        get => _typeNameType ?? (IsModel
            ? TypeNameEnum.Model
            : EnumTypeSpec != null
                ? TypeNameEnum.Enum
                : TypeNameEnum.Unknown);
        set => _typeNameType = value;
    }

    [JsonIgnore]
    public EnumerableType EnumerableType
    {
        get => Options.EnumerableType;
        set => Options.EnumerableType = value;
    }

    public virtual bool IsModel => TypeModel != null;

    public string EnumName => TypeName;
    public string DtoTsName => TypeModel!.Name.CapitalCase;

    public PropertySpec[] ForeignKeys { get; internal set; } = Array.Empty<PropertySpec>();

    public PropertySpec? Id => ParentModel.Properties.SingleOrDefault(p => p.Name.CapitalCase == $"{Name}Id");

    public Type BaseType { get; }

    private static readonly NullabilityInfoContext NullabilityContext = new();

    public PropertySpec(PropertyInfo prop, ModelSpec parent)
    {
        ParentModel = parent;
        PropertyInfo = prop;

        var type = prop.PropertyType;

        Name = new NameSpec(prop.Name);

        var nullableInfo = NullabilityContext.Create(PropertyInfo);
        if (Nullable.GetUnderlyingType(type) != null || nullableInfo.WriteState is NullabilityState.Nullable)
        {
            Options.IsNullable = true;
        }

        type = UnwrapEnumerableLevel();

        var baseType = Nullable.GetUnderlyingType(type);
        if (baseType != null)
        {
            Options.IsNullableItem = true;
            type = baseType;
        }

        if (type == typeof(int))
        {
            _typeNameType = TypeNameEnum.Int;
        }
        else if (type == typeof(float))
        {
            _typeNameType = TypeNameEnum.Float;
        }
        else if (type == typeof(long))
        {
            _typeNameType = TypeNameEnum.Long;
        }
        else if (type == typeof(double))
        {
            _typeNameType = TypeNameEnum.Double;
        }
        else if (type == typeof(string))
        {
            _typeNameType = TypeNameEnum.String;
        }
        else if (type == typeof(DateTime))
        {
            _typeNameType = TypeNameEnum.DateTime;
        }
        else if (type == typeof(decimal))
        {
            _typeNameType = TypeNameEnum.Decimal;
        }
        else if (type == typeof(bool))
        {
            _typeNameType = TypeNameEnum.Bool;
        }
        else if (type == typeof(Guid))
        {
            _typeNameType = TypeNameEnum.Guid;
        }
        // else if (type == typeof(NpgsqlTsVector))
        else if (type.FullName == "NpgsqlTypes.NpgsqlTsVector")
        {
            _typeNameType = TypeNameEnum.TsVector;
        }

        BaseType = type;

        Options.IsUrl = prop.GetCustomAttribute<UrlAttribute>() != null;
        Options.NotMapped = prop.GetCustomAttribute<NotMappedAttribute>() != null;

        Options.JsonB = prop.GetCustomAttribute<ColumnAttribute>()?.TypeName == "jsonb";
        Options.Key = prop.GetCustomAttribute<KeyAttribute>() != null || Name.CapitalCase == "Id";
        Options.JsonIgnore = prop.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition;

        Options.Required = prop.GetCustomAttribute<RequiredAttribute>() != null;

        var pattern = prop.GetCustomAttribute<RegularExpressionAttribute>()?.Pattern;
        Options.Regex = pattern != null ? new Regex(pattern) : null;
        Options.Required |= Options.Key;
        Options.Obsolete = prop.GetCustomAttribute<ObsoleteAttribute>() != null;

        Options.MaxLength = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length;
        Options.MinLength = prop.GetCustomAttribute<MinLengthAttribute>()?.Length;
    }

    private Type UnwrapEnumerableLevel()
    {
        var baseType = PropertyInfo.PropertyType;
        while (true)
        {
            var underlyingType = Nullable.GetUnderlyingType(baseType);
            baseType = underlyingType ?? baseType;

            if (baseType.IsArray)
            {
                var itemType = baseType.GetElementType()!;
                if (Options.EnumerableType != EnumerableType.None && Options.EnumerableType != EnumerableType.Array)
                    throw new InvalidOperationException();

                Options.EnumerableType = EnumerableType.Array;
                Options.EnumerableDimension += baseType.GetArrayRank();
                baseType = itemType;
            }
            else
            {
                var enumerableType = SolveEnumerable(baseType);
                if (enumerableType == EnumerableType.None)
                {
                    return baseType;
                }

                var itemType = baseType.GenericTypeArguments[0];

                baseType = itemType ?? throw new NullReferenceException();

                Options.EnumerableDimension++;

                if (Options.EnumerableType != EnumerableType.None && Options.EnumerableType != enumerableType)
                {
                    throw new InvalidOperationException();
                }

                Options.EnumerableType = enumerableType;
            }
        }
    }

    public override string ToString()
    {
        return PropertyInfo.ToString() ?? Name.CapitalCase;
    }

    private static EnumerableType SolveEnumerable(Type type)
    {
        return SolveEnumerable(type, typeof(Array), typeof(Array), EnumerableType.Array)
               ?? SolveEnumerable(type, typeof(IList), typeof(IList<>), EnumerableType.List)
               ?? SolveEnumerable(type, typeof(ICollection), typeof(ICollection<>), EnumerableType.Collection)
               ?? SolveEnumerable(type, typeof(IEnumerable), typeof(IEnumerable<>), EnumerableType.Enumerable)
               ?? SolveEnumerable(type, typeof(IDictionary), typeof(IDictionary<,>), EnumerableType.Dictionary)
               ?? EnumerableType.None;
    }

    private static EnumerableType? SolveEnumerable(Type type, Type genericType, Type generalType,
        EnumerableType enumerableType)
    {
        Type definition;
        try
        {
            definition = type.GetGenericTypeDefinition();
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        return definition == generalType || definition == genericType
            ? enumerableType
            : null;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TypeNameEnum
{
    Unknown,
    Int,
    Long,
    Float,
    Double,
    Decimal,
    Bool,
    String,
    DateTime,
    Guid,
    Model,
    Enum,
    TsVector,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnumerableType
{
    None,
    Enumerable,
    Collection,
    Array,
    List,
    Dictionary,
}

public class PropertyOptions
{
    public PropertyOptions()
    {
    }

    public PropertyOptions(PropertyOptions source)
    {
        foreach (var prop in GetType().GetProperties())
        {
            prop.SetValue(this, prop.GetValue(source));
        }
    }

    public bool IsNullable { get; set; }
    public bool IsNullableItem { get; set; }
    public bool IsUrl { get; set; }
    public bool NotMapped { get; set; }
    public EnumerableType EnumerableType { get; set; }
    public int EnumerableDimension { get; set; }
    public bool JsonB { get; set; }
    public bool Key { get; set; }
    public bool ModelKeys { get; set; }
    public bool Required { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public bool Obsolete { get; set; }

    public string? RegexPattern
    {
        get => Regex?.ToString();
        set => Regex = value == null ? null : new Regex(value);
    }

    public Regex? Regex { get; set; }
    public JsonIgnoreCondition? JsonIgnore { get; set; }
}