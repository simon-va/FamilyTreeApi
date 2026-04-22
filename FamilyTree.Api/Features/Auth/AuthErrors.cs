using ErrorOr;

namespace FamilyTreeApiV2.Features.Auth;

public static class AuthErrors
{
    public static Error EmailTaken =>
        Error.Conflict("Auth.EmailTaken", "An account with this email address already exists.");

    public static Error SignUpFailed(string message) =>
        Error.Unexpected("Auth.SignUpFailed", $"Sign-up failed: {message}");

    public static Error SignUpNoSession =>
        Error.Unexpected("Auth.SignUpFailed", "Sign-up succeeded but no session was returned. Email confirmation may be required.");

    public static Error ProfileWriteFailed(string message) =>
        Error.Unexpected("Auth.ProfileWriteFailed", $"Account created but profile could not be saved: {message}");

    public static Error InvalidCredentials =>
        Error.Validation("Auth.InvalidCredentials", "Email or password is incorrect.");

    public static Error LoginFailed =>
        Error.Unexpected("Auth.LoginFailed", "Login succeeded but no session was returned.");

    public static Error UserProfileNotFound =>
        Error.NotFound("Auth.UserProfileNotFound", "User profile not found.");
}
