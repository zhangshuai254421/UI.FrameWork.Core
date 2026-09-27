using Framework.Device;
using Microsoft.Extensions.Logging;
using PrismUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemiAppliaction.Views
{
    public class TestViewModel : BaseViewModel
    {
        private readonly DeviceManager _deviceManager;

        public DelegateCommand Test1Command { get; }

        public TestViewModel(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
            Test1Command = new DelegateCommand(Test1);
        }

        public void Test1()
        {
            // 通过设备标识从设备管理器取已初始化设备，不硬编码具体厂商适配器。
            IDevice? device = _deviceManager.GetDevice("CameraA");
            var s = device as CameraBase;
            s.StartAcquisition();
             var result = s.GetOneImage();
            CameraImageFile.Save(result, @"D://1.bmp");
        }
    }
}
