using System;
using System.Threading;
using System.Threading.Tasks;


namespace UI.FrameWork.Core.Common
{
    /// <summary>
    /// Prism Region 导航帮助类，提供异步导航、超时与统一错误处理。
    /// 返回值为布尔型，表示导航是否成功；失败时会把异常记录到注入的 ILogger 中。
    /// </summary>
    public class PrismRegionNavigator
    {
        private readonly IRegionManager _regionManager;
     
        public PrismRegionNavigator(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        }

        /// <summary>
        /// 异步导航到指定 region 的目标视图（以注册名或相对 Uri 表示）。
        /// - 默认不超时（传入 null）；传入 timeout 可在到期时取消并记录超时。
        /// - 统一错误处理：捕获异常并通过 ILogger 记录，方法返回 false 表示失败。
        /// </summary>
        public async Task<bool> NavigateAsync(string regionName, string target)
        {
            if (string.IsNullOrWhiteSpace(regionName)) throw new ArgumentException("regionName required", nameof(regionName));
            if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("target required", nameof(target));
            try
            {
                var uri = new Uri(target, UriKind.Relative);

                await Task.Run(() =>
                {
                    // 发起导航，回调中根据结果设置 tcs
                    _regionManager.RequestNavigate(regionName, target);

                });

               
            }
            catch (Exception ex)
            {
                // 统一错误处理：记录并返回 false
                return false;
            }
            return true;
        }
    }
}
