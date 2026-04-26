using ErrorOr;

namespace FamilyTreeApiV2.Features.Import;

public static class ImportErrors
{
    public static Error AuthenticationFailed =>
        Error.Unauthorized("Import.AuthenticationFailed", "Login bei der V1-API fehlgeschlagen.");

    public static Error OldApiFailed =>
        Error.Failure("Import.OldApiFailed", "Daten konnten nicht aus der V1-API geladen werden.");
}
