using Microsoft.EntityFrameworkCore;
using ShahiZayka.Application.Common;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Application.Interfaces;
using ShahiZayka.Domain.Entities;
using ShahiZayka.Domain.Enums;

namespace ShahiZayka.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure("Email and password are required.", 400);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (existing)
        {
            return Result<AuthResponse>.Failure("An account with this email address already exists.", 409);
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = hash,
            Salt = salt,
            PhoneNumber = request.PhoneNumber.Trim(),
            Address = request.Address.Trim(),
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtTokenGenerator.GenerateToken(user, out var expiresAtUtc);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role
            }
        }, 201);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure("Email and password are required.", 400);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.Salt))
        {
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);
        }

        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure("Your account has been deactivated. Please contact support.", 403);
        }

        var token = _jwtTokenGenerator.GenerateToken(user, out var expiresAtUtc);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role
            }
        });
    }

    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role
        };
    }
}

public class ReviewService : IReviewService
{
    private readonly IApplicationDbContext _context;

    public ReviewService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync()
    {
        return await _context.Reviews
            .Where(r => r.IsApproved)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                CustomerName = r.CustomerName,
                Rating = r.Rating,
                Comment = r.Comment,
                ProfileImageUrl = r.ProfileImageUrl,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<ReviewDto> AddReviewAsync(CreateReviewRequest request)
    {
        var review = new Review
        {
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail?.Trim(),
            Rating = Math.Clamp(request.Rating, 1, 5),
            Comment = request.Comment.Trim(),
            ProfileImageUrl = "images/profile1.jpeg",
            IsApproved = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return new ReviewDto
        {
            Id = review.Id,
            CustomerName = review.CustomerName,
            Rating = review.Rating,
            Comment = review.Comment,
            ProfileImageUrl = review.ProfileImageUrl,
            CreatedAtUtc = review.CreatedAtUtc
        };
    }
}

public class NewsletterService : INewsletterService
{
    private readonly IApplicationDbContext _context;

    public NewsletterService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> SubscribeAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Result<bool>.Failure("Please enter a valid email address.", 400);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await _context.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email.ToLower() == normalizedEmail);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await _context.SaveChangesAsync();
            }
            return Result<bool>.Success(true);
        }

        _context.NewsletterSubscribers.Add(new NewsletterSubscriber
        {
            Email = normalizedEmail,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Result<bool>.Success(true, 201);
    }
}
