namespace SafeBid.Domain;

public class LedgerEntry
{
    public Guid Id { get; set; }
    public Guid? WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
