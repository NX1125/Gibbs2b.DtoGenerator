namespace Gibbs2b.DtoGenerator.Model;

public class TypescriptProjectSpec
{
    public string Name { get; set; } = null!;
    public string[] Paths { get; set; } = null!;
    public NamespaceSpec DefaultNamespace { get; set; } = null!;
}