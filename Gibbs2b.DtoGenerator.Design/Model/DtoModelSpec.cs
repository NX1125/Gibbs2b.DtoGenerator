using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using Gibbs2b.DtoGenerator.Typescript;

namespace Gibbs2b.DtoGenerator.Model;

public class DtoModelSpec
{
    private IList<DtoPropertySpec> _properties;
    private ModelSpec _model;
    private NameSpec? _dtoName;

    [JsonIgnore]
    public ModelSpec Model
    {
        get => _model;
        set
        {
            _model = value;
            ModelName = value.Name;
            NotMapped = value.NotMapped;
        }
    }

    public NameSpec ModelName { get; set; }

    public NameSpec DtoName
    {
        get => _dtoName ?? ModelName;
        set => _dtoName = value;
    }

    public string TsName => $"{Parent.Name}_{DtoName.RemoveSuffix("Dto")}";

    public IList<DtoPropertySpec> Properties
    {
        get => _properties;
        set
        {
            _properties = value;

            foreach (var property in value)
            {
                property.Parent = this;
            }
        }
    }

    public string? DbViewName { get; set; }

    [JsonIgnore]
    public DtoSpec Parent { get; set; } = null!;

    [JsonIgnore]
    public SolutionSpec Solution => Parent.Solution;

    public string? TableName { get; set; }

    public bool NotMapped { get; set; }

    public IList<PropertySpec> TsProperties { get; set; } = Array.Empty<PropertySpec>();

    public DtoSpecFactory Factory => Parent.Factory;

    public Type? DtoType => Factory
        .GetType()
        .GetNestedTypes()
        .SingleOrDefault(t => t.Name == DtoName.CapitalCase);

    public void SolveNames()
    {
        Model = Parent.Solution.GetModel(ModelName.CapitalCase)!;

        if (Model == null)
            throw new ArgumentNullException();

        foreach (var property in _properties)
        {
            property.Parent = this;
            property.SolveRelations();
        }
    }

    private IEnumerable<DtoPropertySpec> ExpandImplicitProperty(DtoPropertySpec prop)
    {
        yield return prop;

        var id = prop.Property.Id;
        if (!Parent.IsView || id == null)
            yield break;

        yield return new DtoPropertySpec(id, this);
    }

    public void ExpandImplicitProperties()
    {
        // _properties = _properties
        //     .SelectMany(ExpandImplicitProperty)
        //     .DistinctBy(p => p.PropertyName)
        //     .ToArray();
    }

    public void SolveTsFields()
    {
        var type = DtoType;
        if (type == null)
            return;

        // var names = _properties
        //     .Select(p => p.PropertyName.CapitalCase)
        //     .ToHashSet();
        //
        // TsProperties = type
        //     .GetProperties()
        //     .Where(p => p.GetCustomAttribute<GenTsPropertyAttribute>() != null && !names.Contains(p.Name))
        //     .Select(p => new PropertySpec(p) { _solution = Solution })
        //     .ToArray();
    }
}