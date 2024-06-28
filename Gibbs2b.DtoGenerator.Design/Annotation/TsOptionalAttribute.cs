namespace Gibbs2b.DtoGenerator.Annotation;

using System;

[AttributeUsage(AttributeTargets.Property)]
public class TsOptionalAttribute : Attribute
{
    /// <summary>
    /// Whether <code>null</code> and <code>undefined</code> should be allowed.
    /// </summary>
    public bool Nullable { get; set; }

    public bool Required { get; set; }
}