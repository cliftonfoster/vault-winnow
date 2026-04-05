namespace VaultWinnow;

internal static class DiffDisplayHelper
{
    public static string GetDescription(string? diffDisplay)
    {
        if (string.IsNullOrWhiteSpace(diffDisplay))
            return string.Empty;

        var parts = new List<string>();

        if (diffDisplay.Length > 0 && diffDisplay[0] == 'N')
            parts.Add("Name");

        if (diffDisplay.Length > 1 && diffDisplay[1] == 'U')
            parts.Add("Username");

        if (diffDisplay.Length > 2 && diffDisplay[2] == 'P')
            parts.Add("Password");

        if (diffDisplay.Length > 3 && diffDisplay[3] == 'O')
            parts.Add("Notes");

        if (diffDisplay.Length > 4 && diffDisplay[4] == 'T')
            parts.Add("TOTP");

        if (diffDisplay.Length > 5 && diffDisplay[5] == 'K')
            parts.Add("Passkey");

        return parts.Count == 0
            ? "No differences from the selected row."
            : "Different: " + string.Join(", ", parts);
    }
}