using System.ComponentModel;

namespace UI.FrameWork.Core.Main.RecipeView
{
    /// <summary>配方上下文展示项基类：为可编辑的展示行提供主键 Id。</summary>
    public class BaseViewShowModel:BindableBase
    {
        /// <summary>主键的后端字段。</summary>
        private int id;

        /// <summary>主键；标记为不可浏览，不在属性表格中显示。</summary>
        [Browsable(false)]
        public int Id 
        { 
            get => id;
            set => SetProperty(ref id, value);
        }
  
    }
}
