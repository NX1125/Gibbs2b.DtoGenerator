using System.Reflection;
using Gibbs2b.DtoGenerator.Annotation;

namespace Gibbs2b.DtoGenerator.Model;

public class TsDtoModelSpec : ModelSpec
{
    public TsDtoSpec Dto { get; }
    public string DtoName { get; set; }

    public TsDtoPropertySpec[] TsProperties { get; protected set; }

    public bool NullableBool { get; set; }

    public TsDtoModelSpec(Type type, TsDtoSpec dto) : base(type)
    {
        Dto = dto;
        DtoName = $"{dto.DtoName}_{type.Name}";
        Project = dto.Project;
        NullableBool = type.GetCustomAttribute<GenTsDtoModelAttribute>()?.NullableBool ?? false;
    }

    internal void LoadTsProperties()
    {
        TsProperties = Type
            .GetProperties()
            .Select(p => new TsDtoPropertySpec(p, this))
            .ToArray();
        Properties = TsProperties
            .Cast<PropertySpec>()
            .ToList();
    }
}