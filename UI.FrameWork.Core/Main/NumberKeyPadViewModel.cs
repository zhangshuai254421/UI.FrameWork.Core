using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main
{
    public class NumberKeyPadViewModel : BindableBase, IJournalAware
    {
        public bool PersistInHistory()
        {
            return false;
        }
    }
}
