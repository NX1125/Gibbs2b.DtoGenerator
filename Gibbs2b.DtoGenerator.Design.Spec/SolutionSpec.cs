using System.Reflection;
using System.Text.Json;
using Gibbs2b.DtoGenerator.Design.Config;
using Gibbs2b.DtoGenerator.Model;

namespace Gibbs2b.DtoGenerator;

public class SolutionSpec
{
    /// <summary>
    /// Base path for all deserialized paths.
    /// </summary>
    public string? CommonPath { get; set; }

    public string Path { get; set; } = null!;

    public IDictionary<string, PythonProjectSpec> PythonProjects { get; set; } =
        new Dictionary<string, PythonProjectSpec>();

    public ICollection<TypescriptProjectSpec> TypescriptProjects { get; set; } = new List<TypescriptProjectSpec>();

    /// <summary>
    /// Currently only one project is supported.
    /// </summary>
    public ProjectSpec Project { get; set; }

    public string? DefaultPythonProjectPath
    {
        get => PythonProjects.TryGetValue("default", out var p) ? p.Path : null;
        set
        {
            if (value != null)
                AddPythonProject("default", value);
        }
    }

    public void AddProject(ProjectSpec project)
    {
        if (Project != null)
            throw new Exception("Only one project is currently supported");

        project.Solution = this;
        project.SourcePath = System.IO.Path.Combine(Path, project.Name.Namespace);

        Project = project;
    }

    public void AddProject(Assembly assembly)
    {
        var project = new ProjectSpec(assembly, this);
        AddProject(project);
    }

    public void AddProject(Type type)
    {
        AddProject(type.Assembly);
    }

    public void AddProject<TProgram>()
    {
        AddProject(typeof(TProgram));
    }

    public void AddPythonProject(string name, string path)
    {
        PythonProjects[name] = new PythonProjectSpec
        {
            Name = name,
            Path = path,
        };
    }

    public static SolutionSpec FromJson<TGenerator>(string? filename = null)
    {
        // from json resource
        var type = typeof(TGenerator);
        filename ??= $"{type.Namespace}.dtosettings.json";

        using var stream = type.Assembly.GetManifestResourceStream(filename);

        if (stream == null)
            throw new Exception($"Resource not found: {filename}");

        var data = JsonSerializer.Deserialize<SolutionSpec>(stream);
        if (data == null)
            throw new Exception($"Failed to deserialize: {filename}");

        if (data.CommonPath != null)
            data.UpdateBasePaths();

        return data;
    }

    private void UpdateBasePaths()
    {
        Path = System.IO.Path.Combine(CommonPath!, Path);
        foreach (var project in TypescriptProjects)
        {
            for (var i = 0; i < project.Paths.Length; i++)
            {
                project.Paths[i] = System.IO.Path.Combine(CommonPath!, project.Paths[i]);
            }
        }
    }
}