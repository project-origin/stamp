using System;
using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.Electricity.V1;

namespace ProjectOrigin.Stamp.ValueObjects;

public class Period : ValueObject
{
    public long DateFrom { get; private set; }
    public long DateTo { get; private set; }

    public Period(long dateFrom, long dateTo)
    {
        if (dateFrom >= dateTo)
            throw new ArgumentException("DateFrom must be smaller than DateTo");

        DateFrom = dateFrom;
        DateTo = dateTo;
    }

    public static Period? Parse(long dateFrom, long dateTo)
    {
        try
        {
            return new Period(dateFrom, dateTo);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public DateInterval ToDateInterval() =>
        new()
        {
            Start = Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeSeconds(DateFrom)),
            End = Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeSeconds(DateTo))
        };
}
