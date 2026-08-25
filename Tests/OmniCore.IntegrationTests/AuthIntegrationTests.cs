using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace OmniCore.IntegrationTests;

public class AuthIntegrationTests
{
    private readonly ApiTestFixture _fixture = new();

    [Fact]
    public async Task AdminLogin_ReturnsJwt()
    {
        var email =
            Environment.GetEnvironmentVariable(
                "OMNICORE_ADMIN_EMAIL");

        var password =
            Environment.GetEnvironmentVariable(
                "OMNICORE_ADMIN_PASSWORD");

        email.Should().NotBeNullOrWhiteSpace();
        password.Should().NotBeNullOrWhiteSpace();

        var token = await _fixture.LoginAsync(
            email!,
            password!);

        token.Should().NotBeNullOrWhiteSpace();
    }
}