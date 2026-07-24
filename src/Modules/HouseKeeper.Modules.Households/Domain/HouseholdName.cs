namespace HouseKeeper.Modules.Households.Domain;

public sealed record HouseholdName
{
    public const int MinLength = 2;
    public const int MaxLength = 120;

    private HouseholdName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool TryCreate(
        string? candidate,
        out HouseholdName? householdName,
        out string? error)
    {
        string normalized = candidate?.Trim() ?? string.Empty;

        if (normalized.Length < MinLength)
        {
            householdName = null;
            error = $"Household name must contain at least {MinLength} characters.";
            return false;
        }

        if (normalized.Length > MaxLength)
        {
            householdName = null;
            error = $"Household name cannot exceed {MaxLength} characters.";
            return false;
        }

        householdName = new HouseholdName(normalized);
        error = null;
        return true;
    }

    public override string ToString() => Value;
}
