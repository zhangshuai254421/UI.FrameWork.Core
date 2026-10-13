namespace SemiAppliaction.Views.Tests
{
    /// <summary>同步上下文替身：Post 即执行，跨线程派发走同步路径直接绕过（摘要 §9.2 测试技巧）；
    /// 列表 VM 与演示装置直通两套测试共用这一份（AlarmListViewModelTests / AlarmDemoTests）。</summary>
    internal sealed class SyncContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
    }
}
