using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FamilyTreeApiV2.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddSupabaseJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var discoveryUrl = configuration["Supabase:DiscoveryUrl"]
            ?? throw new InvalidOperationException("Supabase:DiscoveryUrl is not configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = discoveryUrl;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services;
    }
}
