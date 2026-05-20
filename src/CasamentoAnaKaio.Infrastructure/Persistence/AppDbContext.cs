using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CasamentoAnaKaio.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public static readonly Guid AdminRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid UserRoleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid GuestRoleId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public DbSet<GuestConfirmation> GuestConfirmations => Set<GuestConfirmation>();
    public DbSet<Gift> Gifts => Set<Gift>();
    public DbSet<GiftContribution> GiftContributions => Set<GiftContribution>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

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
            entity.HasIndex(x => x.ProviderPaymentId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.Property(x => x.MercadoPagoPaymentId).HasMaxLength(120);
            entity.Property(x => x.PreferenceId).HasMaxLength(120);
            entity.Property(x => x.InitPoint).HasMaxLength(1000);
            entity.Property(x => x.SandboxInitPoint).HasMaxLength(1000);
            entity.Property(x => x.ExternalReference).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PaymentMethod).HasMaxLength(40).IsRequired();
            entity.Property(x => x.PayerName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.PayerEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.MercadoPagoStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.PixQrCode).HasMaxLength(4000);
            entity.Property(x => x.PixCopyPaste).HasMaxLength(4000);
            entity.HasIndex(x => x.GiftContributionId);
            entity.HasIndex(x => x.MercadoPagoPaymentId);
            entity.HasIndex(x => x.PreferenceId);
            entity.HasIndex(x => x.ExternalReference);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.RoleType).HasConversion<int>();
            entity.HasMany(x => x.Users).WithMany(x => x.Roles).UsingEntity("UserRoles");

            entity.HasData(
                new
                {
                    Id = AdminRoleId,
                    Name = "Admin",
                    RoleType = RoleType.Admin,
                    Description = "Acesso administrativo completo.",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 5, 19, 0, 0, 0, TimeSpan.Zero)
                },
                new
                {
                    Id = UserRoleId,
                    Name = "User",
                    RoleType = RoleType.User,
                    Description = "Usuario autenticado.",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 5, 19, 0, 0, 0, TimeSpan.Zero)
                },
                new
                {
                    Id = GuestRoleId,
                    Name = "Guest",
                    RoleType = RoleType.Guest,
                    Description = "Convidado com acesso publico.",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 5, 19, 0, 0, 0, TimeSpan.Zero)
                });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasMany(x => x.Roles).WithMany(x => x.Users).UsingEntity("UserRoles");
        });
    }
}
