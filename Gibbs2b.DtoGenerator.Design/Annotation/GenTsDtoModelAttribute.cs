namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class GenTsDtoModelAttribute : Attribute
{
    public bool NullableBool { get; set; }
}