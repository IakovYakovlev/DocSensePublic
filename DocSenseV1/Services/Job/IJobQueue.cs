namespace DocSenseV1.Services.Job
{
    public record JobTask (string JobId, string Text, string UserId, int PlanId);
    public interface IJobQueue
    {
        ValueTask EnqueueAsync(JobTask task, CancellationToken ct = default);
        IAsyncEnumerable<JobTask> DequeueAllAsync(CancellationToken ct = default);
    }
}
