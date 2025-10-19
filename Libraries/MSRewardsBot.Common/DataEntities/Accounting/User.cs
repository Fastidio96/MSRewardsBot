using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("user")]
    public class User : BaseEntity
    {
        [Column("username")]
        public string Username { get; set; }

        [Column("password")]
        public string Password { get; set; }
        
        [Column("auth_token")]
        private int? AuthTokenId { get; set; }

        [ForeignKey(nameof(AuthTokenId))]
        public UserAuthToken AuthToken { get; set; }

        [Column("is_admin")]
        public bool IsAdmin { get; set; }

        public List<MSAccount> MSAccounts { get; set; }
    }
}
