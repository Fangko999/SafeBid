using System;

namespace SafeBid.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Cccd { get; set; }
    public bool EmailConfirmed { get; set; } = false;
    public int HealthScore { get; set; } = 100;
    public string BuyerTier { get; set; } = "Bronze";
    public string SellerTier { get; set; } = "Bronze";
    public int SevereViolationCount { get; set; } = 0;
    public bool IsBanned { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return phone;
        if (phone.StartsWith("+84")) return "0" + phone.Substring(3);
        if (phone.StartsWith("84")) return "0" + phone.Substring(2);
        return phone;
    }
}
