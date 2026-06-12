using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main
{
    public class NumberKeyPadViewModel : BindableBase, IJournalAware, INavigationAware
    {
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
           return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
       
        }

        public bool PersistInHistory()
        {
            return false;
        }
    }
}
