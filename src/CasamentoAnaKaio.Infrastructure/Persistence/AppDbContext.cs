using CasamentoAnaKaio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CasamentoAnaKaio.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<GuestConfirmation> GuestConfirmations => Set<GuestConfirmation>();
    public DbSet<Gift> Gifts => Set<Gift>();
    public DbSet<GiftContribution> GiftContributions => Set<GiftContribution>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuestConfirmation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(600);
        });

        modelBuilder.Entity<Gift>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(140).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(600).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Price).HasPrecision(10, 2);

            entity.HasData(
                new
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Jogo de panelas",
                    Description = "Para começar a casa nova com refeições bem cuidadas.",
                    ImageUrl = "https://images.unsplash.com/photo-1584990347449-ae6e1f0da4a9?auto=format&fit=crop&w=900&q=80",
                    Price = 420m,
                    ReservedPercent = 35,
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero)
                },
                new
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Jantar especial",
                    Description = "Uma lembrança para nosso primeiro jantar depois do casamento.",
                    ImageUrl = "https://images.unsplash.com/photo-1543353071-10c8ba85a904?auto=format&fit=crop&w=900&q=80",
                    Price = 280m,
                    ReservedPercent = 60,
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero)
                },
                new
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Cota lua de mel",
                    Description = "Ajude com uma parte da nossa viagem e dos passeios.",
                    ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=900&q=80",
                    Price = 900m,
                    ReservedPercent = 20,
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero)
                },
                new
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Cafeteira",
                    Description = "Para os cafés da manhã e visitas na casa nova.",
                    ImageUrl = "https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?auto=format&fit=crop&w=900&q=80",
                    Price = 360m,
                    ReservedPercent = 45,
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero)
                });
        });

        modelBuilder.Entity<GiftContribution>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ContributorName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ContributorPhone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.Property(x => x.PixKey).HasMaxLength(160).IsRequired();
            entity.Property(x => x.QrCodePayload).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ProviderPaymentId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Mode).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.Gift).WithMany(x => x.Contributions).HasForeignKey(x => x.GiftId);
        });
    }
}
