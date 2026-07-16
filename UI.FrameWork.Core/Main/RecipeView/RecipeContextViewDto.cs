namespace UI.FrameWork.Core.Main.RecipeView
{
    public class RecipeContextViewDto : BaseViewShowModel
    {
        private string cameraName;
        public string CameraName
        {
            get => cameraName;
            set => SetProperty(ref cameraName, value);
        }
    }
}
