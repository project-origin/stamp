namespace ProjectOrigin.Stamp.Test.Extensions;

public static class DateTimeOffsetExtensions
{
    public static long RoundToLatestHourLong(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.RoundToLatestHour().ToUnixTimeSeconds();
    }
    public static DateTimeOffset RoundToLatestHour(this DateTimeOffset dateTimeOffset)
    {
        var rounded = dateTimeOffset.Date.AddHours(dateTimeOffset.Hour);
        return new DateTimeOffset(rounded);
    }
}
