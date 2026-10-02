namespace Framework.Alarm
{
    /// <summary>报警分类（MADB「分类」栏，哲学 §6.1）。</summary>
    public enum AlarmClass
    {
        /// <summary>安全。</summary>
        Safety,

        /// <summary>环境。</summary>
        Environment,

        /// <summary>设备。</summary>
        Equipment,

        /// <summary>工艺。</summary>
        Process,
    }
}
