using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("account")]
    public class MSAccount : BaseEntity
    {
        public MSAccount()
        {
            LastDashboardUpdate = DateTime.MinValue;
        }

        [JsonIgnore]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("last_dashboard_update")]
        public DateTime LastDashboardUpdate { get; set; }

        [JsonIgnore]
        public User User { get; set; }
        public List<AccountCookie> Cookies { get; set; }
    }
}
