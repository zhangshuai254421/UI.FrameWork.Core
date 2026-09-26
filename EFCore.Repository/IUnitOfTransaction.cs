// Copyright (c) 2026 ZhangShuai. All rights reserved.
// 项目：UI.FrameWork —— EFCore.Repository（EF Core 数据访问抽象层）

namespace EFCore.Repository
{
    /// <summary>
    /// 数据库事务句柄：封装一次已开启的事务，支持提交（<see cref="CommitAsync"/>）与回滚
    /// （<see cref="RollbackAsync"/>），并支持保存点（savepoint）做局部回滚——
    /// <see cref="CreateSavepointAsync"/> 打标记、<see cref="RollbackToSavepointAsync"/> 回退到标记、
    /// <see cref="ReleaseSavepointAsync"/> 释放标记；<see cref="SupportsSavepoints"/> 指示提供程序是否支持保存点。
    /// <para>实现 <see cref="IDisposable"/>：Dispose 即结束事务，未提交的修改将被丢弃。</para>
    /// </summary>
    public interface IUnitOfTransaction : IDisposable
    {
        bool SupportsSavepoints { get; }
        Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
        Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
        Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);

        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
