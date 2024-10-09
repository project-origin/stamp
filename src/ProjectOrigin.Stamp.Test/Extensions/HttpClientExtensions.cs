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

    public static async Task<HttpResponseMessage> PostCertificate(this HttpClient client, Guid recipientId, string registryName, string meteringPointId, CertificateDto certificate)
    {
        var request = new CreateCertificateRequest
        {
            RecipientId = recipientId,
            RegistryName = registryName,
            Certificate = certificate,
            MeteringPointId = meteringPointId
        };

        return await client.PostAsJsonAsync("/stamp-api/v1/certificates", request);
    }

    public static async Task<ResultList<WithdrawnCertificateDto, PageInfo>?> GetWithdrawnCertificates(this HttpClient client, int withdrawnCertificateId)
    {
        return await client.GetFromJsonAsync<ResultList<WithdrawnCertificateDto, PageInfo>>($"/stamp-api/v1/certificates/withdrawn?lastWithdrawnId={withdrawnCertificateId}");
    }

    public static async Task<HttpResponseMessage> WithdrawCertificate(this HttpClient client, string registry, Guid certificateId)
    {
        return await client.PostAsync($"v1/certificates/{registry}/{certificateId}/withdraw", new StringContent(""));
    }
}
