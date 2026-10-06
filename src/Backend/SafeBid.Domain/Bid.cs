namespace SafeBid.Domain;

public class Bid
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid BidderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsWinning { get; set; }

    public Auction? Auction { get; set; }
    public User? Bidder { get; set; }
}
