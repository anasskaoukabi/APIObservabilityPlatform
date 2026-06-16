using Microsoft.EntityFrameworkCore;
using TargetAPI.Models;

namespace TargetAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop Pro", Price = 1200.50m, StockQuantity = 10 },
                new Product { Id = 2, Name = "Souris Sans Fil", Price = 25.99m, StockQuantity = 50 },
                new Product { Id = 3, Name = "Clavier Mécanique", Price = 85.00m, StockQuantity = 20 }
            );
        }
    }
}
