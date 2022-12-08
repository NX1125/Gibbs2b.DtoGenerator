using System.Reflection;

namespace Gibbs2b.DtoGenerator.Model;

public class TsDtoPropertySpec : PropertySpec
{
    public TsDtoModelSpec ParentDto { get; }

    public TsDtoModelSpec? TsTypeModel => ParentDto.Dto.Models
        .SingleOrDefault(t => t.Type == BaseType);

    public override bool IsModel => ParentDto.Dto.Models is { Length: >= 0 } && TsTypeModel != null;

    public TsDtoPropertySpec(PropertyInfo prop, TsDtoModelSpec parent) : base(prop, parent)
    {
        ParentDto = parent;
    }
}