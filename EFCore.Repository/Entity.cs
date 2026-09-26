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
        int? _requestedHashCode;
        int _Id;
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
        public bool IsTransient()
        {
            return this.Id == default;
        }

        public bool ResetId()
        {
            if (!IsTransient()) {
                this.Id = default;
            }
            return true;
        }

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
        public static bool operator ==(Entity left, Entity right)
        {
            if (Object.Equals(left, null))
                return (Object.Equals(right, null)) ? true : false;
            else
                return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }
    }
}
