using System.Collections;
using System.Reflection;

namespace Gibbs2b.DtoGenerator.Model;

public abstract class TypeSpec
{
    private static readonly NullabilityInfoContext NullabilityContext = new();
    // a type has a name, generic arguments, and a nullable flag. Especial case for enums, models and arrays

    public Type ClrType { get; set; } = null!;

    public static TypeSpec Parse(Type type, ModelSpec model, PropertyInfo? prop = null)
    {
        if (prop != null && NullabilityContext.Create(prop).WriteState is NullabilityState.Nullable)
            return new NullableTypeSpec(Parse(type, model)) { ClrType = type };

        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
            return new NullableTypeSpec(Parse(underlyingType, model)) { ClrType = type };

        if (type.IsArray)
        {
            var itemType = type.GetElementType()!;
            return new ArrayTypeSpec(Parse(itemType, model)) { ClrType = type };
        }

        var genericTypeDefinition = GetGenericTypeDefinition(type);
        if (genericTypeDefinition != null)
        {
            var enumerableType = GetEnumerableType(genericTypeDefinition);

            if (enumerableType != null)
            {
                var itemType = type.GetGenericArguments()[0];
                return new EnumerableTypeSpec(enumerableType.Value, Parse(itemType, model)) { ClrType = type };
            }

            if (genericTypeDefinition == typeof(Dictionary<,>) ||
                genericTypeDefinition == typeof(IDictionary<,>))
            {
                var keyType = type.GetGenericArguments()[0];
                var valueType = type.GetGenericArguments()[1];
                return new DictionaryTypeSpec(Parse(keyType, model), Parse(valueType, model)) { ClrType = type };
            }

            throw new NotImplementedException();
        }

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
        if (type.FullName == "NpgsqlTypes.NpgsqlTsVector")
            return new PrimitiveTypeSpec(TypeNameEnum.TsVector) { ClrType = type };

        return new LazyTypeSpec(model, type);
    }

    public abstract TypeSpec Solve();

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

    public class PrimitiveTypeSpec : TypeSpec
    {
        public TypeNameEnum Type { get; set; }

        public PrimitiveTypeSpec(TypeNameEnum type)
        {
            Type = type;
        }

        public override TypeSpec Solve()
        {
            return this;
        }
    }

    public class NullableTypeSpec : TypeSpec
    {
        public TypeSpec BaseType { get; set; }

        public NullableTypeSpec(TypeSpec baseType)
        {
            BaseType = baseType;
        }

        public override TypeSpec Solve()
        {
            BaseType = BaseType.Solve();
            return this;
        }
    }

    public class ArrayTypeSpec : TypeSpec
    {
        public TypeSpec BaseType { get; set; }
        public int Rank { get; set; }

        public ArrayTypeSpec(TypeSpec baseType)
        {
            BaseType = baseType;
        }

        public override TypeSpec Solve()
        {
            BaseType = BaseType.Solve();
            return this;
        }
    }

    public class EnumerableTypeSpec : TypeSpec
    {
        public EnumerableType EnumerableType { get; set; }
        public TypeSpec BaseType { get; set; }

        public EnumerableTypeSpec(EnumerableType enumerableType, TypeSpec baseType)
        {
            EnumerableType = enumerableType;
            BaseType = baseType;
        }

        public override TypeSpec Solve()
        {
            BaseType = BaseType.Solve();
            return this;
        }
    }

    public class DictionaryTypeSpec : TypeSpec
    {
        public TypeSpec KeyType { get; set; }
        public TypeSpec ValueType { get; set; }

        public DictionaryTypeSpec(TypeSpec keyType, TypeSpec valueType)
        {
            KeyType = keyType;
            ValueType = valueType;
        }

        public override TypeSpec Solve()
        {
            KeyType = KeyType.Solve();
            ValueType = ValueType.Solve();
            return this;
        }
    }

    public class LazyTypeSpec : TypeSpec
    {
        public ModelSpec ParentModel { get; set; } = null!;
        public TypeSpec Solved { get; set; }

        public LazyTypeSpec(ModelSpec parentModel, Type clrType)
        {
            ParentModel = parentModel;
            ClrType = clrType;

            var project = ParentModel.Project;

            var model = project.GetModel(ClrType);
            if (model != null)
            {
                Solved = new ModelTypeSpec
                {
                    Model = model,
                    ClrType = ClrType,
                };
                return;
            }

            var enumSpec = project.GetEnum(ClrType);
            if (enumSpec != null)
            {
                Solved = new EnumTypeSpec
                {
                    Enum = enumSpec,
                    ClrType = ClrType,
                };
                return;
            }

            // public TsDtoModelSpec? TsTypeModel => ParentDto.Dto.Models
            // .SingleOrDefault(t => t.Type == BaseType);
            // var dto = ;

            throw new NotImplementedException(clrType.FullName);
        }

        public override TypeSpec Solve()
        {
            throw new NotImplementedException();
        }
    }

    public class ModelTypeSpec : TypeSpec
    {
        public ModelSpec Model { get; set; } = null!;

        public override TypeSpec Solve()
        {
            return this;
        }
    }

    public class DtoModelTypeSpec : TypeSpec
    {
        public DtoModelSpec Model { get; set; } = null!;

        public override TypeSpec Solve()
        {
            return this;
        }
    }

    public class EnumTypeSpec : TypeSpec
    {
        public EnumSpec Enum { get; set; } = null!;

        public override TypeSpec Solve()
        {
            return this;
        }
    }
}