namespace Gibbs2b.DtoGenerator.Model;

public class ViewNamespacePrefix
{
    public NamespaceSpec Namespace { get; set; }
    public string Infix { get; set; }

    public string ViewPrefix { get; set; }

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