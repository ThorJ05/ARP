namespace BusinessEsbjerg.Holiday.Service.Auth;

// How many failed logins in a row before the account is locked, and for how long.
public record LockoutSettings(int MaxFailedAttempts = 5, int LockoutMinutes = 15);
