using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Socket.Model {
    public class record : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public string S = "";
        public string message {
            get { return S; }
            set {
                S = value;
                if (this.PropertyChanged != null) {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("message"));
                }
            }
        }
    }
}
