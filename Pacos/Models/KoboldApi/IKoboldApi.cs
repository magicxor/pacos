using Refit;

namespace Pacos.Models.KoboldApi;

public interface IKoboldApi
{
    HttpClient Client { get; }

    [Headers(
        "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/112.0",
        "Accept: */*",
        "Accept-Language: en,en-US;q=0.5",
        "Content-Type: application/json"
    )]
    [Post("/api/latest/generate/")]
    Task<KoboldResponse> Generate([Body] KoboldRequest koboldRequest, CancellationToken cancellationToken = default);
}
