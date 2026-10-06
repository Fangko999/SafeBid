using System;

namespace SafeBid.Domain;

public class Question
{
    public Guid Id { get; private set; }
    public Guid AuctionId { get; private set; }
    public Guid AskerId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? Answer { get; private set; }
    public DateTimeOffset? AnsweredAt { get; private set; }

    // EF Core
    private Question() { }

    public static Question Create(Guid auctionId, Guid askerId, string content)
    {
        return new Question
        {
            Id = Guid.NewGuid(),
            AuctionId = auctionId,
            AskerId = askerId,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddAnswer(string answer)
    {
        Answer = answer;
        AnsweredAt = DateTimeOffset.UtcNow;
    }
}
