namespace Gibbs2b.DtoGenerator.Annotation;

using System;

[AttributeUsage(AttributeTargets.Property)]
public class TsOptionalAttribute : Attribute
{
    public bool Nullable { get; set; }
}