using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OmniCore.IntegrationTests;

public class ApiTestFixture
{
    public HttpClient AuthClient { get; } = new()
    {
        BaseAddress = new Uri("http://localhost:5033/")
    };

    public HttpClient ProductClient { get; } = new()
    {
        BaseAddress = new Uri("http://localhost:5137/")
    };

    public HttpClient InventoryClient { get; } = new()
    {
        BaseAddress = new Uri("http://localhost:5151/")
    };

    public HttpClient OrderClient { get; } = new()
    {
        BaseAddress = new Uri("http://localhost:5027/")
    };

    public async Task<string> LoginAsync(
        string email,
        string password)
    {
        var response = await AuthClient.PostAsJsonAsync(
            "api/Auth/login",
            new
            {
                email,
                password
            });

        response.EnsureSuccessStatusCode();

        var loginResponse =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (loginResponse is null ||
            string.IsNullOrWhiteSpace(loginResponse.Token))
        {
            throw new InvalidOperationException(
                "Login did not return a JWT token.");
        }

        return loginResponse.Token;
    }

    public static void SetBearerToken(
        HttpClient client,
        string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}