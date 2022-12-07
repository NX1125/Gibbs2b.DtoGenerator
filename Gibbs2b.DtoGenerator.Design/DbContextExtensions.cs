using Microsoft.EntityFrameworkCore;

namespace Gibbs2b.DtoGenerator;

public static class DbContextExtensions
{
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