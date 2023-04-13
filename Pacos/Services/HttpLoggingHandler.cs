using System.Diagnostics;
using System.Net.Http.Headers;

namespace Pacos.Services;

public class HttpLoggingHandler : DelegatingHandler
{
    public HttpLoggingHandler(HttpMessageHandler? innerHandler = null)
        : base(innerHandler ?? new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var req = request;
        if (req.RequestUri == null || req.Content == null)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        
        var id = Guid.NewGuid().ToString();
        var msg = $"[{id} -   Request]";

        Debug.WriteLine($"{msg}========Start==========");
        Debug.WriteLine($"{msg} {req.Method} {req.RequestUri.PathAndQuery} {req.RequestUri.Scheme}/{req.Version}");
        Debug.WriteLine($"{msg} Host: {req.RequestUri.Scheme}://{req.RequestUri.Host}");

        foreach (var header in req.Headers)
            Debug.WriteLine($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

        if (req.Content != null)
        {
            foreach (var header in req.Content.Headers)
                Debug.WriteLine($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

            if (req.Content is StringContent || IsTextBasedContentType(req.Headers) ||
                this.IsTextBasedContentType(req.Content.Headers))
            {
                var result = await req.Content.ReadAsStringAsync(cancellationToken);

                Debug.WriteLine($"{msg} Content:");
                Debug.WriteLine($"{msg} {result}");
            }
        }

        var start = DateTime.Now;

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var end = DateTime.Now;

        Debug.WriteLine($"{msg} Duration: {end - start}");
        Debug.WriteLine($"{msg}==========End==========");

        msg = $"[{id} - Response]";
        Debug.WriteLine($"{msg}=========Start=========");

        var resp = response;

        Debug.WriteLine(
            $"{msg} {req.RequestUri.Scheme.ToUpper()}/{resp.Version} {(int)resp.StatusCode} {resp.ReasonPhrase}");

        foreach (var header in resp.Headers)
            Debug.WriteLine($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

        foreach (var header in resp.Content.Headers)
            Debug.WriteLine($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

        if (resp.Content is StringContent || this.IsTextBasedContentType(resp.Headers) ||
            this.IsTextBasedContentType(resp.Content.Headers))
        {
            start = DateTime.Now;
            var result = await resp.Content.ReadAsStringAsync(cancellationToken);
            end = DateTime.Now;

            Debug.WriteLine($"{msg} Content:");
            Debug.WriteLine($"{msg} {result}");
            Debug.WriteLine($"{msg} Duration: {end - start}");
        }

        Debug.WriteLine($"{msg}==========End==========");
        return response;
    }

    private readonly string[] _types = new[] { "html", "text", "xml", "json", "txt", "x-www-form-urlencoded" };

    private bool IsTextBasedContentType(HttpHeaders headers)
    {
        if (!headers.TryGetValues("Content-Type", out var values))
            return false;
        var header = string.Join(" ", values).ToLowerInvariant();

        return _types.Any(t => header.Contains(t));
    }
}