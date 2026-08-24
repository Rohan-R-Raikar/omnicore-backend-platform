using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Services.Clients;

public class CorrelationIdHandler : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-ID";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;

        if (context is not null &&
            context.Items.TryGetValue(
                HeaderName,
                out var correlationId) &&
            correlationId is not null)
        {
            request.Headers.Remove(HeaderName);

            request.Headers.TryAddWithoutValidation(
                HeaderName,
                correlationId.ToString());
        }

        return await base.SendAsync(
            request,
            cancellationToken);
    }
}