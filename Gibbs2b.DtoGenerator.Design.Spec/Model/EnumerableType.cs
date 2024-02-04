using System.Text.Json.Serialization;

namespace Gibbs2b.DtoGenerator.Model;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnumerableType
{
    None,
    Enumerable,
    Collection,
    Array,
    List,

    [Obsolete]
    Dictionary,
}