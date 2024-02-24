namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class GenTsDtoModelAttribute : Attribute
{
    public bool NullableBool { get; set; }

    public string? ProjectName { get; set; }

    public Type? ParentType { get; set; }
}