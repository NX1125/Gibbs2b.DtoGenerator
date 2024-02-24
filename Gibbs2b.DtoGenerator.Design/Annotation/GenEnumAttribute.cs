namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Enum)]
public class GenEnumAttribute : Attribute
{
    public bool TsArrayEnabled { get; set; }
    public string? DtoName { get; set; }
}