namespace Framework.Detection
{
    /// <summary>
    /// 一次分类的一个候选：类别名加置信度，按置信度降序排列（Top-K）。
    /// 分类没有位置概念——整张图一个结论，不像检测逐目标给框。
    /// </summary>
    public sealed record ClassificationResult(string Label, double Confidence);
}
