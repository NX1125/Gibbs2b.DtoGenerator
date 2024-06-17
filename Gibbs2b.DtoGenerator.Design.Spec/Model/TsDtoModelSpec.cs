using System.Reflection;
using Gibbs2b.DtoGenerator.Annotation;
using Gibbs2b.DtoGenerator.Design.Config;

namespace Gibbs2b.DtoGenerator.Model;

public class TsDtoModelSpec
{
    public Type Type { get; }

    public TsDtoSpec Dto { get; }
    public string DtoName { get; set; }

    public TsDtoPropertySpec[] TsProperties { get; protected set; } = null!;

    public bool NullableBool { get; set; }

    public ProjectSpec Project { get; set; }

    public List<TsTypeSpec.LazyTypeSpec> LazyTypeSpecs => Project.LazyTypeSpecs;

    public int Index
    {
        get
        {
            if (Dto.Type == Type)
                return 0;

            var i = Dto.NestedTypes.IndexOf(Type);
            if (i == -1)
            {
                throw new InvalidOperationException($"Type {Type.Name} not found in Dto {Dto.DtoName}");
            }

            return i;
        }
    }

    public string? GenericTypeName { get; set; }

    public TsDtoModelSpec(Type type, TsDtoSpec dto)
    {
        Type = type;
        Dto = dto;
        DtoName = $"{dto.DtoName}_{type.Name}";
        Project = dto.Project;
        ModelAttribute = type.GetCustomAttribute<GenTsDtoModelAttribute>();
        NullableBool = ModelAttribute?.NullableBool ?? false;
        // get parameter name, if any
        GenericTypeName = type.IsGenericType
            ? type
                .GetGenericArguments()
                .Select(x => x.Name)
                .SingleOrDefault()
            : null;
        if (GenericTypeName != null)
        {
            var baseName = type.Name.Split('`')[0];
            DtoName = $"{dto.DtoName}_{baseName}";
        }
    }

    public GenTsDtoModelAttribute? ModelAttribute { get; set; }

    internal void LoadTsProperties()
    {
        TsProperties = Type
            .GetProperties()
            .Select(p => new TsDtoPropertySpec(p, this))
            .ToArray();
    }

    public override string ToString()
    {
        return DtoName;
    }
}