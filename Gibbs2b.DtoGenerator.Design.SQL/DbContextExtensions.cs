using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
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

    public static void ExcludeView<TModel>(this ModelBuilder builder, string tableName) where TModel : class
    {
        builder
            .Entity<TModel>()
            .ToTable(tableName, b => b.ExcludeFromMigrations());
    }

    [Obsolete]
    public static void CreateModelDto<TDto>(this ModelBuilder modelBuilder, string[] properties, string tableName, string[]? keys = null) where TDto : class
    {
        modelBuilder.Entity<TDto>(op =>
        {
            foreach (var p in properties)
                op.Property(p).HasColumnName(p);

            if (keys != null)
            {
                foreach (var key in keys)
                {
                    op.Property(key).IsRequired();
                }

                op.HasKey(keys);
            }
            else
            {
                op.Property("Id").IsRequired();
            }
        });
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
            var method = entityType.GetMethod("OnModelCreating", BindingFlags.Public | BindingFlags.Static);

            method?.Invoke(null, [modelBuilder]);
        }
    }
}