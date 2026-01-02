using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MSRewardsBot.Common.DataEntities.Stats;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("account")]
    public class MSAccount : BaseEntity
    {
        public MSAccount()
        {
            Cookies = new List<AccountCookie>();
            Stats = new MSAccountStats();
            IsCookiesExpired = false;
            IsAccountBanned = false;
        }

        [JsonIgnore]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [JsonIgnore]
        public User User { get; set; }
        public List<AccountCookie> Cookies { get; set; }

        [NotMapped]
        public bool IsCookiesExpired { get; set; }

        [NotMapped]
        public bool IsAccountBanned { get; set; }

        [NotMapped]
        public MSAccountStats Stats { get; set; }
    }
}
