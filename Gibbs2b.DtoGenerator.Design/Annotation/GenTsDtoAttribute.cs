namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class GenTsDtoAttribute : Attribute
{
    public string? Project { get; set; }
}