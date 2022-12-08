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

    public Type? DtoType { get; }

    public DtoModelSpec(Type? type)
    {
        DtoType = type;
    }
}