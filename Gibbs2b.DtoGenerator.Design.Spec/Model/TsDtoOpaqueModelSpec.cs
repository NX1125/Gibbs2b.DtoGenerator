using System.Reflection;
using Gibbs2b.DtoGenerator.Annotation;
using Gibbs2b.DtoGenerator.Design.Config;

namespace Gibbs2b.DtoGenerator.Model;

public class TsDtoOpaqueModelSpec
{
    public TsDtoOpaqueModelSpec(Type type, ProjectSpec project)
    {
        var attr = type.GetCustomAttribute<GenTsDtoOpaqueModelAttribute>()!;

        Project = project;
        Attr = attr;
        Type = type;
    }

    public GenTsDtoOpaqueModelAttribute Attr { get; set; }
    public Type Type { get; set; }
    public ProjectSpec Project { get; set; }
}