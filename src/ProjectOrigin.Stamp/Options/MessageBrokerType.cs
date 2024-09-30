using System.Text.Json.Serialization;

namespace ProjectOrigin.Stamp.Options;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageBrokerType { InMemory, RabbitMq }
