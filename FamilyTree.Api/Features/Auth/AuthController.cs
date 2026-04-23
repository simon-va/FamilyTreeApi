using System.Security.Claims;
using FamilyTreeApiV2.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTreeApiV2.Features.Auth;

[ApiController]
[Route("auth")]
public class AuthController(
    AuthHandler handler,
    IValidator<SignUpRequest> signUpValidator,
    IValidator<LoginRequest> loginValidator)
    : ControllerBase
{
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        var validation = await signUpValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var result = await handler.SignUpAsync(request);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var result = await handler.LoginAsync(request);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.DeleteAccountAsync(userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : NoContent();
    }
}
