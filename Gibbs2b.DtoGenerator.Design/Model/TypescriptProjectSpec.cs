using System.Text.Json.Serialization;

namespace Gibbs2b.DtoGenerator.Model;

public class TypescriptProjectSpec
{
    public string Name { get; set; } = null!;
    public string[] Paths { get; set; } = Array.Empty<string>();

    [JsonIgnore]
    public NamespaceSpec Namespace
    {
        init => Namespaces = new[] { value };
    }

    [JsonIgnore]
    public NamespaceSpec[] Namespaces { get; set; } = Array.Empty<NamespaceSpec>();

    /// <summary>
    /// For JSON only.
    /// </summary>
    [JsonPropertyName("Namespaces")]
    public string[] NamespaceStrings
    {
        get => Namespaces.Select(x => x.ToString()).ToArray();
        set => Namespaces = value.Select(x => new NamespaceSpec(x)).ToArray();
    }
}