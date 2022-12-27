using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Gibbs2b.DtoGenerator;

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
}