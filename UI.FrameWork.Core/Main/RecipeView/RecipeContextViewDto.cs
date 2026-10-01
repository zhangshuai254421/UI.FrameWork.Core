using Recipe.Infrastructure.Contracts;

namespace UI.FrameWork.Core.Main.RecipeView
{
    /// <summary>
    /// 相机配置编辑行的 INPC 薄包装（ADR 0002）：属性写透到底层
    /// <see cref="CameraConfigurationEditInput"/>，保存时把 Input 原样交给 service——
    /// VM→实体方向零映射；离开页面不保存即丢弃，天然可取消。
    /// </summary>
    public class RecipeContextViewDto : BaseViewShowModel
    {
        /// <summary>被包装的编辑输入对象；属性写入直接透传到它。</summary>
        private readonly CameraConfigurationEditInput input;

        /// <summary>包装编辑输入对象并同步其主键到展示行。</summary>
        public RecipeContextViewDto(CameraConfigurationEditInput input)
        {
            this.input = input;
            Id = input.Id;
        }

        /// <summary>相机名称；写入透传到底层 Input 并通知界面刷新，绑定配方上下文视图的编辑行。</summary>
        public string CameraName
        {
            get => input.CameraName;
            set
            {
                if (input.CameraName == value) return;
                input.CameraName = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>取回底层编辑输入对象；保存时原样交给服务，不做二次映射。</summary>
        public CameraConfigurationEditInput ToInput() => input;
    }
}
