namespace Gibbs2b.DtoGenerator.Annotation;

/// <summary>
/// Used to mark properties that have been generated. Do not use this.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GeneratedDtoAttribute : Attribute
{
}