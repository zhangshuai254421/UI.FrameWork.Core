using System.Linq.Expressions;

// Copyright (c) 2026 ZhangShuai. All rights reserved.
// 项目：UI.FrameWork —— EFCore.Repository（EF Core 数据访问抽象层）

namespace EFCore.Repository
{
    /// <summary>
    /// 分页查询参数：<see cref="PageNum"/> 从 1 开始计页，<see cref="PageSize"/> 为每页条数（默认 15）。
    /// </summary>
    public class PageParameter
    {
        public int PageNum { get; set; } = 1;

        public int PageSize { get; set; } = 15;
    }

    /// <summary>
    /// 带过滤条件的分页参数：在基础分页字段之外附加类型化的过滤对象 <see cref="Filter"/>，
    /// 用于"分页 + 条件"的组合查询；构造时从传入的基础 <see cref="PageParameter"/> 拷贝分页字段。
    /// </summary>
    /// <typeparam name="TFilter">过滤条件对象类型</typeparam>
    public class PageParameter<TFilter> : PageParameter
    {
        public PageParameter(PageParameter page, TFilter filter)
        {
            PageNum = page.PageNum;

            PageSize = page.PageSize;

            Filter = filter;
        }

        public TFilter Filter { get; }
    }
}
