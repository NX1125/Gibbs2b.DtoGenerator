namespace Gibbs2b.DtoGenerator.Annotation;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class GenTsDtoOpaqueModelAttribute : Attribute
{
    public string? ImportFrom { get; set; } = "./model";
    public string Name { get; set; }

    public GenTsDtoOpaqueModelAttribute(string name)
    {
        Name = name;
    }
}