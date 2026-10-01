// Copyright (c) 2026 ZhangShuai. All rights reserved.
// 项目：UI.FrameWork —— EFCore.Repository（EF Core 数据访问抽象层）

namespace EFCore.Repository
{

    /// <summary>
    /// 数据库实体基类：约定 int 自增主键 <see cref="Id"/>，并提供基于主键的值相等语义——
    /// 同类型且 Id 相等的两个实体视为相等（Equals / GetHashCode / == / != 保持一致），
    /// 使实体可以按"业务身份"而非对象引用参与集合去重与比对。
    /// <para><see cref="IsTransient"/>：判定实体是否尚未持久化（Id 为默认值 0）；
    /// <see cref="ResetId"/>：把已持久化实体的主键重置为瞬态。</para>
    /// </summary>
    public class Entity
    {
        /// <summary>已计算哈希码的缓存：保证同一实体多次取哈希结果稳定。</summary>
        int? _requestedHashCode;
        /// <summary>主键 backing 字段：默认值 0 表示尚未持久化。</summary>
        int _Id;
        /// <summary>自增主键；为默认值 0 时表示实体尚未持久化（瞬态）。</summary>
        public virtual int Id
        {
            get
            {
                return _Id;
            }
            protected set
            {
                _Id = value;
            }
        }
        /// <summary>判断实体是否为瞬态（主键为默认值 0，尚未持久化）。</summary>
        public bool IsTransient()
        {
            return this.Id == default;
        }

        /// <summary>把已持久化实体的主键重置为默认值 0（转为瞬态）；恒返回 true。</summary>
        public bool ResetId()
        {
            if (!IsTransient()) {
                this.Id = default;
            }
            return true;
        }

        /// <summary>值相等语义：同类型、双方均已持久化且主键相等才视为相等。</summary>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Entity))
                return false;

            if (Object.ReferenceEquals(this, obj))
                return true;

            if (this.GetType() != obj.GetType())
                return false;

            Entity item = (Entity)obj;

            if (item.IsTransient() || this.IsTransient())
                return false;
            else
                return item.Id == this.Id;
        }

        /// <summary>已持久化实体返回按主键计算并缓存的哈希码；瞬态实体返回基类哈希。</summary>
        public override int GetHashCode()
        {
            if (!IsTransient())
            {
                if (!_requestedHashCode.HasValue)
                    _requestedHashCode = this.Id.GetHashCode() ^ 31; // XOR for random distribution (http://blogs.msdn.com/b/ericlippert/archive/2011/02/28/guidelines-and-rules-for-gethashcode.aspx)

                return _requestedHashCode.Value;
            }
            else
                return base.GetHashCode();

        }
        /// <summary>相等运算符：语义与 Equals 一致，任一侧为 null 时按引用判断。</summary>
        public static bool operator ==(Entity left, Entity right)
        {
            if (Object.Equals(left, null))
                return (Object.Equals(right, null)) ? true : false;
            else
                return left.Equals(right);
        }

        /// <summary>不等运算符：为相等运算符取反。</summary>
        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }
    }
}
