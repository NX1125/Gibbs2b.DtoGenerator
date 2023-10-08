using System.Reflection;
using System.Text.Json.Serialization;
using Gibbs2b.DtoGenerator.Annotation;
using Gibbs2b.DtoGenerator.Design.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Gibbs2b.DtoGenerator.Model;

public class ModelSpec : ITypescriptInterface
{
    public NameSpec Name { get; set; }
    public NamespaceSpec Namespace { get; set; }
    public NamespaceSpec UsingName { get; set; }

    public List<PropertySpec> Properties { get; protected set; } = null!;

    public SolutionSpec Solution => Project.Solution;

    public ProjectSpec Project { get; set; } = null!;

    public Type Type { get; set; }

    public bool NotMapped { get; set; }

    public IEnumerable<PropertySpec> PrimaryKeys => Array.Empty<PropertySpec>();

    public string? TableName { get; set; }

    public IEnumerable<string> TypescriptProjects { get; set; } = Array.Empty<string>();

    public ModelSpec(Type type)
    {
        Type = type;
        Name = new(type.Name);
        Namespace = new NamespaceSpec(type);

        var usingName = type.FullName!;
        if (!usingName.StartsWith(type.Namespace!))
            throw new NotImplementedException();

        var attr = type.GetCustomAttribute<GenModelAttribute>();

        TableName = attr?.TableName;

        UsingName = new NamespaceSpec(usingName.Remove(0, type.Namespace!.Length + 1).Replace('+', '.'));
    }

    internal void LoadProperties()
    {
        Properties = Type
            .GetProperties()
            .Select(p => new PropertySpec(p, this))
            .ToList();
    }

    public PropertySpec? GetPropertyByName(string name)
    {
        return Properties.FirstOrDefault(p => p.Name.CapitalCase == name);
    }

    public PropertySpec? FindProperty(MemberInfo? prop)
    {
        return Properties.SingleOrDefault(p => p.PropertyInfo == prop);
    }
}