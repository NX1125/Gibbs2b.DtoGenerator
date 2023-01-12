using System.Reflection;
using System.Text.Json.Serialization;

namespace Gibbs2b.DtoGenerator.Model;

public class NamespaceSpec
{
    public static readonly NamespaceSpec SystemNamespace = new("System");
    public static readonly NamespaceSpec GenericCollections = new("System.Collections.Generic");
    public static readonly NamespaceSpec DataAnnotations = new("System.ComponentModel.DataAnnotations");
    public static readonly NamespaceSpec NpgsqlTypes = new("NpgsqlTypes");
    public static readonly NamespaceSpec EntityFrameworkCore = new("Microsoft.EntityFrameworkCore");

    [JsonIgnore]
    public NameSpec[] Parts { get; set; } = null!;

    [JsonIgnore]
    public string KebabCasePath => Path.Combine(Parts.Select(p => p.KebabCase).ToArray());

    [Obsolete]
    public NamespaceSpec()
    {
    }

    public NamespaceSpec(string ns)
    {
        Namespace = ns;
    }

    public NamespaceSpec(Type type) : this(type.Namespace!)
    {
    }

    public NamespaceSpec(Assembly assembly) : this(assembly.GetName().Name!)
    {
    }

    public NamespaceSpec(params string[] parts)
    {
        Parts = parts
            .Select(p => new NameSpec { CapitalCase = p })
            .ToArray();
    }

    public NamespaceSpec(IEnumerable<NameSpec> ns)
    {
        Parts = ns.ToArray();
    }

    public string Namespace
    {
        get => string.Join(".", Parts.Select(p => p.CapitalCase));
        set
        {
            Parts = value
                .Split('.')
                .Select(n => new NameSpec { CapitalCase = n })
                .ToArray();
            if (Parts.Length <= 0)
                throw new ArgumentException();
        }
    }

    public static readonly NamespaceSpec Linq = new() { Namespace = "System.Linq" };

    public override string ToString()
    {
        return Namespace;
    }

    public override bool Equals(object? obj)
    {
        return obj is NamespaceSpec n && n.Namespace == Namespace;
    }

    public override int GetHashCode()
    {
        return Namespace.GetHashCode();
    }

    public string GetPath(NamespaceSpec baseNamespace)
    {
        return GetPath(baseNamespace.Namespace);
    }

    public string GetPath(string bs)
    {
        var ns = Namespace;

        if (!ns.StartsWith(bs))
            throw new ArgumentException($"{ns} does not start with {bs}");

        return string.Join("/", ns
            .Remove(0, bs.Length + 1)
            .Split('.')
            .Prepend(bs));
    }

    public bool StartsWith(NamespaceSpec ns)
    {
        return Namespace == ns.Namespace || Namespace.StartsWith($"{ns}.");
    }

    public NamespaceSpec Append(NameSpec name)
    {
        return new NamespaceSpec(Parts.Append(name));
    }
}