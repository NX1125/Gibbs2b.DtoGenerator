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
        var lambda = field as LambdaExpression;

        modelBuilder.HasTsVector(
            field,
            include,
            Expression.Lambda<Func<TEntity, object?>>(lambda.Body, false, lambda.Parameters),
            config);
    }

    public static void CallOnModelCreatingForDbSets<TContext>(this TContext context, ModelBuilder modelBuilder)
        where TContext : DbContext
    {
        // For each DbSet property, call the following method when available
        // public static void OnModelCreating(ModelBuilder modelBuilder)

        var dbSets = context
            .GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        foreach (var dbSet in dbSets)
        {
            var entityType = dbSet.PropertyType.GetGenericArguments()[0];
            InvokeOnModelCreating(entityType, modelBuilder);
        }
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
}