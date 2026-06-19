namespace BEScanCV.Application.Interfaces;

public interface IUploadProgressNotifier
{
    Task NotifyAsync(string batchId, object payload, CancellationToken cancellationToken = default);
}

