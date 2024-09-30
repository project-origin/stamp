using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Stamp.Options;
public class RetryOptions
{
    public const string Retry = nameof(Retry);

    [Required]
    public int DefaultFirstLevelRetryCount { get; set; }

    [Required]
    public int RegistryTransactionStillProcessingRetryCount { get; set; }
}
