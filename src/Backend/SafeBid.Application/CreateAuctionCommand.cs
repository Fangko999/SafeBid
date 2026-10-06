using MediatR;
using SafeBid.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SafeBid.Application;

public record CreateAuctionCommand(
    Guid SellerId,
    string Title,
    Guid CategoryId,
    decimal StartPrice,
    decimal StepPrice,
    decimal? ReservePrice,
    decimal? BuyNowPrice,
    DateTime StartTime,
    DateTime EndTime,
    List<string> MediaUrls) : IRequest<Result<Guid>>;

public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;

    public CreateAuctionCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { request.SellerId }, cancellationToken);
        if (user == null || user.HealthScore < 60)
            return Result<Guid>.Failure(new Error("Auction.Forbidden", "Seller health score too low"));

        var auctionResult = Auction.CreateDraft(request.SellerId, request.Title, request.CategoryId, request.StartPrice, request.StepPrice, request.ReservePrice, request.BuyNowPrice, request.StartTime, request.EndTime);
        
        if (!auctionResult.IsSuccess)
            return Result<Guid>.Failure(auctionResult.Error);
            
        var auction = auctionResult.Value;
        _db.Auctions.Add(auction);

        var sortOrder = 0;
        foreach (var url in request.MediaUrls)
        {
            _db.AuctionMedia.Add(AuctionMedia.Create(auction.Id, url, sortOrder++));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(auction.Id);
    }
}
