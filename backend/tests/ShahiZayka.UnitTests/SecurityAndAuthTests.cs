using Microsoft.EntityFrameworkCore;
using Moq;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Application.Interfaces;
using ShahiZayka.Application.Services;
using ShahiZayka.Domain.Entities;
using ShahiZayka.Domain.Enums;
using ShahiZayka.Infrastructure.Data;
using ShahiZayka.Infrastructure.Security;
using Xunit;

namespace ShahiZayka.UnitTests;

public class SecurityAndAuthTests : IDisposable
{
    private readonly ShahiZaykaDbContext _context;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly Mock<IJwtTokenGenerator> _jwtMock;
    private readonly AuthService _authService;

    public SecurityAndAuthTests()
    {
        var options = new DbContextOptionsBuilder<ShahiZaykaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ShahiZaykaDbContext(options);
        _passwordHasher = new Pbkdf2PasswordHasher();
        _jwtMock = new Mock<IJwtTokenGenerator>();

        DateTime exp;
        _jwtMock.Setup(j => j.GenerateToken(It.IsAny<User>(), out exp))
            .Returns("MOCK_JWT_TOKEN_AMAZON_SDE");

        _authService = new AuthService(_context, _passwordHasher, _jwtMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void PasswordHasher_GeneratesUniqueSaltsAndVerifiesCorrectly()
    {
        // Arrange
        var password = "SecureRoyalPassword@2026";

        // Act
        var (hash1, salt1) = _passwordHasher.HashPassword(password);
        var (hash2, salt2) = _passwordHasher.HashPassword(password);

        // Assert: Different random salts must be generated
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);

        // Assert: Verification succeeds
        Assert.True(_passwordHasher.VerifyPassword(password, hash1, salt1));
        Assert.True(_passwordHasher.VerifyPassword(password, hash2, salt2));

        // Wrong password fails
        Assert.False(_passwordHasher.VerifyPassword("WrongPassword123", hash1, salt1));
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesCustomerAccount()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Kaushik Candidate",
            Email = "kaushik.sde@amazon.com",
            Password = "MyStrongPassword@123",
            PhoneNumber = "+1 206-555-0100",
            Address = "Amazon HQ, Seattle"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Equal("kaushik.sde@amazon.com", result.Value.User.Email);
        Assert.Equal(UserRole.Customer, result.Value.User.Role);
        Assert.NotEmpty(result.Value.Token);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "First User",
            Email = "duplicate@test.com",
            Password = "Password@123"
        };

        await _authService.RegisterAsync(request);

        // Act: Second registration with same email
        var duplicateResult = await _authService.RegisterAsync(new RegisterRequest
        {
            FullName = "Second User",
            Email = "DUPLICATE@TEST.COM", // Case-insensitive duplicate
            Password = "Password@456"
        });

        // Assert
        Assert.False(duplicateResult.IsSuccess);
        Assert.Equal(409, duplicateResult.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var email = "login@test.com";
        var password = "CorrectPassword@123";

        await _authService.RegisterAsync(new RegisterRequest
        {
            FullName = "Login Tester",
            Email = email,
            Password = password
        });

        // Act
        var result = await _authService.LoginAsync(new LoginRequest
        {
            Email = email,
            Password = password
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.Token);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = "wrongpass@test.com";
        await _authService.RegisterAsync(new RegisterRequest
        {
            FullName = "Wrong Pass Tester",
            Email = email,
            Password = "ActualPassword@123"
        });

        // Act
        var result = await _authService.LoginAsync(new LoginRequest
        {
            Email = email,
            Password = "WrongPassword@123"
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
    }
}
