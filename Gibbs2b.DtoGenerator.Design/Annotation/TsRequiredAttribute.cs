namespace Gibbs2b.DtoGenerator.Annotation;

using System;

[AttributeUsage(AttributeTargets.Property)]
public class TsRequiredAttribute : TsOptionalAttribute
{
    public TsRequiredAttribute()
    {
        Required = true;
    }
}