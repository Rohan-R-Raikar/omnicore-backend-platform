using OmniCore.OrderService.Models.External;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Services.Clients;

public class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;

    public InventoryServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<InventoryDto?> ReserveAsync(
        Guid productId,
        int quantity,
        string authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        return SendQuantityRequestAsync(
            $"api/Inventory/{productId}/reserve",
            quantity,
            authorizationHeader,
            cancellationToken);
    }

    public Task<InventoryDto?> ReleaseAsync(
        Guid productId,
        int quantity,
        string authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        return SendQuantityRequestAsync(
            $"api/Inventory/{productId}/release",
            quantity,
            authorizationHeader,
            cancellationToken);
    }

    private async Task<InventoryDto?> SendQuantityRequestAsync(
        string url,
        int quantity,
        string authorizationHeader,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            url);

        AddAuthorization(request, authorizationHeader);

        request.Content = JsonContent.Create(
            new InventoryQuantityRequest
            {
                Quantity = quantity
            });

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<InventoryDto>(
                cancellationToken: cancellationToken);
    }

    private static void AddAuthorization(
        HttpRequestMessage request,
        string authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return;
        }

        request.Headers.Authorization =
            AuthenticationHeaderValue.Parse(authorizationHeader);
    }
}