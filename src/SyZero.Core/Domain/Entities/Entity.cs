using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SyZero.Domain.Entities
{

    /// <summary>
    /// 实体继承此类  常用主键类型long
    /// </summary>
    public class Entity : IEntity
    {
        private long _Id = SYID.NextId();
        /// <summary>
        /// 根
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id
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
