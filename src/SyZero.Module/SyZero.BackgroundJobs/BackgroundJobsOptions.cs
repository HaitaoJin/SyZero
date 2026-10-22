namespace SyZero.BackgroundJobs
{
    /// <summary>后台任务模块配置。</summary>
    public class BackgroundJobsOptions
    {
        /// <summary>队列容量。0 = 无界（注意内存占用）。</summary>
        public int QueueCapacity { get; set; } = 1024;

        /// <summary>队列已满时是否丢弃最旧任务（默认 false：拒绝新任务并返回 false）。</summary>
        public bool DropOldestWhenFull { get; set; } = false;

        /// <summary>并发 Worker 数。</summary>
        public int WorkerCount { get; set; } = 1;
    }
}
