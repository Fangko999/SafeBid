using System;

namespace SafeBid.Domain;

public class AuctionMedia
{
    public Guid Id { get; private set; }
    public Guid AuctionId { get; private set; }
    public string MediaUrl { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private AuctionMedia() { }

    public static AuctionMedia Create(Guid auctionId, string mediaUrl, int sortOrder)
    {
        return new AuctionMedia
        {
            Id = Guid.NewGuid(),
            AuctionId = auctionId,
            MediaUrl = mediaUrl,
            SortOrder = sortOrder
        };
    }
}
