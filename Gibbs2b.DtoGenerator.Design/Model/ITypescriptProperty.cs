namespace Gibbs2b.DtoGenerator.Model;

public interface ITypescriptProperty
{
    public PropertyOptions Options { get; }
    public TypeNameEnum TypeNameType { get; }
    public EnumerableType EnumerableType { get; }
    public string? EnumName { get; }
    public string DtoTsName { get; }
    public NameSpec PropertyName { get; }
}