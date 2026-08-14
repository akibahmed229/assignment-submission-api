using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using AssignmentSubmissionSystem.Api.Models.Enums;
using AssignmentSubmissionSystem.Api.Services;
using AssignmentSubmissionSystem.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtService> _jwt = new();

    private AuthService CreateSut(out AssignmentSubmissionSystem.Api.Data.AppDbContext db)
    {
        db = TestDbContextFactory.Create();
        return new AuthService(db, _hasher.Object, _jwt.Object);
    }

    [Fact]
    public async Task RegisterAsync_Should_Create_User_When_Email_Is_Unique()
    {
        var sut = CreateSut(out var db);
        _hasher.Setup(h => h.Hash("P@ssword123")).Returns("hashed-value");
        _jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("fake-jwt");

        var dto = new RegisterDto { FullName = "Jane Doe", Email = "jane@example.com", Password = "P@ssword123", Role = Role.Student };
        var result = await sut.RegisterAsync(dto);

        result.Email.Should().Be("jane@example.com");
        result.Token.Should().Be("fake-jwt");
        db.Users.Should().ContainSingle(u => u.Email == "jane@example.com" && u.PasswordHash == "hashed-value");
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Email_Already_Registered()
    {
        var sut = CreateSut(out var db);
        db.Users.Add(new User { FullName = "Existing", Email = "jane@example.com", PasswordHash = "x", Role = Role.Student });
        await db.SaveChangesAsync();

        var dto = new RegisterDto { FullName = "Jane Doe", Email = "jane@example.com", Password = "P@ssword123", Role = Role.Student };

        var act = () => sut.RegisterAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Token_When_Credentials_Valid()
    {
        var sut = CreateSut(out var db);
        var user = new User { FullName = "Jane", Email = "jane@example.com", PasswordHash = "hashed", Role = Role.Student, IsActive = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        _hasher.Setup(h => h.Verify("correct-password", "hashed")).Returns(true);
        _jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("fake-jwt");

        var result = await sut.LoginAsync(new LoginDto { Email = "jane@example.com", Password = "correct-password" });

        result.Token.Should().Be("fake-jwt");
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_Unauthorized_When_User_Does_Not_Exist()
    {
        var sut = CreateSut(out _);

        var act = () => sut.LoginAsync(new LoginDto { Email = "ghost@example.com", Password = "whatever" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_Unauthorized_When_Password_Wrong()
    {
        var sut = CreateSut(out var db);
        db.Users.Add(new User { FullName = "Jane", Email = "jane@example.com", PasswordHash = "hashed", Role = Role.Student, IsActive = true });
        await db.SaveChangesAsync();
        _hasher.Setup(h => h.Verify("wrong-password", "hashed")).Returns(false);

        var act = () => sut.LoginAsync(new LoginDto { Email = "jane@example.com", Password = "wrong-password" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_Unauthorized_When_User_Is_Inactive()
    {
        var sut = CreateSut(out var db);
        db.Users.Add(new User { FullName = "Jane", Email = "jane@example.com", PasswordHash = "hashed", Role = Role.Student, IsActive = false });
        await db.SaveChangesAsync();
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "hashed")).Returns(true);

        var act = () => sut.LoginAsync(new LoginDto { Email = "jane@example.com", Password = "correct-password" });

        // IsActive check happens even if the password would otherwise verify
        // -- deactivated accounts must not be able to log in, full stop.
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
