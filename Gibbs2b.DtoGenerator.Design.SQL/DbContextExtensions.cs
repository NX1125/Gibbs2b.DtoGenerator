using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NpgsqlTypes;

namespace Gibbs2b.DtoGenerator.Design.SQL;

public static class DbContextExtensions
{
    public static void HasTsVector<TEntity>(this ModelBuilder modelBuilder,
        Expression<Func<TEntity, NpgsqlTsVector>> field,
        Expression<Func<TEntity, object>> include,
        Expression<Func<TEntity, object?>> index,
        string config = "english") where TEntity : class
    {
        modelBuilder
            .Entity<TEntity>()
            .HasGeneratedTsVectorColumn(
                field,
                config,
                include)
            .HasIndex(index)
            .HasMethod("GIN");
    }

    public static void HasTsVector<TEntity>(this ModelBuilder modelBuilder,
        Expression<Func<TEntity, NpgsqlTsVector>> field,
        Expression<Func<TEntity, object>> include,
        string config = "english") where TEntity : class
    {
        LambdaExpression lambda = field;

        modelBuilder.HasTsVector(
            field,
            include,
            Expression.Lambda<Func<TEntity, object?>>(lambda.Body, false, lambda.Parameters),
            config);
    }

    public static HashSet<Type> CallOnModelCreatingForDbSets<TContext>(this TContext context, ModelBuilder modelBuilder)
        where TContext : DbContext
    {
        // For each DbSet property, call the following method when available
        // public static void OnModelCreating(ModelBuilder modelBuilder)

        var dbSets = context
            .GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        HashSet<Type> called = new();

        foreach (var dbSet in dbSets)
        {
            var entityType = dbSet.PropertyType.GetGenericArguments()[0];
            InvokeOnModelCreating(entityType, modelBuilder);

            called.Add(entityType);
        }

        return called;
    }

    public static void CallOnModelCreatingForEntities(this ModelBuilder modelBuilder)
    {
        // For each entity type in the model, call the following method when available
        // public static void OnModelCreating(ModelBuilder modelBuilder)

        var types = modelBuilder.Model
            .GetEntityTypes()
            .Select(e => e.ClrType)
            .ToList();

        foreach (var entityType in types)
        {
            InvokeOnModelCreating(entityType, modelBuilder);
        }
    }

    private static void InvokeOnModelCreating(Type entityType, ModelBuilder modelBuilder)
    {
        var method = entityType.GetMethod("OnModelCreating", BindingFlags.Public | BindingFlags.Static);

        method?.Invoke(null, [modelBuilder]);
    }

    public static void ModifyProperty<TEntity, TProperty>(this DbContext context, TEntity entry, Expression<Func<TEntity, TProperty>> field,
        bool modified = true)
        where TEntity : class
    {
        context
            .Entry(entry)
            .ModifyProperty(field, modified);
    }

    public static void ModifyProperty<TEntity, TPropertyType>(this EntityEntry<TEntity> entry,
        Expression<Func<TEntity, TPropertyType>> property,
        bool modified = true) where TEntity : class
    {
        entry.Property(property).IsModified = modified;
    }

    public static TEntity ModifyProperties<TEntity, TSource, TTarget>(this DbContext context, TEntity entity, TSource source,
        Expression<Func<TSource, TTarget>> expression)
        where TEntity : class
    {
        var entry = context.Entry(entity);
        return ModifyProperties(entry, entity, source, expression);
    }

    public static TEntity ModifyProperties<TEntity, TSource, TTarget>(this EntityEntry<TEntity> entry, TSource source,
        Expression<Func<TSource, TTarget>> expression)
        where TEntity : class
    {
        return ModifyProperties(entry, entry.Entity, source, expression);
    }

    /// <summary>
    /// Similar to <see cref="PropertyValues.SetValues(object)"/>, but it enforces that the properties are set to a modified state.
    /// </summary>
    public static void ModifyProperties<TEntity, TSource>(this EntityEntry<TEntity> entry, TSource source)
        where TEntity : class
    {
        // use all fields of the source class
        var entityType = entry.Entity.GetType();

        var properties = typeof(TSource)
            .GetProperties()
            .Where(x => x.CanRead)
            .ToArray();

        foreach (var property in properties)
        {
            var value = property.GetValue(source);
            var propertyInfo = entityType.GetProperty(property.Name);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException($"Property {property.Name} not found on {entityType.Name}");
            }

            propertyInfo.SetValue(entry.Entity, value);
            entry.Property(property.Name).IsModified = true;
        }
    }

    public static TEntity ModifyProperties<TEntity, TSource, TTarget>(this EntityEntry<TEntity> entry, TEntity entity, TSource source,
        Expression<Func<TSource, TTarget>> expression)
        where TEntity : class
    {
        var entityType = entity!.GetType();

        var properties = ((NewExpression) expression.Body).Arguments
            .Cast<MemberExpression>()
            .Select(x => x.Member)
            .Cast<PropertyInfo>()
            .ToArray();

        foreach (var property in properties)
        {
            var value = property.GetValue(source);
            entityType.GetProperty(property.Name)!.SetValue(entity, value);
            entry.Property(property.Name).IsModified = true;
        }

        return entity;
    }

    public static IQueryable<TSource> TakeOrAll<TSource>(this IQueryable<TSource> source, int? count)
    {
        return count.HasValue ? source.Take(count.Value) : source;
    }

    public static IQueryable<TSource> SkipOrAll<TSource>(this IQueryable<TSource> source, int? count)
    {
        return count.HasValue ? source.Skip(count.Value) : source;
    }

    public static IQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source,
        Expression<Func<TSource, TKey>> keySelector,
        OrderByDirection direction)
    {
        return direction switch
        {
            OrderByDirection.Ascending => source.OrderBy(keySelector),
            OrderByDirection.Descending => source.OrderByDescending(keySelector),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }
}

public enum OrderByDirection
{
    Ascending,
    Descending,
}