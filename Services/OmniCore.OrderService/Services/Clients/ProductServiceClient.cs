using OmniCore.OrderService.Models.External;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Services.Clients;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;

    public ProductServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductDto?> GetProductAsync(
        Guid productId,
        string authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/Products/{productId}");

        AddAuthorization(request, authorizationHeader);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductDto>(
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