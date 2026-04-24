using ErrorOr;
using FamilyTreeApiV2.Infrastructure.Supabase;
using Supabase.Gotrue.Exceptions;

namespace FamilyTreeApiV2.Features.Auth;

public class AuthHandler(Supabase.Client supabase, IAuthRepository authRepository, ISupabaseAdminService supabaseAdmin)
{
    public async Task<ErrorOr<AuthResponse>> SignUpAsync(SignUpRequest request)
    {
        Supabase.Gotrue.Session? session;

        try
        {
            session = await supabase.Auth.SignUp(request.Email, request.Password);
        }
        catch (GotrueException ex) when (ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
        {
            return AuthErrors.EmailTaken;
        }
        catch (GotrueException ex)
        {
            return AuthErrors.SignUpFailed(ex.Message);
        }

        if (session?.User is null || session.AccessToken is null || session.RefreshToken is null)
            return AuthErrors.SignUpNoSession;

        var userId = Guid.Parse(session.User.Id!);

        User user;
        try
        {
            user = await authRepository.InsertUserAsync(userId, request.FirstName, request.LastName, request.Email);
        }
        catch (Exception ex)
        {
            await supabaseAdmin.DeleteUserAsync(userId);
            return AuthErrors.ProfileWriteFailed(ex.Message);
        }

        return new AuthResponse(session.AccessToken, session.RefreshToken, user);
    }

    public async Task<ErrorOr<AuthResponse>> LoginAsync(LoginRequest request)
    {
        Supabase.Gotrue.Session? session;

        try
        {
            session = await supabase.Auth.SignIn(request.Email, request.Password);
        }
        catch (GotrueException)
        {
            return AuthErrors.InvalidCredentials;
        }

        if (session?.User is null || session.AccessToken is null || session.RefreshToken is null)
            return AuthErrors.LoginFailed;

        var userId = Guid.Parse(session.User.Id!);
        var user = await authRepository.GetUserAsync(userId);

        if (user is null)
            return AuthErrors.UserProfileNotFound;

        return new AuthResponse(session.AccessToken, session.RefreshToken, user);
    }

    public async Task<ErrorOr<Deleted>> DeleteAccountAsync(Guid userId)
    {
        var isLastOwner = await authRepository.IsLastOwnerOfAnyBoardAsync(userId);
        if (isLastOwner)
            return AuthErrors.LastBoardOwner;

        await authRepository.DeleteUserAsync(userId);

        var deleted = await supabaseAdmin.DeleteUserAsync(userId);
        if (!deleted)
            return AuthErrors.DeleteFailed;

        return Result.Deleted;
    }
}
