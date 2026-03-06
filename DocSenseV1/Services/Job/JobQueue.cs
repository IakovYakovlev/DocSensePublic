
using System.Threading.Channels;

namespace DocSenseV1.Services.Job
{
    public class JobQueue : IJobQueue
    {
        // BoundedChannel ограничивает количество задач в памяти, чтобы не переполнять память
        private readonly Channel<JobTask> _queue;

        public JobQueue(int capacity = 100)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait // Если канал заполнен, ждать освобождения места
            };
            _queue = Channel.CreateBounded<JobTask>(options);
        }

        public async ValueTask EnqueueAsync(JobTask task, CancellationToken ct = default)
        {
            await _queue.Writer.WriteAsync(task, ct);
        }

        public IAsyncEnumerable<JobTask> DequeueAllAsync(CancellationToken ct = default)
        {
            return _queue.Reader.ReadAllAsync(ct);
        }
    }
}
