using FluentAssertions;
using ProjectOrigin.Stamp.ValueObjects;
using Xunit;

namespace ProjectOrigin.Stamp.Test.ValueObjects;

public class PeriodTests
{
    [Fact]
    public void ShouldCreate()
    {
        var dateFrom = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var dateTo = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        var period = new Period(dateFrom, dateTo);

        period.DateFrom.Should().Be(dateFrom);
        period.DateTo.Should().Be(dateTo);
    }

    [Theory]
    [InlineData(1718366646, 1718366646)] //14/06/2024 14:04:06
    [InlineData(1718366647, 1718366646)] //14/06/2024 14:04:07
    public void ShouldThrowArgumentException_WhenDateFromIsGreaterThanOrEqualToDateTo(long dateFrom, long dateTo)
    {
        Action act = () => new Period(dateFrom, dateTo);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldParse()
    {
        var dateFrom = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var dateTo = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        var period = Period.Parse(dateFrom, dateTo);

        Assert.NotNull(period);
        Assert.Equal(dateFrom, period.DateFrom);
        Assert.Equal(dateTo, period.DateTo);
    }

    [Theory]
    [InlineData(1718366646, 1718366646)] //14/06/2024 14:04:06
    [InlineData(1718366647, 1718366646)] //14/06/2024 14:04:07
    public void ShouldNotParse(long dateFrom, long dateTo)
    {
        var period = Period.Parse(dateFrom, dateTo);

        Assert.Null(period);
    }

    [Fact]
    public void Equals_WhenSameValues_ExpectTrue()
    {
        var dateFrom = 123L;
        var dateTo = 124L;
        var period1 = new Period(dateFrom, dateTo);
        var period2 = new Period(dateFrom, dateTo);

        period1.Equals(period2).Should().BeTrue();
        (period1 == period2).Should().BeTrue();
    }

    [Fact]
    public void ToDateInterval()
    {
        var dateFrom = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var dateTo = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        var period = new Period(dateFrom, dateTo);

        var dateInterval = period.ToDateInterval();

        dateInterval.Start.Seconds.Should().Be(dateFrom);
        dateInterval.End.Seconds.Should().Be(dateTo);
    }
}
