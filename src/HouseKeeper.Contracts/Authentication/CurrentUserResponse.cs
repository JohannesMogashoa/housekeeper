namespace HouseKeeper.Contracts.Authentication;

public sealed record CurrentUserResponse(string Subject, string DisplayName);
