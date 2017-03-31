using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Socket.Model {
    public class Client {
        string username;
        Windows.Networking.Sockets.StreamSocket clientsocket;
        Windows.Networking.HostName serverHost;
        string serverPort;
        Stream streamIn;
        StreamReader reader;
        string nickname;
        bool working;
        Client(string u_n, string s_p, string HostIp){
            username = u_n;
            serverPort = s_p;
            working = true;
            clientsocket = new Windows.Networking.Sockets.StreamSocket();
            serverHost = new Windows.Networking.HostName(HostIp);

            streamIn = clientsocket.InputStream.AsStreamForRead();
            reader = new StreamReader(streamIn);
        }

        private async void Listener() {
            while (working) {

            }
        }
    }
}
