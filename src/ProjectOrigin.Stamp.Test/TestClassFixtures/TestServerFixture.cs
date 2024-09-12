#region Copyright notice and license
// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/grpc/test-services/sample/Tests/Server/IntegrationTests/Helpers/GrpcTestFixture.cs
#endregion

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.Stamp.Test.TestClassFixtures.TestServerHelpers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using ProjectOrigin.Stamp.Server.Options;
using Xunit.Abstractions;
using ProjectOrigin.HierarchicalDeterministicKeys;
using System.Text;

namespace ProjectOrigin.Stamp.Test.TestClassFixtures
{
    public delegate void LogMessage(LogLevel logLevel, string categoryName, EventId eventId, string message, Exception? exception);

    public class TestServerFixture<TStartup> : IDisposable where TStartup : class
    {
        private TestServer? _server;
        private IHost? _host;
        private HttpMessageHandler? _handler;
        private Dictionary<string, string?>? _configurationDictionary;
        private bool _disposed = false;
        public event Action<IServiceCollection>? ConfigureTestServices;

        public event LogMessage? LoggedMessage;

        public string PostgresConnectionString { get; set; } = "http://foo";

        public RetryOptions RetryOptions { get; set; } = new()
        {
            DefaultFirstLevelRetryCount = 5,
            RegistryTransactionStillProcessingRetryCount = 100
        };

        public RabbitMqOptions RabbitMqOptions { get; set; } = new()
        {
            Host = "localhost",
            Port = 5672,
            Username = "guest",
            Password = "quest"
        };

        public RegistryOptions RegistryOptions { get; set; } = new()
        {
            Registries = new List<Server.Options.Registry>(),
            IssuerPrivateKeyPems = new Dictionary<string, byte[]>()
        };

        public TestServerFixture()
        {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(new ForwardingLoggerProvider((logLevel, category, eventId, message, exception) =>
            {
                LoggedMessage?.Invoke(logLevel, category, eventId, message, exception);
            }));
        }

        public T GetRequiredService<T>() where T : class
        {
            EnsureServer();
            return _host!.Services.GetRequiredService<T>();
        }

        public void ConfigureHostConfiguration(Dictionary<string, string?> configuration)
        {
            if (_configurationDictionary != null)
                foreach (var keyValuePair in configuration)
                {
                    _configurationDictionary[keyValuePair.Key] = keyValuePair.Value;
                }
            else
                _configurationDictionary = configuration;
        }

        private void EnsureServer()
        {
            if (_host == null)
            {
                var envVariables = new Dictionary<string, string?>
                {
                    {"Otlp:Enabled", "false"},
                    {"RestApiOptions:PathBase", "/stamp-api"},
                    {"MessageBroker:Type", "RabbitMq"},
                    {"MessageBroker:RabbitMq:Host", RabbitMqOptions.Host},
                    {"MessageBroker:RabbitMq:Port", RabbitMqOptions.Port.ToString()},
                    {"MessageBroker:RabbitMq:Username", RabbitMqOptions.Username},
                    {"MessageBroker:RabbitMq:Password", RabbitMqOptions.Password},
                    {"ConnectionStrings:Database", PostgresConnectionString},
                    {"Retry:DefaultFirstLevelRetryCount", RetryOptions.DefaultFirstLevelRetryCount.ToString()},
                    {"Retry:RegistryTransactionStillProcessingRetryCount", RetryOptions.RegistryTransactionStillProcessingRetryCount.ToString()}
                };

                for (var i = 0; i < RegistryOptions.Registries.Count; i++)
                {
                    var registry = RegistryOptions.Registries[i];
                    envVariables.Add($"Registries:{i}:Name", registry.Name);
                    envVariables.Add($"Registries:{i}:Address", registry.Address);
                }

                foreach (var pem in RegistryOptions.IssuerPrivateKeyPems)
                {
                    envVariables.Add($"IssuerPrivateKeyPems:{pem.Key}", Convert.ToBase64String(pem.Value));
                }

                ConfigureHostConfiguration(envVariables);

                var builder = new HostBuilder();

                if (_configurationDictionary != null)
                {
                    builder.ConfigureHostConfiguration(config =>
                        {
                            config.Add(new MemoryConfigurationSource()
                            {
                                InitialData = _configurationDictionary
                            });
                        });
                }

                builder
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(LoggerFactory);
                    })
                    .ConfigureWebHostDefaults(webHost =>
                    {
                        webHost
                            .UseTestServer()
                            .UseEnvironment("Development")
                            .UseStartup<TStartup>();
                    })
                    .ConfigureServices(services =>
                    {
                        if (ConfigureTestServices != null)
                            ConfigureTestServices.Invoke(services);
                    });

                _host = builder.Start();
                _server = _host.GetTestServer();
                _handler = _server.CreateHandler();
            }
        }

        public LoggerFactory LoggerFactory { get; }

        public HttpClient CreateHttpClient()
        {
            EnsureServer();

            var client = _server!.CreateClient();
            return client;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _handler?.Dispose();
                    _host?.Dispose();
                    _server?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TestServerFixture()
        {
            Dispose(false);
        }

        public IDisposable GetTestLogger(ITestOutputHelper outputHelper)
        {
            return new TestServerContext<TStartup>(this, outputHelper);
        }
    }
}

