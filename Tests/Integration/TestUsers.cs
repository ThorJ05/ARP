using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessEsbjerg.Holiday.Api.Endpoints;

namespace BusinessEsbjerg.Holiday.Tests.Integration;

// The users the seeder creates in development. They all have the same development-only password.
public static class TestUsers
{
    public const string Password = "Test1234!";
    public const string Anna = "anna@businessesbjerg.com";
    public const string Bo = "bo@businessesbjerg.com";
    public const string Erik = "erik@businessesbjerg.com";
    public const string Accountant = "regnskab@businessesbjerg.com";

    // A client that is logged in as the user (the token is sent on every request).
    public static async Task<HttpClient> LoggedInAs(this ApiFactory api, string email)
    {
        var client = api.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }
}
