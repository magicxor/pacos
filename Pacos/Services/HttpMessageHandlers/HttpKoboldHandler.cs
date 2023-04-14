using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Pacos.Services.HttpMessageHandlers;

public class HttpKoboldHandler : DelegatingHandler
{
    public HttpKoboldHandler() : base()
    {
    }

    public HttpKoboldHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    private static readonly string[] TextContentTypes = { "html", "text", "xml", "json", "txt", "x-www-form-urlencoded" };

    private static bool IsTextBasedContentType(HttpHeaders headers)
    {
        if (!headers.TryGetValues("Content-Type", out var values))
            return false;
        var header = string.Join(" ", values).ToLowerInvariant();

        return TextContentTypes.Any(t => header.Contains(t));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is not null &&
            (request.Content is StringContent or JsonContent
                            || IsTextBasedContentType(request.Headers)
                            || IsTextBasedContentType(request.Content.Headers)))
        {
            // use this trick to have
            // "localAddress" : "172.17.0.5:1080"
            // instead of
            // "localAddress" : "41777854c23f/172.17.0.5:1080"
            var result = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
