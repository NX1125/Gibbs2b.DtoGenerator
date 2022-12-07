using System.Text.Json.Serialization;

namespace Gibbs2b.DtoGenerator.Model;

public class DtoSpec : ITypescriptInterface
{
    private ICollection<DtoModelSpec> _models = new List<DtoModelSpec>();

    public ICollection<DtoModelSpec> Models
    {
        get => _models;
        set
        {
            _models = value;

            foreach (var model in value)
            {
                model.Parent = this;
            }
        }
    }

    [JsonIgnore]
    public IEnumerable<DtoModelSpec> MappedModels => _models
        .Where(m => !m.NotMapped);

    [JsonIgnore]
    public ProjectSpec Parent { get; internal set; }

    [JsonIgnore]
    public SolutionSpec Solution => Parent.Solution;

    public DtoOptions Options { get; set; }

    public bool IsView => Options.IsView;

    public NamespaceSpec Namespace { get; set; }
    public NameSpec Name { get; set; }

    public string CsPath => Path.Combine(Namespace.GetPath(Parent.Name), $"{Name.CapitalCase}.cs");
    public string FullCsPath => Path.Combine(Solution.Path, CsPath);
    public DtoSpecFactory Factory { get; init; }

    public void SolveRelations()
    {
        if (Parent == null)
            throw new ArgumentNullException();

        foreach (var model in _models)
        {
            model.Parent = this;
            model.SolveNames();
        }
    }

    public DtoModelSpec? FindDtoByModel(ModelSpec model)
    {
        return _models.FirstOrDefault(dto => dto.ModelName.Equals(model.Name));
    }

    public void SolveTsFields()
    {
        foreach (var model in _models)
        {
            model.Parent = this;
            model.SolveTsFields();
        }
    }
}

public class DtoOptions
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CopyToModel { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CopyFromModel { get; set; }

    public ICollection<string> PythonProjects { get; set; } = new List<string>();

    public ICollection<string> TypescriptProjects { get; set; } = new List<string>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsView { get; set; }
}