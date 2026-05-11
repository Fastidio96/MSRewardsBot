using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MSRewardsBot.Common.DataEntities.Accounting
{
    [Table("account")]
    public class MSAccount : BaseEntity, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

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
        public string? Email
        {
            get => _email;
            set
            {
                if (_email == value)
                {
                    return;
                }
                _email = value;
                NotifyPropertyChanged();
            }
        }
        private string? _email;

        [JsonIgnore]
        public User User { get; set; }
        public List<AccountCookie> Cookies { get; set; }

        
        [NotMapped]
        public bool IsCookiesExpired
        {
            get => _isCookiesExpired;
            set
            {
                if (_isCookiesExpired == value)
                {
                    return;
                }
                _isCookiesExpired = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isCookiesExpired;

        [NotMapped]
        public bool IsAccountBanned
        {
            get => _isAccountBanned;
            set
            {
                if (_isAccountBanned == value)
                {
                    return;
                }
                _isAccountBanned = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isAccountBanned;

        [NotMapped]
        public MSAccountStats Stats { get; set; }

        public void ChangeProperty(MSAccount account, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(Email):
                    Email = account.Email;
                    break;
                case nameof(IsCookiesExpired):
                    IsCookiesExpired = account.IsCookiesExpired;
                    break;
                case nameof(IsAccountBanned):
                    IsAccountBanned = account.IsAccountBanned;
                    break;
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
