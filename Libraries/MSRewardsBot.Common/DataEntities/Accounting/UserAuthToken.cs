using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("user_token")]
    public class UserAuthToken : BaseEntity
    {
        [Column("token")]
        public Guid Token { get; set; }

        [Column("created")]
        public DateTime Created { get; set; }

        [Column("last_time_used")]
        public DateTime LastTimeUsed { get; set; }

        [Column("is_admin")]
        public bool IsAdmin { get; set; }

        [Column("is_valid")]
        public bool IsValid { get; set; }
    }
}
