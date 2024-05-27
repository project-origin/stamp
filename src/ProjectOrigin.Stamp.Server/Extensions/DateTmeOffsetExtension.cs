using System;

namespace ProjectOrigin.Stamp.Server.Extensions;

public static class DateTmeOffsetExtension
{
    public static DateTimeOffset ToUtcTime(this DateTimeOffset date)
    {
        return DateTimeOffset.FromUnixTimeSeconds(date.ToUnixTimeSeconds());
    }
}
