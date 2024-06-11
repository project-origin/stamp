namespace ProjectOrigin.Stamp.Test.Extensions;

public static class DateTimeOffsetExtensions
{
    public static long RoundToLatestHour(this DateTimeOffset dateTimeOffset)
    {
        var rounded = dateTimeOffset.Date.AddHours(dateTimeOffset.Hour);
        return new DateTimeOffset(rounded).ToUnixTimeSeconds();
    }
}
