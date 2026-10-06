using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<WithdrawalRequest> WithdrawalRequests { get; }
    DbSet<Category> Categories { get; }
    DbSet<AuctionMedia> AuctionMedia { get; }
    DbSet<Auction> Auctions { get; }
    DbSet<WatchlistItem> WatchlistItems { get; }
    DbSet<Bid> Bids { get; }
    DbSet<Question> Questions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
