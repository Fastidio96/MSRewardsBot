using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("user")]
    public class User : BaseEntity
    {
        public User()
        {
            MSAccounts = new List<MSAccount>();
        }

        [Column("username")]
        public string Username { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [JsonIgnore]
        [Column("is_admin")]
        public bool IsAdmin { get; set; }

        public UserAuthToken AuthToken { get; set; }
        public List<MSAccount> MSAccounts { get; set; }
    }
}
