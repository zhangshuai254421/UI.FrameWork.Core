// Copyright (c) 2026 ZhangShuai. All rights reserved.
// 项目：UI.FrameWork —— EFCore.Repository（EF Core 数据访问抽象层）

namespace EFCore.Repository
{
    /// <summary>
    /// 分页查询结果：<see cref="Total"/> 为符合条件的总记录数（用于计算总页数），
    /// <see cref="Data"/> 为当前页数据；无数据时为空序列而非 null。
    /// </summary>
    /// <typeparam name="TModel">数据项类型</typeparam>
    public class PagedResult<TModel>
    {
        public int Total { get; set; }

        public IEnumerable<TModel> Data { get; set; } = Enumerable.Empty<TModel>();
    }
}
