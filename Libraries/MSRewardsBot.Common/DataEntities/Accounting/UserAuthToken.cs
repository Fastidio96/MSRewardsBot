using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("user_token")]
    public class UserAuthToken : BaseEntity
    {
        [Column("user")]
        private int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [Column("token")]
        public Guid Token { get; set; }

        [Column("created")]
        public DateTime CreatedAt { get; set; }

        [Column("last_time_used")]
        public DateTime LastTimeUsed { get; set; }
    }
}
