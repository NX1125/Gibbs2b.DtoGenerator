using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Gibbs2b.DtoGenerator.Model;

public class NameSpec
{
    private static readonly Regex UppercaseRegex = new(@"[A-Z]");
    private static readonly Regex WhitespaceRegex = new(@"\s+");

    [JsonIgnore]
    public string[] Parts { get; set; } = null!;

    public string CapitalCase
    {
        get => string.Join("", Parts.Select(Capitalize));
        set
        {
            Parts = WhitespaceRegex
                .Split(UppercaseRegex
                    .Replace(value, m => ' ' + m.Value)
                    .Trim());
            if (Parts.Length == 0 || Parts.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentOutOfRangeException(null, value, null);
        }
    }

    [JsonIgnore]
    public string SnakeCaseName => string.Join('_', Parts.Select(p => p.ToLower()));

    [JsonIgnore]
    public string CamelCase => string.Join("", Parts
        .Skip(1)
        .Select(Capitalize)
        .Prepend(Parts[0].ToLower()));

    public string KebabCase => string.Join('-', Parts.Select(p => p.ToLower()));

    [Obsolete]
    public NameSpec()
    {
    }

    public NameSpec(string name)
    {
        CapitalCase = name;
    }

    public NameSpec(string[] parts)
    {
        Parts = parts;
    }

    public override string ToString()
    {
        return CapitalCase;
    }

    public override bool Equals(object? obj)
    {
        return obj is NameSpec name && name.CapitalCase == CapitalCase;
    }

    public override int GetHashCode()
    {
        return CapitalCase.GetHashCode();
    }

    public NameSpec Append(params string[] parts)
    {
        return new NameSpec(Parts
            .Concat(parts)
            .ToArray());
    }

    private static string Capitalize(string s)
    {
        return char.ToUpper(s[0]) + s.Substring(1).ToLower();
    }

    public NameSpec RemoveSuffix(string suffix)
    {
        return suffix.Equals(Parts[^1], StringComparison.OrdinalIgnoreCase)
            ? new NameSpec(Parts.SkipLast(1).ToArray())
            : this;
    }
}