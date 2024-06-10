using System;
using MassTransit;
using ProjectOrigin.Stamp.Server.EventHandlers;
using ProjectOrigin.Stamp.Server.Options;
using ProjectOrigin.Stamp.Server.Serialization;

namespace ProjectOrigin.Stamp.Server.Extensions;

public static class IBusRegistrationConfiguratorExtensions
{
    public static void ConfigureMassTransitTransport(this IBusRegistrationConfigurator busConfig, MessageBrokerOptions options)
    {
        switch (options.Type)
        {
            case MessageBrokerType.InMemory:
                Serilog.Log.Logger.Warning("MessageBroker.Type is set to InMemory, this is not recommended for production use, messages are not durable");

                busConfig.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureDefaults(context);
                });
                break;

            case MessageBrokerType.RabbitMq:
                busConfig.UsingRabbitMq((context, cfg) =>
                {
                    //cfg.ConfigureDefaults(context);
                    cfg.ConfigureEndpoints(context);

                    //cfg.ReceiveEndpoint("certificate-created-event-handler2",
                    //    e => e.ConfigureConsumer<CertificateCreatedEventHandler>(context));

                    var rabbitOption = options.RabbitMq!;
                    cfg.Host(rabbitOption.Host, rabbitOption.Port, "/", h =>
                    {
                        h.Username(rabbitOption.Username);
                        h.Password(rabbitOption.Password);
                    });
                });
                break;

            default:
                throw new NotSupportedException($"Message broker type ”{options.Type}” not supported");
        }

    }

    private static void ConfigureDefaults<T>(this IBusFactoryConfigurator<T> cfg, IBusRegistrationContext context) where T : IReceiveEndpointConfigurator
    {
        cfg.ConfigureEndpoints(context);
        cfg.UseInMemoryOutbox(context);

        cfg.ConfigureJsonSerializerOptions(options =>
        {
            options.Converters.Add(new TransactionConverter());
            return options;
        });
    }
}
