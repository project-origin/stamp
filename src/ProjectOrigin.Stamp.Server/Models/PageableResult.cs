using System;
using System.Collections.Generic;
using System.Linq;
using ProjectOrigin.Stamp.Server.Services.REST.v1;

namespace ProjectOrigin.Stamp.Server.Models;

public record PageResult<T>
{
    public required IEnumerable<T> Items { get; init; }

    public required int Count { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required int TotalCount { get; init; }

    public ResultList<TR, PageInfo> ToResultList<TR>(Func<T, TR> map) => new()
    {
        Result = Items.Select(map),
        Metadata = new PageInfo
        {
            Count = Count,
            Offset = Offset,
            Limit = Limit,
            Total = TotalCount
        }
    };
}
