using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using ProjectOrigin.Stamp.Server.Services.REST.v1;

namespace ProjectOrigin.Stamp.Test.Extensions;

public static class HttpClientExtensions
{
    public static async Task<T?> ReadJson<T>(this HttpContent content)
    {
        var options = GetJsonSerializerOptions();
        return await content.ReadFromJsonAsync<T>(options);
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static async Task<Guid> AddRecipient(this HttpClient client, WalletEndpointReferenceDto walletEndpointRef)
    {
        var request = new CreateRecipientRequest
        {
            WalletEndpointReference = walletEndpointRef
        };

        var response = await client.PostAsJsonAsync("/stamp-api/v1/recipients", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadJson<CreateRecipientResponse>())!.Id;
    }

    public static async Task PostCertificate(this HttpClient client, Guid recipientId, string registryName, CertificateDto certificate)
    {
        var request = new CreateCertificateRequest
        {
            RecipientId = recipientId,
            RegistryName = registryName,
            Certificate = certificate
        };

        var response = await client.PostAsJsonAsync("/stamp-api/v1/certificates", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
