namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class GenModelAttribute : Attribute
{
    public string TableName { get; set; } = null!;
    public bool IsView { get; set; }
    public bool NotMapped { get; set; }

    public GenModelAttribute()
    {
    }
}