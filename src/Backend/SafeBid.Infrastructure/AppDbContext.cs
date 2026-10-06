using Microsoft.EntityFrameworkCore;
using SafeBid.Application;
using SafeBid.Domain;

namespace SafeBid.Infrastructure;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AuctionMedia> AuctionMedia => Set<AuctionMedia>();
    public DbSet<Auction> Auctions => Set<Auction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne<User>().WithOne().HasForeignKey<Wallet>(e => e.UserId);
        });

        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<WithdrawalRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuctionMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AuctionId);
            entity.HasOne<Auction>().WithMany().HasForeignKey(e => e.AuctionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Auction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.StartPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.StepPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ReservePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BuyNowPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CurrentPrice).HasColumnType("decimal(18,2)");
        });
    }
}
