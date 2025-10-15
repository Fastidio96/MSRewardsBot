using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSRewardsBot.Client.DataEntities
{
    public class AppInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool ConnectedToServer
        {
            get => _connectedToServer;
            set
            {
                if (_connectedToServer != value)
                {
                    _connectedToServer = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _connectedToServer;

        public bool IsUserLogged
        {
            get => _isUserLogged;
            set
            {
                if (_isUserLogged != value)
                {
                    _isUserLogged = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _isUserLogged;

        


        private void NotifyPropertyChanged([CallerMemberName]string propName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
