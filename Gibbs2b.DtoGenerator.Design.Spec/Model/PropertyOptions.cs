using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Gibbs2b.DtoGenerator.Model;

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