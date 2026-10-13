// Copyright (c) 2026 ZhangShuai. All rights reserved.
// 项目：UI.FrameWork —— EFCore.Repository（EF Core 数据访问抽象层）

namespace EFCore.Repository
{
    /// <summary>
    /// 数据库事务句柄：封装一次已开启的事务，支持提交（<see cref="CommitAsync"/>）与回滚
    /// （<see cref="RollbackAsync"/>），并支持保存点（savepoint）做局部回滚——
    /// <see cref="CreateSavepointAsync"/> 建保存点、<see cref="RollbackToSavepointAsync"/> 回退到保存点、
    /// <see cref="ReleaseSavepointAsync"/> 释放保存点；<see cref="SupportsSavepoints"/> 指示提供程序是否支持保存点。
    /// <para>实现 <see cref="IDisposable"/>：Dispose 即结束事务，未提交的修改将被丢弃。</para>
    /// </summary>
    public interface IUnitOfTransaction : IDisposable
    {
        /// <summary>当前数据库提供程序是否支持保存点（局部回滚）。</summary>
        bool SupportsSavepoints { get; }
        /// <summary>在事务内创建指定名称的保存点；提供程序不支持时抛出 NotSupportedException。</summary>
        Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
        /// <summary>回滚到指定保存点（撤销该点之后的修改）；不支持时抛出 NotSupportedException。</summary>
        Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
        /// <summary>释放指定保存点（释放后不可再回退到它）；不支持时抛出 NotSupportedException。</summary>
        Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>提交事务，持久化其中的全部变更。</summary>
        Task CommitAsync(CancellationToken cancellationToken = default);
        /// <summary>回滚整个事务，丢弃其中的全部变更。</summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
