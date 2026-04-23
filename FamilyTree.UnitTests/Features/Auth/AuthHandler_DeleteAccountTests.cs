using ErrorOr;
using FamilyTreeApiV2.Features.Auth;
using FamilyTreeApiV2.Infrastructure.Supabase;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Auth;

public class AuthHandler_DeleteAccountTests
{
    // Rules:
    // - User cannot delete account if they are the sole owner of any active board
    // - If Supabase Admin API call fails, return unexpected error and do not delete from DB
    // - On success: user is deleted from DB and Result.Deleted is returned

    private readonly Mock<IAuthRepository> _repoMock = new();
    private readonly Mock<ISupabaseAdminService> _supabaseAdminMock = new();
    private readonly AuthHandler _handler;

    public AuthHandler_DeleteAccountTests()
    {
        _handler = new AuthHandler(null!, _repoMock.Object, _supabaseAdminMock.Object);
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenUserIsLastBoardOwner_ShouldReturnLastBoardOwnerError()
    {
        _repoMock
            .Setup(r => r.IsLastOwnerOfAnyBoardAsync("user-1"))
            .ReturnsAsync(true);

        var result = await _handler.DeleteAccountAsync("user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.LastBoardOwner");
        _supabaseAdminMock.Verify(s => s.DeleteUserAsync(It.IsAny<string>()), Times.Never);
        _repoMock.Verify(r => r.DeleteUserAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenSupabaseDeleteFails_ShouldReturnDeleteFailedError()
    {
        _repoMock
            .Setup(r => r.IsLastOwnerOfAnyBoardAsync("user-1"))
            .ReturnsAsync(false);

        _supabaseAdminMock
            .Setup(s => s.DeleteUserAsync("user-1"))
            .ReturnsAsync(false);

        var result = await _handler.DeleteAccountAsync("user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.DeleteFailed");
        _repoMock.Verify(r => r.DeleteUserAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenValid_ShouldDeleteUserAndReturnDeleted()
    {
        _repoMock
            .Setup(r => r.IsLastOwnerOfAnyBoardAsync("user-1"))
            .ReturnsAsync(false);

        _supabaseAdminMock
            .Setup(s => s.DeleteUserAsync("user-1"))
            .ReturnsAsync(true);

        _repoMock
            .Setup(r => r.DeleteUserAsync("user-1"))
            .Returns(Task.CompletedTask);

        var result = await _handler.DeleteAccountAsync("user-1");

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        _repoMock.Verify(r => r.DeleteUserAsync("user-1"), Times.Once);
    }
}
