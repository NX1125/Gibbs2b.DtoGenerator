using System.Reflection;

namespace Gibbs2b.DtoGenerator.Model;

public class DtoTsPropertySpec
{
    public PropertySpec Property { get; }

    public DtoTsPropertySpec(PropertyInfo prop)
    {
        Property = new PropertySpec(prop);
    }
}