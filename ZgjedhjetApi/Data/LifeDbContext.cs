using Microsoft.EntityFrameworkCore;
using ZgjedhjetApi.Models.Entities;

namespace ZgjedhjetApi.Data
{
    public class LifeDbContext : DbContext
    {
        public LifeDbContext(DbContextOptions<LifeDbContext> options) : base(options)
        {
        }

        public DbSet<Zgjedhjet> Zgjedhjet { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Zgjedhjet>(entity =>
            {
                entity.ToTable("Zgjedhjet");

                entity.HasKey(x => new { x.Kategoria, x.Komuna, x.Qendra_e_votimit, x.Vendvotimi });

                entity.Property(x => x.Kategoria).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Komuna).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Qendra_e_votimit).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Vendvotimi).HasMaxLength(50).IsRequired();
            });
        }
    }
}
