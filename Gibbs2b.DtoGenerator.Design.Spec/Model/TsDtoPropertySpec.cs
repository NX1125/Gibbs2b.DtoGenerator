using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Gibbs2b.DtoGenerator.Annotation;

namespace Gibbs2b.DtoGenerator.Model;

public class TsDtoPropertySpec
{
    private object[]? _possibleValues;
    public NameSpec Name { get; set; }
    public TsTypeSpec Type { get; set; }
    public PropertyOptions Options { get; set; } = new();
    public TsDtoModelSpec ParentDto { get; }

    public object[]? PossibleValues => _possibleValues ??= Property.GetCustomAttribute<TsValueAttribute>()?.Values;

    public TsDtoPropertySpec(PropertyInfo prop, TsDtoModelSpec parent)
    {
        Property = prop;
        ParentDto = parent;
        Name = new(prop.Name);

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

        Type = TsTypeSpec.Parse(prop.PropertyType, parent, this);
    }

    public PropertyInfo Property { get; set; }
}