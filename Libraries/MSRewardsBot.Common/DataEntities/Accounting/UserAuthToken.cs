using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("user_token")]
    public class UserAuthToken : BaseEntity
    {
        [JsonIgnore]
        [Column("user")]
        public int UserId { get; set; }

        [JsonIgnore]
        public User User { get; set; }

        [Column("token")]
        public Guid Token { get; set; }

        [JsonIgnore]
        [Column("created")]
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]
        [Column("last_time_used")]
        public DateTime LastTimeUsed { get; set; }
    }
}
