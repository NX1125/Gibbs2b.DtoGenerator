using Microsoft.CodeAnalysis;

namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class GenModelAttribute : Attribute
{
    public string TableName { get; set; }
    public bool IsView { get; set; }
    public bool NotMapped { get; set; }

    public GenModelAttribute()
    {
    }
}