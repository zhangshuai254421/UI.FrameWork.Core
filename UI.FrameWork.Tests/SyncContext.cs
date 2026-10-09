namespace SemiAppliaction.Views.Tests
{
    /// <summary>同步编队替身：Post 即执行——跨线程编队用同步路径绕过（摘要 §9.2 测试技巧）；
    /// 列表 VM 与演示装置直通两套测试共用这一份（AlarmListViewModelTests / AlarmDemoRigTests）。</summary>
    internal sealed class SyncContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
    }
}
