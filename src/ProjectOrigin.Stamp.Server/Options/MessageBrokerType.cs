using System.Text.Json.Serialization;

namespace ProjectOrigin.Stamp.Server.Options;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageBrokerType { InMemory, RabbitMq }
