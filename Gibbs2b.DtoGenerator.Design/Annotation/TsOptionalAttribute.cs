using Gibbs2b.DTO.Generator.Typescript.Annotation;

namespace Gibbs2b.DtoGenerator.Annotation;

using System;

[Obsolete]
[AttributeUsage(AttributeTargets.Property)]
public class TsOptionalAttribute : DtoOptionalAttribute
{
    /// <summary>
    /// Whether <code>null</code> and <code>undefined</code> should be allowed.
    /// </summary>
    public bool Nullable { get; set; }

    public bool Required { get; set; }
}