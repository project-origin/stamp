using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectOrigin.Stamp.Services.REST.v1;

namespace ProjectOrigin.Stamp.Test.Extensions;

public static class WalletServiceClientExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public static async Task<IEnumerable<WalletCertificate>> QueryCertificates(this HttpClient client)
    {
        var response = await client.GetFromJsonAsync<ResultList<WalletCertificate>>("v1/certificates",
            options: JsonSerializerOptions);

        return response!.Result.ToList();
    }

    public static async Task<WalletEndpointReferenceDto> CreateWalletAndEndpoint(this HttpClient client)
    {
        var request = new CreateWalletRequest
        {
            PrivateKey = null
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res1 = await client.PostAsync("v1/wallets", content);
        res1.EnsureSuccessStatusCode();

        if (res1 == null || res1.Content == null)
            throw new HttpRequestException("Failed to create wallet.");

        var createResponse = (await res1.Content.ReadFromJsonAsync<CreateWalletResponse>())!;

        var res2 = await client.PostAsync($"v1/wallets/{createResponse.WalletId}/endpoints", null);
        res2.EnsureSuccessStatusCode();

        if (res2 == null || res2.Content == null)
            throw new HttpRequestException("Failed to create wallet endpoint.");

        var response = (await res2.Content.ReadFromJsonAsync<CreateWalletEndpointResponse>())!;
        return response.WalletReference;
    }

    public static async Task<IList<WalletCertificate>> RepeatedlyGetCertificatesUntil(this HttpClient client, Func<IEnumerable<WalletCertificate>, bool> condition, TimeSpan? timeLimit = null)
    {
        if (timeLimit.HasValue && timeLimit.Value <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(timeLimit)} must be a positive time span");

        var limit = timeLimit ?? TimeSpan.FromSeconds(30);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var response = await client.GetFromJsonAsync<ResultList<WalletCertificate>>("v1/certificates",
                options: JsonSerializerOptions);

            if (condition(response!.Result))
                return response.Result.ToList();

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception(
            $"Condition for certificates in wallet not met within time limit ({limit.TotalSeconds} seconds)");
    }
}

public record CreateWalletRequest()
{
    /// <summary>
    /// The private key to import. If not provided, a private key will be generated.
    /// </summary>
    public byte[]? PrivateKey { get; init; }
}

public record CreateWalletResponse()
{
    public Guid WalletId { get; init; }
}

public record CreateWalletEndpointResponse(WalletEndpointReferenceDto WalletReference);

/// <summary>
/// A certificate that is available to use in the wallet.
/// </summary>
public record WalletCertificate()
{
    /// <summary>
    /// The id of the certificate.
    /// </summary>
    public required FederatedStreamId FederatedStreamId { get; init; }

    /// <summary>
    /// The quantity available on the certificate.
    /// </summary>
    public required uint Quantity { get; init; }

    /// <summary>
    /// The start of the certificate.
    /// </summary>
    public required long Start { get; init; }

    /// <summary>
    /// The end of the certificate.
    /// </summary>
    public required long End { get; init; }

    /// <summary>
    /// The Grid Area of the certificate.
    /// </summary>
    public required string GridArea { get; init; }

    /// <summary>
    /// The type of certificate (production or consumption).
    /// </summary>
    public required CertificateType CertificateType { get; init; }

    /// <summary>
    /// The attributes of the certificate.
    /// </summary>
    public required Dictionary<string, string> Attributes { get; init; }
}


public record ResultList<T>()
{
    public required IEnumerable<T> Result { get; init; }
    public required PageInfo Metadata { get; init; }
}
public record PageInfo()
{
    public required int Count { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required int Total { get; init; }
}

public record FederatedStreamId()
{
    public required string Registry { get; init; }
    public required Guid StreamId { get; init; }
}

public enum CertificateType
{
    Consumption = 1,
    Production = 2
}
