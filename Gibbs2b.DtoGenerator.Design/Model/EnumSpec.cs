using System.Collections;

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

    public ICollection<string> Values { get; set; }
}