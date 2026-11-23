using System.Net;
using System.Net.Http.Json;
using BusinessEsbjerg.Holiday.Api.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace BusinessEsbjerg.Holiday.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory api)
{
    [Fact]
    public async Task Login_gives_a_token()
    {
        // Arrange
        var client = api.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestUsers.Anna, TestUsers.Password));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(login!.Token));
    }

    [Fact]
    public async Task Same_error_for_bad_login()
    {
        // Arrange
        var client = api.CreateClient();

        // Act
        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestUsers.Erik, "wrong password"));
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@businessesbjerg.com", "wrong password"));

        // Assert: the answer must not tell which emails exist
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var first = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>();
        var second = await unknownEmail.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(first!.Detail, second!.Detail);
    }

    [Fact]
    public async Task No_token_gives_401()
    {
        // Arrange
        var client = api.CreateClient();

        // Act
        var response = await client.GetAsync("/api/me/balances");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Employee_cant_see_queue()
    {
        // Arrange
        var anna = await api.LoggedInAs(TestUsers.Anna);

        // Act
        var response = await anna.GetAsync("/api/requests/queue");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Accountant_has_no_balance()
    {
        // Arrange: the accountant's login isn't an employee
        var accountant = await api.LoggedInAs(TestUsers.Accountant);

        // Act
        var response = await accountant.GetAsync("/api/me/balances");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
