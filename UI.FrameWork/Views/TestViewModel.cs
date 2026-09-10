using Framework.Device;
using Framework.Device.HikVision;
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
        IDevice device;
        public DelegateCommand Test1Command { get; }
        public TestViewModel() 
        {
            Test1Command = new DelegateCommand(Test1);
        }
        public void Test1()
        {
             device = new CameraHikVision();
            device.Open();
        }
    }
}
