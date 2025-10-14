using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("user")]
    public class User : BaseEntity
    {
        [Column("username")]
        public string Username { get; set; }

        [Column("username")]
        public string Password { get; set; }
         
        public List<Account> Accounts { get; set; }

        public List<UserAuthToken> AuthTokens { get; set; }
    }
}
