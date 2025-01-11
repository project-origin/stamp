using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;

namespace ProjectOrigin.Stamp.Server.Metrics;

public class MeterBase(IMeterFactory meterFactory, IConfiguration configuration)
{
    public const string MeterName = "ProjectOrigin.Stamp";
    public Meter Meter { get; } = meterFactory.Create(configuration["StampMeterName"] ?? MeterName);
}
