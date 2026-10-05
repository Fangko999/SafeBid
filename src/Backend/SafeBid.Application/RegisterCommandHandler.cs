using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;
using BCrypt.Net;

namespace SafeBid.Application;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IAppDbContext _context;

    public RegisterCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var validator = new RegisterCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<Guid>.Failure(DomainErrors.User.PasswordMismatch); // Simplified for this task to return our specific Error class
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            return Result<Guid>.Failure(DomainErrors.User.DuplicateEmail);
        }

        // Phone Normalization
        var normalizedPhone = request.PhoneNumber;
        if (normalizedPhone.StartsWith("+84"))
        {
            normalizedPhone = "0" + normalizedPhone.Substring(3);
        }
        else if (normalizedPhone.StartsWith("84"))
        {
            normalizedPhone = "0" + normalizedPhone.Substring(2);
        }

        if (await _context.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken))
        {
            return Result<Guid>.Failure(DomainErrors.User.DuplicatePhone);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = normalizedPhone,
            EmailConfirmed = false,
            HealthScore = 100,
            BuyerTier = "Bronze",
            SellerTier = "Bronze",
            SevereViolationCount = 0,
            IsBanned = false,
            CreatedAt = DateTime.UtcNow
        };

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AvailableBalance = 0,
            HoldAmount = 0,
            IsConfiscated = false
        };

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
