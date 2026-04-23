using FamilyTreeApiV2.Features.Auth;
using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Infrastructure.Supabase;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentValidation;

namespace FamilyTreeApiV2.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<ISupabaseAdminService, SupabaseAdminService>();

        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is not configured.");
        var supabaseApiKeySecret = configuration["Supabase:ApiKeySecret"]
                                   ?? throw new InvalidOperationException("Supabase:ApiKeySecret is not configured.");

        services.AddSingleton<Supabase.Client>(_ =>
        {
            var client = new Supabase.Client(supabaseUrl, supabaseApiKeySecret, new Supabase.SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            });
            client.InitializeAsync().GetAwaiter().GetResult();
            return client;
        });

        return services;
    }

    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<AuthHandler>();

        services.AddScoped<IBoardsRepository, BoardsRepository>();
        services.AddScoped<BoardsHandler>();

        services.AddScoped<IMembersRepository, MembersRepository>();
        services.AddScoped<MembersHandler>();

        services.AddScoped<IPersonsRepository, PersonsRepository>();
        services.AddScoped<IFuzzyDateRepository, FuzzyDateRepository>();
        services.AddScoped<PersonsHandler>();

        services.AddValidatorsFromAssemblyContaining<SignUpRequestValidator>();

        return services;
    }
}
