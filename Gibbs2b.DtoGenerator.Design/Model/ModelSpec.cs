using System.Reflection;
using System.Text.Json.Serialization;
using Gibbs2b.DtoGenerator.Annotation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Gibbs2b.DtoGenerator.Model;

public class ModelSpec : ITypescriptInterface
{
    private List<PropertySpec> _properties;
    private IEntityType? _entityType;

    public NameSpec Name { get; set; }
    public NamespaceSpec Namespace { get; set; }
    public NamespaceSpec UsingName { get; set; }

    public List<PropertySpec> Properties
    {
        get => _properties;
        set
        {
            _properties = value;

            foreach (var property in value)
            {
                property.Parent = this;
            }

            PropertyMap = value
                .ToDictionary(s => s.Name.CapitalCase);
        }
    }

    public IDictionary<string, PropertySpec> PropertyMap { get; set; }

    public SolutionSpec Solution => Parent.Solution;

    public ProjectSpec Parent { get; set; }

    public Type Type { get; set; }

    public bool NotMapped { get; set; }

    public NameSpec[] Keys { get; set; } = { new() { CapitalCase = "Id" } };

    public string? TableName { get; set; }

    public IEnumerable<string> TypescriptProjects { get; set; } = Array.Empty<string>();

    public ModelSpec()
    {
    }

    public ModelSpec(Type type)
    {
        Type = type;
        Name = new NameSpec { CapitalCase = type.Name };
        Namespace = new NamespaceSpec(type);
        Properties = type
            .GetProperties()
            .Select(p => new PropertySpec(p))
            .ToList();

        var usingName = type.FullName!;
        if (!usingName.StartsWith(type.Namespace!))
            throw new NotImplementedException();

        var attr = type.GetCustomAttribute<GenModelAttribute>();

        TableName = attr?.TableName;

        UsingName = new NamespaceSpec(usingName.Remove(0, type.Namespace!.Length + 1).Replace('+', '.'));
    }

    public PropertySpec? GetPropertyByName(string name)
    {
        return Properties.FirstOrDefault(p => p.Name.CapitalCase == name);
    }

    public void CreateSchema()
    {
        if (Solution == null)
            throw new NullReferenceException();

        foreach (var property in _properties)
        {
            property.CreateSchema();
        }
    }
}