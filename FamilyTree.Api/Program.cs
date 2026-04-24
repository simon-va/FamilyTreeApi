using Dapper;
using FamilyTreeApiV2.Extensions;
using FamilyTreeApiV2.Infrastructure.Database;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;

SqlMapper.AddTypeHandler(new BoardRoleTypeHandler());
SqlMapper.AddTypeHandler(new GenderTypeHandler());
SqlMapper.AddTypeHandler(new FuzzyDatePrecisionTypeHandler());
SqlMapper.AddTypeHandler(new FuzzyDateFieldPrecisionTypeHandler());
SqlMapper.AddTypeHandler(new ViewerPrivacyModeTypeHandler());
SqlMapper.AddTypeHandler(new NullableViewerPrivacyModeTypeHandler());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.AddSupabaseJwtAuth(builder.Configuration);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "FamilyTree API";
        doc.Info.Version = "v1";
        doc.Info.Description = "API für die Verwaltung von Familienstammbäumen (DSGVO-konform, DACH-Region).";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var address = app.Urls.FirstOrDefault(u => u.StartsWith("https")) ?? app.Urls.First();
        Console.WriteLine($"\nScalar UI: {address}/scalar/v1\n");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
