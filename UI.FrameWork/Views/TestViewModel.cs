using Framework.Core.Common;
using PrismUI.Core;

namespace SemiAppliaction.Views
{
    // 作者：Zhang Shuai
    // 描述：测试中心页 ViewModel——各测试入口按钮的导航。
    public class TestViewModel : BaseViewModel
    {
        //Test1Command：导航到相机测试页（真实相机取流喂 ImageViewer）
        public DelegateCommand Test1Command { get; }

        //DetectionTestCommand：导航到检测测试页（选图跑 YOLO 推理）
        public DelegateCommand DetectionTestCommand { get; }

        public TestViewModel()
        {
            Test1Command = new DelegateCommand(() =>
                IoC.Get<INavigationService>().NavigateToAsync(nameof(CameraTestView), RegionNames.MainRegion));

            DetectionTestCommand = new DelegateCommand(() =>
                IoC.Get<INavigationService>().NavigateToAsync(nameof(DetectionTestView), RegionNames.MainRegion));
        }
    }
}
