namespace Recipe.Domain
{
    /// <summary>
    /// 相机配置参数实体：记录配方绑定的相机名称，继承配方参数基类。
    /// </summary>
    public class CameraConfiguration : RecipeParameter
    {
        /// <summary>相机名称。</summary>
        public string CameraName { get; set; }

        /// <summary>无参构造，供 ORM/序列化使用。</summary>
        public CameraConfiguration() { }
        /// <summary>构造函数：指定参数 Id、所属配方 Id 与相机名称。</summary>
        public CameraConfiguration(int id, int recipeId, string cameraName)
        {
            base.Id = id;
            RecipeId = recipeId;
            CameraName = cameraName;
        }
    }
}
