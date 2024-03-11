namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Property)]
public class TsValueAttribute : Attribute
{
    public object[] Values { get; set; }

    public TsValueAttribute(params object[] values)
    {
        Values = values;
    }
}