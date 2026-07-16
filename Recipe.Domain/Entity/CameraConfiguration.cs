namespace Recipe.Domain
{
    public class CameraConfiguration : RecipeParameter
    {
        public string CameraName { get; set; }

        public CameraConfiguration() { }
        public CameraConfiguration(int id, int recipeId, string cameraName)
        {
            base.Id = id;
            RecipeId = recipeId;
            CameraName = cameraName;
        }
    }
}
