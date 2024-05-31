using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Stamp.Server.Options;
public class RetryOptions
{
    public const string Retry = nameof(Retry);

    [Required]
    public int DefaultFirstLevelRetryCount { get; set; }
}
