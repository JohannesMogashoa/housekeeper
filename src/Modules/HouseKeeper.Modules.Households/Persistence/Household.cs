using HouseKeeper.Modules.Households.Domain;

namespace HouseKeeper.Modules.Households.Persistence;

internal sealed class Household
{
    private Household()
    {
    }

    private Household(Guid id, HouseholdName name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name.Value;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Household Create(
        Guid id,
        HouseholdName name,
        DateTimeOffset createdAtUtc) => new(id, name, createdAtUtc);
}
