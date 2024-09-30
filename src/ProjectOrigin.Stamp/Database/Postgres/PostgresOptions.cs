using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Stamp.Database.Postgres;

public sealed class PostgresOptions
{
    [Required]
    public required string ConnectionString { get; set; }
}
