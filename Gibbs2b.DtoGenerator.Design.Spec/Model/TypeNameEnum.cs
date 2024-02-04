using System.Text.Json.Serialization;

namespace Gibbs2b.DtoGenerator.Model;

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