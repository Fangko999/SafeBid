using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public record QuestionDto(Guid Id, string MaskedAskerName, string Content, DateTimeOffset CreatedAt, string? Answer, DateTimeOffset? AnsweredAt);

public record GetQuestionsQuery(Guid AuctionId) : IRequest<Result<List<QuestionDto>>>;

public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, Result<List<QuestionDto>>>
{
    private readonly IAppDbContext _context;

    public GetQuestionsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<QuestionDto>>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
    {
        var auctionExists = await _context.Auctions.AnyAsync(a => a.Id == request.AuctionId, cancellationToken);
        if (!auctionExists)
            return Result<List<QuestionDto>>.Failure(new Error("Auction.NotFound", "Phiên đấu giá không tồn tại"));

        // Join with Users to get AskerName
        var questionsRaw = await _context.Questions
            .Where(q => q.AuctionId == request.AuctionId)
            .Join(_context.Users, q => q.AskerId, u => u.Id, (q, u) => new { q, u.FullName })
            .OrderBy(x => x.q.CreatedAt)
            .ToListAsync(cancellationToken);

        var questions = questionsRaw.Select(x => new QuestionDto(
            x.q.Id,
            MaskName(x.FullName),
            x.q.Content,
            x.q.CreatedAt,
            x.q.Answer,
            x.q.AnsweredAt
        )).ToList();

        return Result<List<QuestionDto>>.Success(questions);
    }

    private static string MaskName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Anonymous";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, 1) + "***";
        var firstName = parts.Last();
        var lastName = parts.First();
        return $"{lastName} {firstName.Substring(0, 1)}***";
    }
}
