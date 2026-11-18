namespace BusinessEsbjerg.Holiday.Service.Auth;

/// <summary>How many failed logins in a row before the account is locked, and for how long.</summary>
public record LockoutSettings(int MaxFailedAttempts = 5, int LockoutMinutes = 15);
