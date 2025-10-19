using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSRewardsBot.Common.DataEntities
{
    public abstract class BaseEntity
    {
        [Key]
        [Column("id")]
        public int DbId { get; set; }
    }
}
