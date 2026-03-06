using DocSenseV1.Services.Job;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocSenseV1Test.Services.Job
{
    public class JobQueueTest
    {
        [Fact]
        public async Task EnqueueAndDequeue_ShouldMaintainOrder()
        {
            // Arrange
            var queue = new JobQueue(capacity: 10);
            var task1 = new JobTask("id1", "text1", "user1", 1);
            var task2 = new JobTask("id2", "text2", "user2", 1);

            // Act
            await queue.EnqueueAsync(task1);
            await queue.EnqueueAsync(task2);

            var results = new List<JobTask>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Читаем задачи через IAsyncEnumerable
            await foreach (var task in queue.DequeueAllAsync(cts.Token))
            {
                results.Add(task);
                if(results.Count == 2)
                    break;
            }

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Equal("id1", results[0].JobId);
            Assert.Equal("id2", results[1].JobId);
        }
    }
}
