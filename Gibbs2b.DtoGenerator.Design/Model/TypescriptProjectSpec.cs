namespace Gibbs2b.DtoGenerator.Model;

public class TypescriptProjectSpec
{
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public NamespaceSpec Namespace { get; set; } = null!;
}