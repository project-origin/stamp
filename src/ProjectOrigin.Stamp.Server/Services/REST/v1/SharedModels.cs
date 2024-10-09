using System.Collections.Generic;

namespace ProjectOrigin.Stamp.Server.Services.REST.v1;

public record ResultList<T, TPageInfo>()
{
    public required IEnumerable<T> Result { get; init; }
    public required TPageInfo Metadata { get; init; }
}

public record PageInfo()
{
    public required int Count { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required int Total { get; init; }
}
