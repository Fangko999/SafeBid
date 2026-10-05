using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
