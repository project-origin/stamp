using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Stamp.Server.Options;
public class RetryOptions
{
    public const string Retry = nameof(Retry);

    [Required]
    public int DefaultFirstLevelRetryCount { get; set; }

    [Required]
    public int RegistryTransactionStillProcessingRetryCount { get; set; }
    [Required]
    public int RegistryTransactionStillProcessingInitialIntervalSeconds { get; set; }
    [Required]
    public int RegistryTransactionStillProcessingIntervalIncrementSeconds { get; set; }
}
