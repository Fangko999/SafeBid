using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record AddQuestionCommand(Guid AuctionId, Guid AskerId, string Content) : IRequest<Result<Guid>>;

public class AddQuestionCommandHandler : IRequestHandler<AddQuestionCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;

    public AddQuestionCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(AddQuestionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _db.Auctions.FindAsync(new object[] { request.AuctionId }, cancellationToken);
        if (auction == null) 
            return Result<Guid>.Failure(new Error("Auction.NotFound", "Phiên đấu giá không tồn tại"));

        var question = Question.Create(request.AuctionId, request.AskerId, request.Content);
        _db.Questions.Add(question);
        
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(question.Id);
    }
}
