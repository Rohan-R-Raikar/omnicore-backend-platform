using FluentAssertions;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OmniCore.IntegrationTests;

public class OrderIntegrationTests
{
    private readonly ApiTestFixture _fixture = new();

    [Fact]
    public async Task CreateAndCancelOrder_ReservesAndReleasesInventory()
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

        ApiTestFixture.SetBearerToken(
            _fixture.OrderClient,
            token);

        var sku =
            $"ORD-{Guid.NewGuid():N}"[..20];

        var createProductResponse =
            await _fixture.ProductClient.PostAsJsonAsync(
                "api/Products",
                new
                {
                    name = "Order Integration Product",
                    sku,
                    description = "Integration order test",
                    price = 999m,
                    isActive = true
                });

        createProductResponse.EnsureSuccessStatusCode();

        var product =
            await createProductResponse.Content
                .ReadFromJsonAsync<ProductResponse>();

        var updateInventoryResponse =
            await _fixture.InventoryClient.PutAsJsonAsync(
                $"api/Inventory/{product!.Id}",
                new
                {
                    availableQuantity = 10,
                    reservedQuantity = 0
                });

        updateInventoryResponse.EnsureSuccessStatusCode();

        var createOrderResponse =
            await _fixture.OrderClient.PostAsJsonAsync(
                "api/Orders",
                new
                {
                    items = new[]
                    {
                        new
                        {
                            productId = product.Id,
                            quantity = 2
                        }
                    }
                });

        createOrderResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var order =
            await createOrderResponse.Content
                .ReadFromJsonAsync<OrderResponse>();

        order.Should().NotBeNull();
        order!.TotalAmount.Should().Be(1998m);

        var inventoryAfterOrder =
            await _fixture.InventoryClient
                .GetFromJsonAsync<InventoryResponse>(
                    $"api/Inventory/{product.Id}");

        inventoryAfterOrder!.AvailableQuantity
            .Should()
            .Be(8);

        inventoryAfterOrder.ReservedQuantity
            .Should()
            .Be(2);

        var cancelResponse =
            await _fixture.OrderClient.PostAsync(
                $"api/Orders/{order.Id}/cancel",
                null);

        cancelResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var inventoryAfterCancel =
            await _fixture.InventoryClient
                .GetFromJsonAsync<InventoryResponse>(
                    $"api/Inventory/{product.Id}");

        inventoryAfterCancel!.AvailableQuantity
            .Should()
            .Be(10);

        inventoryAfterCancel.ReservedQuantity
            .Should()
            .Be(0);
    }

    private class ProductResponse
    {
        public Guid Id { get; set; }
    }

    private class OrderResponse
    {
        public Guid Id { get; set; }

        public decimal TotalAmount { get; set; }
    }

    private class InventoryResponse
    {
        public int AvailableQuantity { get; set; }

        public int ReservedQuantity { get; set; }
    }
}