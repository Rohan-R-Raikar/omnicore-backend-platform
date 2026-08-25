using FluentAssertions;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OmniCore.IntegrationTests;

public class InventoryIntegrationTests
{
    private readonly ApiTestFixture _fixture = new();

    [Fact]
    public async Task AdminCanSetAndReserveInventory()
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

        ApiTestFixture.SetBearerToken(
            _fixture.InventoryClient,
            token);

        var sku =
            $"INV-{Guid.NewGuid():N}"[..20];

        var productResponse =
            await _fixture.ProductClient.PostAsJsonAsync(
                "api/Products",
                new
                {
                    name = "Inventory Integration Product",
                    sku,
                    description = "Integration inventory test",
                    price = 500m,
                    isActive = true
                });

        productResponse.EnsureSuccessStatusCode();

        var product =
            await productResponse.Content
                .ReadFromJsonAsync<ProductResponse>();

        var updateResponse =
            await _fixture.InventoryClient.PutAsJsonAsync(
                $"api/Inventory/{product!.Id}",
                new
                {
                    availableQuantity = 10,
                    reservedQuantity = 0
                });

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var reserveResponse =
            await _fixture.InventoryClient.PostAsJsonAsync(
                $"api/Inventory/{product.Id}/reserve",
                new
                {
                    quantity = 2
                });

        reserveResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var inventory =
            await reserveResponse.Content
                .ReadFromJsonAsync<InventoryResponse>();

        inventory!.AvailableQuantity
            .Should()
            .Be(8);

        inventory.ReservedQuantity
            .Should()
            .Be(2);
    }

    private class ProductResponse
    {
        public Guid Id { get; set; }
    }

    private class InventoryResponse
    {
        public int AvailableQuantity { get; set; }

        public int ReservedQuantity { get; set; }
    }
}