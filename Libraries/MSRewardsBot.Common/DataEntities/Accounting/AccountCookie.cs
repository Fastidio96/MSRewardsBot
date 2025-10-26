using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("account_cookie")]
    public class AccountCookie : BaseEntity
    {
        [JsonIgnore]
        [Column("ms_account_id")]
        public int MSAccountId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("value")]
        public string Value { get; set; }

        [Column("domain")]
        public string Domain { get; set; }

        [Column("path")]
        public string Path { get; set; }

        [Column("expires")]
        public DateTime? Expires { get; set; }

        [Column("http_only")]
        public bool HttpOnly { get; set; }

        [Column("secure")]
        public bool Secure { get; set; }

        [Column("same_site")]
        public string SameSite { get; set; }

        [JsonIgnore]
        public MSAccount MSAccount { get; set; }
    }
}
