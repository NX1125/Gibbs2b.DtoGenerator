using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;
using NpgsqlTypes;

namespace Gibbs2b.DtoGenerator.Model;

public class PropertySpec : IPropertySpec, ITypescriptProperty
{
    private TypeNameEnum? _typeNameType;
    public NameSpec Name { get; set; }

    public string TypeName { get; set; }

    [JsonIgnore]
    public PropertyInfo? PropertyInfo { get; set; }

    public PropertyOptions Options { get; set; } = new();

    [JsonIgnore]
    public ModelSpec Parent { get; internal set; } = null!;

    internal SolutionSpec? _solution;

    [JsonIgnore]
    public SolutionSpec Solution => _solution ?? Parent.Solution;

    [JsonIgnore]
    public ModelSpec? TypeModel => Solution.GetModel(TypeName);

    [JsonIgnore]
    public EnumSpec? TypeEnum => Solution.GetEnum(TypeName);

    public NameSpec PropertyName => Name;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TypeNameEnum TypeNameType
    {
        get => _typeNameType ?? (TypeModel != null
            ? TypeNameEnum.Model
            : TypeEnum != null
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

    public string EnumName => TypeName;
    public string DtoTsName => TypeModel!.Name.CapitalCase;

    public NameSpec[] ForeignKeys { get; set; } = Array.Empty<NameSpec>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasMany { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? WithMany { get; set; }

    [JsonIgnore]
    public bool ForceColumnName { get; set; }

    [JsonIgnore]
    public PropertySpec? Id => Parent.Properties.SingleOrDefault(p => p.Name.CapitalCase == $"{Name}Id");

    public PropertySpec()
    {
    }

    public PropertySpec(PropertyInfo prop)
    {
        PropertyInfo = prop;

        var type = prop.PropertyType;

        Name = new NameSpec { CapitalCase = prop.Name };

        var baseType = Nullable.GetUnderlyingType(type);

        if (baseType != null)
        {
            Options.IsNullable = true;
            type = baseType;
        }

        EnumerableType? enumerableType = null;

        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
        {
            type = type.GenericTypeArguments[0];
        }
        else if (type.IsArray)
        {
            enumerableType = EnumerableType.Array;
            type = type.GetElementType()!;
        }

        baseType = Nullable.GetUnderlyingType(type);

        if (baseType != null)
        {
            Options.IsNullableItem = true;
            type = baseType;
        }

        TypeName = type.Name;

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
        else if (type == typeof(NpgsqlTsVector))
        {
            _typeNameType = TypeNameEnum.TsVector;
        }

        // TODO: To switch
        Options.EnumerableType = enumerableType
                                 ?? SolveEnumerable(prop.PropertyType, typeof(Array), typeof(Array), EnumerableType.Array)
                                 ?? SolveEnumerable(prop.PropertyType, typeof(IList), typeof(IList<>), EnumerableType.List)
                                 ?? SolveEnumerable(prop.PropertyType, typeof(ICollection), typeof(ICollection<>), EnumerableType.Collection)
                                 ?? SolveEnumerable(prop.PropertyType, typeof(IEnumerable), typeof(IEnumerable<>), EnumerableType.Enumerable)
                                 ?? EnumerableType.None;

        Options.IsUrl = prop.GetCustomAttribute<UrlAttribute>() != null;
        Options.NotMapped = prop.GetCustomAttribute<NotMappedAttribute>() != null;

        Options.JsonB = prop.GetCustomAttribute<ColumnAttribute>()?.TypeName == "jsonb";
        Options.Key = prop.GetCustomAttribute<KeyAttribute>() != null || Name.CapitalCase == "Id";
        Options.JsonIgnore = prop.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition;

        Options.Required = prop.GetCustomAttribute<RequiredAttribute>() != null;

        var pattern = prop.GetCustomAttribute<RegularExpressionAttribute>()?.Pattern;
        Options.Regex = pattern != null ? new Regex(pattern) : null;
        Options.Required |= Options.Key;

        Options.MaxLength = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length;
        Options.MinLength = prop.GetCustomAttribute<MinLengthAttribute>()?.Length;
    }

    private static EnumerableType? SolveEnumerable(Type type, Type genericType, Type generalType, EnumerableType enumerableType)
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

    public void CreateSchema()
    {
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
}

public class PropertyOptions
{
    public PropertyOptions()
    {
    }

    public PropertyOptions(PropertyOptions source)
    {
        Key = source.Key;
        Regex = source.Regex;
        Required = source.Required;
        EnumerableType = source.EnumerableType;
        IsNullable = source.IsNullable;
        IsUrl = source.IsUrl;
        JsonB = source.JsonB;
        JsonIgnore = source.JsonIgnore;
        MaxLength = source.MaxLength;
        MinLength = source.MinLength;
        ModelKeys = source.ModelKeys;
        NotMapped = source.NotMapped;
        RegexPattern = source.RegexPattern;
        IsNullableItem = source.IsNullableItem;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsNullable { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsNullableItem { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsUrl { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool NotMapped { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public EnumerableType EnumerableType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool JsonB { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Key { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ModelKeys { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Required { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinLength { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxLength { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegexPattern
    {
        get => Regex?.ToString();
        set => Regex = value == null ? null : new Regex(value);
    }

    [JsonIgnore]
    public Regex? Regex { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonIgnoreCondition? JsonIgnore { get; set; }

    public PropertyOptions Clone()
    {
        return new PropertyOptions
        {
            Key = Key,
            Regex = Regex,
            Required = Required,
            EnumerableType = EnumerableType,
            IsNullable = IsNullable,
            IsUrl = IsUrl,
            JsonB = JsonB,
            JsonIgnore = JsonIgnore,
            MaxLength = MaxLength,
            MinLength = MinLength,
            ModelKeys = ModelKeys,
            NotMapped = NotMapped,
            RegexPattern = RegexPattern,
            IsNullableItem = IsNullableItem,
        };
    }
}