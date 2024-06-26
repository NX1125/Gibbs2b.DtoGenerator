using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Gibbs2b.DtoGenerator.Model;

public abstract class TsTypeSpec
{
    private static readonly NullabilityInfoContext NullabilityContext = new();
    // a type has a name, generic arguments, and a nullable flag. Especial case for enums, models and arrays

    public Type ClrType { get; set; } = null!;

    public abstract IEnumerable<TsTypeSpec> GetChildren();

    public static TsTypeSpec Parse(Type type, TsDtoModelSpec parentModel, TsDtoPropertySpec prop)
    {
        if (NullabilityContext.Create(prop.Property).WriteState is not NullabilityState.Nullable)
            return ParseType(type, parentModel, prop);

        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        return new NullableTypeSpec(ParseType(type, parentModel, prop)) { ClrType = type };
    }

    private static TsTypeSpec ParseType(Type type, TsDtoModelSpec parentModel, TsDtoPropertySpec prop)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
            return new NullableTypeSpec(ParseType(underlyingType, parentModel, prop)) { ClrType = type };

        if (type.IsArray)
        {
            var itemType = type.GetElementType()!;
            return new ArrayTypeSpec(ParseType(itemType, parentModel, prop))
            {
                ClrType = type,
                Rank = type.GetArrayRank(),
            };
        }

        var genericTypeDefinition = GetGenericTypeDefinition(type);
        if (genericTypeDefinition != null)
        {
            var enumerableType = GetEnumerableType(genericTypeDefinition);

            if (enumerableType != null)
            {
                var itemType = type.GetGenericArguments()[0];
                return new EnumerableTypeSpec(enumerableType.Value, ParseType(itemType, parentModel, prop)) { ClrType = type };
            }

            if (genericTypeDefinition == typeof(Dictionary<,>) ||
                genericTypeDefinition == typeof(IDictionary<,>))
            {
                var keyType = type.GetGenericArguments()[0];
                var valueType = type.GetGenericArguments()[1];
                return new DictionaryTypeSpec(ParseType(keyType, parentModel, prop), ParseType(valueType, parentModel, prop)) { ClrType = type };
            }

            var argument = type
                .GetGenericArguments()
                .Single();

            return new LazyGenericArgumentSpec
            {
                Argument = ParseType(argument, parentModel, prop),
                Property = prop.Property,
                Container = ParseType2(genericTypeDefinition, parentModel, prop),
            };
        }

        return ParseType2(type, parentModel, prop);
    }

    private static TsTypeSpec ParseType2(Type type, TsDtoModelSpec parentModel, TsDtoPropertySpec prop)
    {
        if (type == typeof(int))
            return new PrimitiveTypeSpec(TypeNameEnum.Int) { ClrType = type };
        if (type == typeof(float))
            return new PrimitiveTypeSpec(TypeNameEnum.Float) { ClrType = type };
        if (type == typeof(long))
            return new PrimitiveTypeSpec(TypeNameEnum.Long) { ClrType = type };
        if (type == typeof(double))
            return new PrimitiveTypeSpec(TypeNameEnum.Double) { ClrType = type };
        if (type == typeof(string))
            return new PrimitiveTypeSpec(TypeNameEnum.String) { ClrType = type };
        if (type == typeof(DateTime))
            return new PrimitiveTypeSpec(TypeNameEnum.DateTime) { ClrType = type };
        if (type == typeof(decimal))
            return new PrimitiveTypeSpec(TypeNameEnum.Decimal) { ClrType = type };
        if (type == typeof(bool))
            return new PrimitiveTypeSpec(TypeNameEnum.Bool) { ClrType = type };
        if (type == typeof(Guid))
            return new PrimitiveTypeSpec(TypeNameEnum.Guid) { ClrType = type };
        if (type == typeof(object))
            return new PrimitiveTypeSpec(TypeNameEnum.Object) { ClrType = type };
        if (type.FullName == "NpgsqlTypes.NpgsqlTsVector")
            return new PrimitiveTypeSpec(TypeNameEnum.TsVector) { ClrType = type };

        if (type == typeof(IFormFile))
            return new JsTypeSpec { Type = JsType.Blob };

        if (type.IsGenericParameter)
        {
            return new GenericTypeSpec { GenericTypeName = type.Name };
        }

        var project = parentModel.Project;

        if (project.TsOpaqueModels.TryGetValue(type, out var opaqueType))
        {
            return new OpaqueTypeSpec
            {
                BaseType = opaqueType,
                ClrType = type,
            };
        }

        var enumSpec = project.GetOrLoadEnum(type);
        if (enumSpec != null)
        {
            return new EnumTypeSpec
            {
                Enum = enumSpec,
                ClrType = type,
            };
        }

        LazyTypeSpec lazy = new()
        {
            Type = type,
            ParentModel = parentModel,
            Info = prop,
        };
        parentModel.LazyTypeSpecs.Add(lazy);
        return lazy;
    }

    private static EnumerableType? GetEnumerableType(Type type)
    {
        if (type == typeof(List<>) || type == typeof(IList<>) || type == typeof(IList))
            return EnumerableType.List;

        if (type == typeof(IEnumerable<>) || type == typeof(IEnumerable))
            return EnumerableType.Enumerable;

        if (type == typeof(ICollection<>) || type == typeof(ICollection))
            return EnumerableType.Collection;

        return null;
    }

    private static Type? GetGenericTypeDefinition(Type type)
    {
        try
        {
            return type.GetGenericTypeDefinition();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public class PrimitiveTypeSpec : TsTypeSpec
    {
        public TypeNameEnum Type { get; set; }

        public PrimitiveTypeSpec(TypeNameEnum type)
        {
            Type = type;
        }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield break;
        }
    }

    public class NullableTypeSpec : TsTypeSpec
    {
        public TsTypeSpec BaseType { get; set; }

        public NullableTypeSpec(TsTypeSpec baseType)
        {
            BaseType = baseType;
        }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield return BaseType;
        }
    }

    public class ArrayTypeSpec : TsTypeSpec
    {
        public TsTypeSpec BaseType { get; set; }
        public int Rank { get; set; }

        public ArrayTypeSpec(TsTypeSpec baseType)
        {
            BaseType = baseType;
        }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield return BaseType;
        }
    }

    public class EnumerableTypeSpec : TsTypeSpec
    {
        public EnumerableType EnumerableType { get; set; }
        public TsTypeSpec BaseType { get; set; }

        public EnumerableTypeSpec(EnumerableType enumerableType, TsTypeSpec baseType)
        {
            EnumerableType = enumerableType;
            BaseType = baseType;
        }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield return BaseType;
        }
    }

    public class DictionaryTypeSpec : TsTypeSpec
    {
        public TsTypeSpec KeyType { get; set; }
        public TsTypeSpec ValueType { get; set; }

        public DictionaryTypeSpec(TsTypeSpec keyType, TsTypeSpec valueType)
        {
            KeyType = keyType;
            ValueType = valueType;
        }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield return KeyType;
            yield return ValueType;
        }
    }

    public class DtoModelTypeSpec : TsTypeSpec
    {
        public TsDtoModelSpec Model { get; set; } = null!;

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield break;
        }
    }

    public class EnumTypeSpec : TsTypeSpec
    {
        public EnumSpec Enum { get; set; } = null!;

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield break;
        }
    }

    public class OpaqueTypeSpec : TsTypeSpec
    {
        public TsDtoOpaqueModelSpec BaseType { get; set; }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield break;
        }
    }

    public class LazyGenericArgumentSpec : TsTypeSpec
    {
        public PropertyInfo Property { get; set; } = null!;
        public TsTypeSpec Container { get; set; } = null!;
        public TsTypeSpec Argument { get; set; } = null!;

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield return Container;
            yield return Argument;
        }
    }

    public class LazyTypeSpec : TsTypeSpec
    {
        public Type Type { get; set; } = null!;
        public TsDtoModelSpec ParentModel { get; set; } = null!;

        public DtoModelTypeSpec Model { get; set; } = null!;

        public TsDtoPropertySpec Info { get; set; }

        public void Solve()
        {
            if (Info.Options.JsonIgnore == JsonIgnoreCondition.Always)
                return;

            var model = ParentModel.Project.GetOrLoadModel(Type);
            if (model == null)
            {
                throw new InvalidOperationException($"Model not found for {Type.FullName} in {ParentModel.Dto.Type.FullName}.{Info.Name}");
            }

            Model = new DtoModelTypeSpec
            {
                Model = model,
                ClrType = Type,
            };
        }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            if (Model == null)
                throw new InvalidOperationException("Lazy type not solved");

            yield return Model;
        }
    }

    public class GenericTypeSpec : TsTypeSpec
    {
        public string GenericTypeName { get; set; } = null!;

        public bool IsArgument { get; set; }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield break;
        }
    }

    public class GenericModelSpec : TsTypeSpec
    {
        public TsTypeSpec Model { get; set; } = null!;
        public TsTypeSpec[] GenericArguments { get; set; } = null!;

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield return Model;
            foreach (var arg in GenericArguments)
            {
                yield return arg;
            }
        }
    }

    public class JsTypeSpec : TsTypeSpec
    {
        public JsType Type { get; set; }

        public override IEnumerable<TsTypeSpec> GetChildren()
        {
            yield break;
        }
    }

    public enum JsType
    {
        Blob,
    }
}