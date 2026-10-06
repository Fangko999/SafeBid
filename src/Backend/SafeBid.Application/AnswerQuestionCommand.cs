using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record AnswerQuestionCommand(Guid AuctionId, Guid QuestionId, Guid SellerId, string Answer) : IRequest<Result<bool>>;

public class AnswerQuestionCommandHandler : IRequestHandler<AnswerQuestionCommand, Result<bool>>
{
    private readonly IAppDbContext _db;

    public AnswerQuestionCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> Handle(AnswerQuestionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _db.Auctions.FindAsync(new object[] { request.AuctionId }, cancellationToken);
        if (auction == null) 
            return Result<bool>.Failure(new Error("Auction.NotFound", "Phiên đấu giá không tồn tại"));

        if (auction.SellerId != request.SellerId)
            return Result<bool>.Failure(new Error("Question.Forbidden", "Chỉ chủ phiên đấu giá mới được trả lời"));

        var question = await _db.Questions.FindAsync(new object[] { request.QuestionId }, cancellationToken);
        if (question == null || question.AuctionId != request.AuctionId)
            return Result<bool>.Failure(new Error("Question.NotFound", "Câu hỏi không tồn tại"));

        question.AddAnswer(request.Answer);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
