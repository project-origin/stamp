using System;
using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.Electricity.V1;

namespace ProjectOrigin.Stamp.Server.Helpers;

public static class PeriodHelper
{
    public static DateInterval ToDateInterval(long start, long end) =>
        new()
        {
            Start = Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeSeconds(start)),
            End = Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeSeconds(end))
        };
}
