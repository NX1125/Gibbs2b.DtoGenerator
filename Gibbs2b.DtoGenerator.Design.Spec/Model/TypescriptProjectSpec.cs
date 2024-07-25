using System.Text.Json.Serialization;

namespace Gibbs2b.DtoGenerator.Model;

public class TypescriptProjectSpec
{
    public string Name { get; set; } = null!;
    public string[] ProjectPaths { get; set; } = Array.Empty<string>();

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

    public string DefaultDtoPath { get; set; } = null!;
    public string? DefaultEnumPath { get; set; }
    public string? EnumPathPrefix { get; set; }
    public string DefaultHandlerPath { get; set; } = null!;
    public bool IsFetch { get; set; }
    public bool Disabled { get; set; }
}