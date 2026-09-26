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
        private readonly CameraConfigurationEditInput input;

        public RecipeContextViewDto(CameraConfigurationEditInput input)
        {
            this.input = input;
            Id = input.Id;
        }

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

        public CameraConfigurationEditInput ToInput() => input;
    }
}
