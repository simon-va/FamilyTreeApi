namespace FamilyTreeApiV2.Features.Auth;

public record SignUpRequest(string Email, string Password, string FirstName, string LastName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, string RefreshToken, User User);
public record User(Guid Id, string Email, string FirstName, string LastName);
