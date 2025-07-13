using CryptoApi.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace CryptoApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<CryptoData> CryptoData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure primary key and indexing
            modelBuilder.Entity<CryptoData>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<CryptoData>()
                .HasIndex(d => d.Symbol)
                .IsUnique();
        }
    }
}
