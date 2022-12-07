namespace Gibbs2b.DtoGenerator.Annotation;

/// <summary>
/// Used to mark properties that have been generated. Do not use this.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class GeneratedPropertyAttribute : Attribute
{
}