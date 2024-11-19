using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.Stamp.Server.Options;
using ProjectOrigin.Stamp.Test.Extensions;
using ProjectOrigin.Stamp.Test.TestClassFixtures.Options;
using Testcontainers.PostgreSql;

namespace ProjectOrigin.Stamp.Test.TestClassFixtures;

public class ProjectOriginStack : RegistryFixture
{
    private readonly Lazy<IContainer> walletContainer;
    private readonly PostgreSqlContainer postgresContainer;

    private const int WalletHttpPort = 5000;

    private const string PathBase = "/wallet-api";

    private const string WalletAlias = "wallet-container";

    public ProjectOriginStack()
    {
        postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithNetwork(Network)
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .Build();

        var networkOptions = new NetworkOptions();
        networkOptions.Registries.Add(RegistryName, new RegistryInfo
        {
            Url = RegistryUrlWithinNetwork,
        });
        networkOptions.Areas.Add("DK1", new AreaInfo
        {
            IssuerKeys = new List<KeyInfo>{
                new (){
                    PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.ExportPkixText()))
                }
            }
        });
        networkOptions.Areas.Add("DK2", new AreaInfo
        {
            IssuerKeys = new List<KeyInfo>{
                new (){
                    PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk2IssuerKey.ExportPkixText()))
                }
            }
        });

        var configFile = networkOptions.ToTempYamlFile();

        walletContainer = new Lazy<IContainer>(() =>
        {
            var connectionString = $"Host={postgresContainer.IpAddress};Port=5432;Database=postgres;Username=postgres;Password=postgres";

            // Get an available port from system and use that as the host port
            var udp = new UdpClient(0, AddressFamily.InterNetwork);
            var hostPort = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;

            return new ContainerBuilder()
                .WithImage("ghcr.io/project-origin/vault:1.6.13")
                .WithName(WalletAlias + Guid.NewGuid())
                .WithNetwork(Network)
                .WithNetworkAliases(WalletAlias)
                .WithResourceMapping(configFile, "/app/tmp/")
                .WithPortBinding(hostPort, WalletHttpPort)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("ServiceOptions__EndpointAddress", $"http://localhost:{hostPort}/")
                .WithEnvironment("ServiceOptions__PathBase", PathBase)
                .WithEnvironment("network__ConfigurationUri", "file:///app/tmp/" + Path.GetFileName(configFile))
                .WithEnvironment("Otlp__Enabled", "false")
                .WithEnvironment("ConnectionStrings__Database", connectionString)
                .WithEnvironment("MessageBroker__Type", "InMemory")
                .WithEnvironment("auth__type", "jwt")
                .WithEnvironment("auth__jwt__AllowAnyJwtToken", "true")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingRetryCount", "5")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingInitialIntervalSeconds", "1")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingIntervalIncrementSeconds", "5")
                .WithEnvironment("Job__CheckForWithdrawnCertificatesIntervalInSeconds", "5")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilEndpointIsReady(WalletHttpPort, "/"))
                //.WithEnvironment("Logging__LogLevel__Default", "Trace")
                .Build();
        });
    }

    public RegistryOptions RegistryOptions => new()
    {
        IssuerPrivateKeyPems = new Dictionary<string, byte[]>
        {
            { "DK1", Encoding.UTF8.GetBytes(Dk1IssuerKey.ExportPkixText()) },
            { "DK2", Encoding.UTF8.GetBytes(Dk2IssuerKey.ExportPkixText()) },
        },
        Registries = new List<Server.Options.Registry>
        {
          new() {Name = RegistryName, Address = RegistryUrl }
        }
    };

    public string WalletUrl => new UriBuilder("http", walletContainer.Value.Hostname, walletContainer.Value.GetMappedPublicPort(WalletHttpPort), PathBase).Uri.ToString();

    public HttpClient CreateWalletClient(string subject)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(WalletUrl);
        var authentication = new AuthenticationHeaderValue("Bearer", GenerateToken(subject));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authentication.Scheme, authentication.Parameter);

        return client;
    }

    private static string GenerateToken(string subject)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", subject) }),
            Expires = DateTime.UtcNow.AddDays(7),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public override async Task InitializeAsync()
    {
        await Task.WhenAll(base.InitializeAsync(), postgresContainer.StartAsync());
        await walletContainer.Value.StartAsync();
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();

        await Task.WhenAll(
            postgresContainer.DisposeAsync().AsTask(),
            walletContainer.Value.DisposeAsync().AsTask());
    }
}
