using System.Reflection;
using System.Text.Json;
using Gibbs2b.DtoGenerator.Model;

namespace Gibbs2b.DtoGenerator;

public class SolutionSpec
{
    public string Path { get; set; }

    public ICollection<ProjectSpec> Projects { get; set; } = new List<ProjectSpec>();

    public IDictionary<string, PythonProjectSpec> PythonProjects { get; set; } = new Dictionary<string, PythonProjectSpec>();

    public ICollection<TypescriptProjectSpec> TypescriptProjects { get; set; } = new List<TypescriptProjectSpec>();

    public string? DefaultPythonProjectPath
    {
        get => PythonProjects.TryGetValue("default", out var p) ? p.Path : null;
        set
        {
            if (value != null)
                AddPythonProject("default", value);
        }
    }

    public string? TypescriptProject
    {
        get => TypescriptProjects.SingleOrDefault(p => p.Name == "default")?.Path;
        set
        {
            if (value != null)
                TypescriptProjects.Add(new TypescriptProjectSpec { Name = "default", Path = value });
        }
    }

    public void AddProject(ProjectSpec project)
    {
        project.SourcePath = System.IO.Path.Combine(Path, project.Name.Namespace);
        Projects.Add(project);
    }

    public void AddProject(Assembly assembly)
    {
        var project = new ProjectSpec(assembly);
        AddProject(project);
    }

    public void AddPythonProject(string name, string path)
    {
        PythonProjects[name] = new PythonProjectSpec
        {
            Name = name,
            Path = path,
        };
    }

    public void Solve(bool schema)
    {
        foreach (var project in Projects)
        {
            if (!project.CsprojPath.Exists)
                throw new ArgumentException(project.CsprojPath.FullName);

            project.Solution = this;
            if (schema)
                project.CreateSchema();

            foreach (var model in project.Models)
            {
                model.Parent = project;
            }

            foreach (var model in project.Dto
                         .OrderByDescending(d => d.Options.IsView))
            {
                model.Parent = project;
                model.SolveRelations();
            }

            foreach (var dto in project.Dto)
            {
                foreach (var model in dto.Models)
                {
                    model.ExpandImplicitProperties();
                }
            }
        }
    }

    public void CreateSchema()
    {
        Solve(true);

        // using var stream = File.Create(System.IO.Path.Combine(Path, "generator-schema.json"));
        // JsonSerializer.Serialize(stream, this, new JsonSerializerOptions
        // {
        //     WriteIndented = true,
        // });
    }

    public ModelSpec? GetModel<TModel>()
    {
        return GetModel(typeof(TModel));
    }

    public ModelSpec? GetModel(Type type)
    {
        return Projects
            .SelectMany(project => project.Models)
            .FirstOrDefault(model => model.Name.CapitalCase == type.Name);
    }

    public ModelSpec? GetModel(string name)
    {
        return Projects
            .SelectMany(project => project.Models)
            .FirstOrDefault(model => model.Name.CapitalCase == name);
    }

    public EnumSpec? GetEnum(Type type)
    {
        return Projects
            .SelectMany(project => project.Enums)
            .FirstOrDefault(model => model.Name.CapitalCase == type.Name);
    }

    public EnumSpec? GetEnum(string name)
    {
        return Projects
            .SelectMany(project => project.Enums)
            .FirstOrDefault(model => model.Name.CapitalCase == name);
    }
}