namespace BusinessEsbjerg.Holiday.Tests.E2E;

// E2E tests need the website, the API and the database running.
// They are skipped unless the environment variable E2E is 1, so "Run All" stays green on a normal day.
public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute() => Skip = E2ESwitch.SkipReason;
}

internal static class E2ESwitch
{
    public static string? SkipReason =>
        Environment.GetEnvironmentVariable("E2E") == "1"
            ? null
            : "Set E2E=1 and start the website, the API and the database to run the E2E tests.";
}
