using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main
{
    /// <summary>数字键盘页 ViewModel：目前作为占位实现，导航接口均为空操作。</summary>
    public class NumberKeyPadViewModel : BindableBase, IJournalAware, INavigationAware
    {
        /// <summary>始终复用当前实例作为导航目标。</summary>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
           return true;
        }

        /// <summary>离开页面时无需清理，空实现。</summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }

        /// <summary>进入页面时无需初始化，空实现。</summary>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
       
        }

        /// <summary>不进入导航历史，点击返回无法回到数字键盘页。</summary>
        public bool PersistInHistory()
        {
            return false;
        }
    }
}
