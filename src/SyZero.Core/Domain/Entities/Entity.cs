using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SyZero.Domain.Entities
{
    /// <summary>
    /// 带泛型主键的实体基类。默认主键由调用方负责赋值。
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    public class Entity<TKey> : IEntity<TKey>
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public virtual TKey Id { get; set; } = default!;
    }

    /// <summary>
    /// 实体继承此类（默认 long 主键，使用 SYID 雪花 ID 自动生成）。
    /// </summary>
    public class Entity : Entity<long>, IEntity
    {
        private long _Id = SYID.NextId();
        /// <summary>
        /// 主键
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public override long Id
        {
            get { return this._Id; }
            set
            {
                if (value == 0)
                    this._Id = SYID.NextId();
                else
                    this._Id = value;
            }
        }
    }
}
