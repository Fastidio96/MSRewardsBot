using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("account")]
    public class MSAccount : BaseEntity
    {
        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [Column("email")]
        public string Email { get; set; }

        public List<AccountCookie> Cookies { get; set; }
    }
}
