using System;
using System.Collections.Generic;

namespace SafeBid.Domain;

public class Auction
{
    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public decimal StartPrice { get; private set; }
    public decimal StepPrice { get; private set; }
    public decimal? ReservePrice { get; private set; }
    public decimal? BuyNowPrice { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public AuctionStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private Auction() { }

    public static Result<Auction> CreateDraft(
        Guid sellerId, 
        string title, 
        Guid categoryId, 
        decimal startPrice,
        decimal stepPrice,
        decimal? reservePrice,
        decimal? buyNowPrice,
        DateTime startTime,
        DateTime endTime)
    {
        if (startTime <= DateTime.UtcNow) return Result<Auction>.Failure(new Error("Auction.InvalidTime", "StartTime must be in the future"));
        if (endTime < startTime.AddHours(1)) return Result<Auction>.Failure(new Error("Auction.InvalidTime", "EndTime must be at least 1 hour after StartTime"));
        if (stepPrice <= 0) return Result<Auction>.Failure(new Error("Auction.InvalidPrice", "StepPrice must be > 0"));
        if (reservePrice.HasValue && reservePrice.Value <= startPrice) return Result<Auction>.Failure(new Error("Auction.InvalidPrice", "ReservePrice must be > StartPrice"));
        if (buyNowPrice.HasValue && buyNowPrice.Value <= startPrice) return Result<Auction>.Failure(new Error("Auction.InvalidPrice", "BuyNowPrice must be > StartPrice"));

        return Result<Auction>.Success(new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Title = title,
            CategoryId = categoryId,
            StartPrice = startPrice,
            StepPrice = stepPrice,
            ReservePrice = reservePrice,
            BuyNowPrice = buyNowPrice,
            CurrentPrice = startPrice,
            StartTime = startTime,
            EndTime = endTime,
            Status = AuctionStatus.Draft
        });
    }
}
