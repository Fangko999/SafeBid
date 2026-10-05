using System;

namespace SafeBid.Domain;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal AvailableBalance { get; set; } = 0;
    public decimal HoldAmount { get; set; } = 0;
    public bool IsConfiscated { get; set; } = false;
    public byte[]? RowVersion { get; set; }
}
