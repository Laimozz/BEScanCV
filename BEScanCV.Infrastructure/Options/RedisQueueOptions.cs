namespace BEScanCV.Infrastructure.Options;

public sealed class RedisQueueOptions
{
    public const string SectionName = "Redis";

    public int Database { get; set; }
    public string UploadQueueKey { get; set; } = "bescancv:cv-upload:pending";
    public string UploadProcessingQueueKey { get; set; } = "bescancv:cv-upload:processing";
    public int PollingIntervalMilliseconds { get; set; } = 500;
}
