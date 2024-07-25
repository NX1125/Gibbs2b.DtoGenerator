namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Method)]
public class GenHandlerAttribute : Attribute
{
    public bool FormData { get; set; }
}