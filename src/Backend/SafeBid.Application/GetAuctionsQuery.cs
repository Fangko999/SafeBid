using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public record GetAuctionsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<Result<PaginatedResult<AuctionDto>>>;

public class AuctionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal StartPrice { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string MainImageUrl { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
}

public class GetAuctionsQueryHandler : IRequestHandler<GetAuctionsQuery, Result<PaginatedResult<AuctionDto>>>
{
    private readonly IAppDbContext _dbContext;

    public GetAuctionsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<AuctionDto>>> Handle(GetAuctionsQuery request, CancellationToken cancellationToken)
    {
        var query = from a in _dbContext.Auctions
                    where a.Status == AuctionStatus.Active
                    join c in _dbContext.Categories on a.CategoryId equals c.Id
                    join u in _dbContext.Users on a.SellerId equals u.Id
                    orderby a.EndTime ascending
                    select new AuctionDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        CurrentPrice = a.CurrentPrice,
                        StartPrice = a.StartPrice,
                        EndTime = a.EndTime,
                        Status = a.Status.ToString(),
                        CategoryName = c.Name,
                        SellerName = u.FullName,
                        MainImageUrl = _dbContext.AuctionMedia
                            .Where(m => m.AuctionId == a.Id)
                            .OrderBy(m => m.SortOrder)
                            .Select(m => m.MediaUrl)
                            .FirstOrDefault() ?? string.Empty
                    };

        var totalRecords = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<AuctionDto>(
            items,
            totalRecords,
            request.PageNumber,
            request.PageSize
        );

        return Result<PaginatedResult<AuctionDto>>.Success(result);
    }
}
