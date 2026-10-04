using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeBid.Api.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = null!;
        
        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = null!;
        
        public string Role { get; set; } = "User"; // Admin, User
        public int HealthScore { get; set; } = 100;
        public string BuyerTier { get; set; } = "Bronze";
        public string SellerTier { get; set; } = "Bronze";
        public int SevereViolationCount { get; set; } = 0;
        public bool IsBanned { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Wallet Wallet { get; set; } = null!;
    }

    public class Wallet
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal AvailableBalance { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal HoldAmount { get; set; } = 0;
        
        public bool IsConfiscated { get; set; } = false;
        
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
    }
}
