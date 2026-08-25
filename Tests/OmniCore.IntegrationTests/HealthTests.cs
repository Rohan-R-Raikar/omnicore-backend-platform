using FluentAssertions;
using System.Threading.Tasks;

namespace OmniCore.IntegrationTests;

public class HealthTests
{
    private readonly ApiTestFixture _fixture = new();

    [Fact]
    public async Task AuthService_Health_ReturnsSuccess()
    {
        var response =
            await _fixture.AuthClient.GetAsync("health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task ProductService_Health_ReturnsSuccess()
    {
        var response =
            await _fixture.ProductClient.GetAsync("health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task InventoryService_Health_ReturnsSuccess()
    {
        var response =
            await _fixture.InventoryClient.GetAsync("health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task OrderService_Health_ReturnsSuccess()
    {
        var response =
            await _fixture.OrderClient.GetAsync("health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}