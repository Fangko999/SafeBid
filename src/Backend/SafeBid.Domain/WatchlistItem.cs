namespace SafeBid.Domain;

public class WatchlistItem
{
    public Guid UserId { get; set; }
    public Guid AuctionId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Auction? Auction { get; set; }
}
