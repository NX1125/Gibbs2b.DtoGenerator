using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using Gibbs2b.DtoGenerator.Annotation;

namespace Gibbs2b.DtoGenerator.Model;

public class EnumSpec
{
    public EnumSpec(Type type)
    {
        Type = type;
        Name = new NameSpec { CapitalCase = type.Name };
        Namespace = new NamespaceSpec { Namespace = type.Namespace! };
        Values = type.GetEnumNames();
    }

    public EnumSpec()
    {
    }

    public Type Type { get; }

    public NameSpec Name { get; set; }
    public NamespaceSpec Namespace { get; set; }

    [Obsolete]
    public ICollection<string> Values { get; set; }

    public bool TsArrayEnabled => Type.GetCustomAttribute<GenEnumAttribute>()!.TsArrayEnabled;

    public Type? ConverterType => Type.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType;

    public bool IsJsonName => ConverterType == typeof(JsonStringEnumConverter);
}