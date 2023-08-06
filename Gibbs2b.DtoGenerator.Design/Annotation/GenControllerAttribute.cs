namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class GenControllerAttribute : Attribute
{
    public string? TypescriptProject { get; set; }
}