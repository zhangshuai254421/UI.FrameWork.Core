using System.ComponentModel;

namespace UI.FrameWork.Core.Main.RecipeView
{
    public class BaseViewShowModel:BindableBase
    {
        private int id;

        [Browsable(false)]
        public int Id 
        { 
            get => id;
            set => SetProperty(ref id, value);
        }
  
    }
}
