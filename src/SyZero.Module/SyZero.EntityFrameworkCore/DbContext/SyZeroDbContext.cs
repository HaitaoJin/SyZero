using Microsoft.EntityFrameworkCore;
using SyZero.Domain.Entities;
namespace SyZero.EntityFrameworkCore
{
    public class SyZeroDbContext<TContext> : DbContext
        where TContext : DbContext
    {
        public SyZeroDbContext()
        {
        }

        public SyZeroDbContext(DbContextOptions<TContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // modelBuilder.Entity<ArticleEntity>().HasIndex(u => u.Id).IsUnique();
        }
    }




}
