using System.Text.Json.Serialization;

namespace Gibbs2b.DtoGenerator.Model;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TypeNameEnum
{
    Unknown,
    Object,
    Int,
    Long,
    Float,
    Double,
    Decimal,
    Bool,
    String,
    Uri,
    DateTime,
    Guid,
    Model,
    Enum,
    TsVector,
}