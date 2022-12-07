namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class GenViewAttribute : GenModelAttribute
{
    public GenViewAttribute(string? tableName = null)
    {
        IsView = true;
    }
}