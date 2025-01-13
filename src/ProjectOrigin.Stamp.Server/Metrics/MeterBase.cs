using System.Diagnostics.Metrics;

namespace ProjectOrigin.Stamp.Server.Metrics;

public class MeterBase(IMeterFactory meterFactory)
{
    public const string MeterName = "ProjectOrigin.Stamp";
    public Meter Meter { get; } = meterFactory.Create(MeterName);
}
