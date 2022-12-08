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
        project.Solution = this;
        project.SourcePath = System.IO.Path.Combine(Path, project.Name.Namespace);
        Projects.Add(project);
    }

    public void AddProject(Assembly assembly)
    {
        var project = new ProjectSpec(assembly, this);
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
}