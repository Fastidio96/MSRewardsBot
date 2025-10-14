using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSRewardsBot.Common.DataEntities
{
    public abstract class BaseEntity
    {
        [Column("id")]
        [Key]
        public long DbId { get; set; }
    }
}
