using FluentAssertions;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OmniCore.IntegrationTests;

public class ProductIntegrationTests
{
    private readonly ApiTestFixture _fixture = new();

    [Fact]
    public async Task AdminCanCreateAndGetProduct()
    {
        var email =
            Environment.GetEnvironmentVariable(
                "OMNICORE_ADMIN_EMAIL");

        var password =
            Environment.GetEnvironmentVariable(
                "OMNICORE_ADMIN_PASSWORD");

        var token = await _fixture.LoginAsync(
            email!,
            password!);

        ApiTestFixture.SetBearerToken(
            _fixture.ProductClient,
            token);

        var sku =
            $"INT-{Guid.NewGuid():N}"[..20];

        var createResponse =
            await _fixture.ProductClient.PostAsJsonAsync(
                "api/Products",
                new
                {
                    name = "Integration Test Product",
                    sku,
                    description = "Created by integration test",
                    price = 1234.56m,
                    isActive = true
                });

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<ProductResponse>();

        created.Should().NotBeNull();

        var getResponse =
            await _fixture.ProductClient.GetAsync(
                $"api/Products/{created!.Id}");

        getResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    private class ProductResponse
    {
        public Guid Id { get; set; }
    }
}