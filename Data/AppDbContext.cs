using Microsoft.EntityFrameworkCore;
using OrderOutboxDemo.Models;

namespace OrderOutboxDemo.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OutboxMessage> OutboxMessages
        => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerEmail)
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.ProcessedAtUtc,
                x.CreatedAtUtc
            });
        });
    }
}