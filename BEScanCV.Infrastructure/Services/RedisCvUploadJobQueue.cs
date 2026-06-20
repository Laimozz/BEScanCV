using System.Text.Json;
using BEScanCV.Application.Interfaces;
using BEScanCV.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BEScanCV.Infrastructure.Services;

public sealed class RedisCvUploadJobQueue : ICvUploadJobQueue
{
    private const string RequeueScript = """
        local removed = redis.call('LREM', KEYS[1], 1, ARGV[1])
        if removed > 0 then
            redis.call('LPUSH', KEYS[2], ARGV[1])
        end
        return removed
        """;

    private const string RecoverScript = """
        local count = 0
        while true do
            local value = redis.call('RPOP', KEYS[1])
            if not value then
                break
            end
            redis.call('LPUSH', KEYS[2], value)
            count = count + 1
        end
        return count
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _database;
    private readonly RedisKey _pendingQueueKey;
    private readonly RedisKey _processingQueueKey;
    private readonly TimeSpan _pollingInterval;
    private readonly ILogger<RedisCvUploadJobQueue> _logger;

    public RedisCvUploadJobQueue(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisQueueOptions> options,
        ILogger<RedisCvUploadJobQueue> logger)
    {
        var queueOptions = options.Value;

        _database = connectionMultiplexer.GetDatabase(queueOptions.Database);
        _pendingQueueKey = queueOptions.UploadQueueKey;
        _processingQueueKey = queueOptions.UploadProcessingQueueKey;
        _pollingInterval = TimeSpan.FromMilliseconds(
            Math.Max(100, queueOptions.PollingIntervalMilliseconds));
        _logger = logger;
    }

    public async ValueTask EnqueueAsync(
        CvUploadJob job,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = Serialize(job);
        await _database.ListLeftPushAsync(_pendingQueueKey, payload);
    }

    public async ValueTask<CvUploadJob> DequeueAsync(
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var payload = await _database.ListRightPopLeftPushAsync(
                _pendingQueueKey,
                _processingQueueKey);

            if (payload.HasValue)
            {
                return Deserialize(payload);
            }

            await Task.Delay(_pollingInterval, cancellationToken);
        }
    }

    public async ValueTask AcknowledgeAsync(
        CvUploadJob job,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _database.ListRemoveAsync(
            _processingQueueKey,
            Serialize(job),
            count: 1);
    }

    public async ValueTask RequeueAsync(
        CvUploadJob job,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _database.ScriptEvaluateAsync(
            RequeueScript,
            [_processingQueueKey, _pendingQueueKey],
            [Serialize(job)]);
    }

    public async ValueTask<int> RecoverProcessingJobsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _database.ScriptEvaluateAsync(
            RecoverScript,
            [_processingQueueKey, _pendingQueueKey]);

        var recoveredJobs = (int)(long)result;
        if (recoveredJobs > 0)
        {
            _logger.LogWarning(
                "Recovered {RecoveredJobs} CV upload jobs from Redis processing queue.",
                recoveredJobs);
        }

        return recoveredJobs;
    }

    private static RedisValue Serialize(CvUploadJob job) =>
        JsonSerializer.Serialize(job, JsonOptions);

    private static CvUploadJob Deserialize(RedisValue payload)
    {
        var job = JsonSerializer.Deserialize<CvUploadJob>(payload.ToString(), JsonOptions);
        return job ?? throw new InvalidOperationException("Redis CV upload job payload is invalid.");
    }
}
