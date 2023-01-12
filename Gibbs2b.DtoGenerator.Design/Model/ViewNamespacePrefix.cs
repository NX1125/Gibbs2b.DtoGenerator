namespace Gibbs2b.DtoGenerator.Model;

public class ViewNamespacePrefix
{
    public NamespaceSpec Namespace { get; set; } = null!;
    public string Infix { get; set; } = null!;

    public string ViewPrefix { get; set; } = null!;

    public ViewNamespacePrefix()
    {
    }

    public ViewNamespacePrefix(string ns, string? infix = null)
    {
        Namespace = new NamespaceSpec(ns);
        Infix = infix ?? Namespace.Parts[^1].CapitalCase;
        ViewPrefix = $"Dto_{infix}_";
    }
}