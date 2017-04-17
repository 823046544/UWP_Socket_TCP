using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using TCP_Socket.Model;
using static TCP_Socket.Model.Client;
//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace TCP_Socket {
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page {
        Model.record Rec = null;
        SynchronizationContext m_SyncContext = null;
        String user_name;
        bool working;
        List<Windows.Networking.Sockets.StreamSocket> S;
        public MainPage() {
            this.InitializeComponent();
            Rec = new Model.record();
            Rec.message = "";
            user_name = "lzh";
            m_SyncContext = SynchronizationContext.Current;
            if (S != null) S.Clear();
        }

        
        private async void SocketListener_ConnectionReceived(Windows.Networking.Sockets.StreamSocketListener sender, Windows.Networking.Sockets.StreamSocketListenerConnectionReceivedEventArgs args) {
            /*Windows.Networking.Sockets.StreamSocket temp_socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName temp_serverHost = new Windows.Networking.HostName("localhost");
            string temp_serverPort = "8888";            
            await temp_socket.ConnectAsync(temp_serverHost, temp_serverPort);*/
            bool flag = true;
            Windows.Networking.Sockets.StreamSocket temp_socket = args.Socket;
            foreach (Windows.Networking.Sockets.StreamSocket item in S) if (item == temp_socket) flag = false;
            if (flag) S.Add(temp_socket);
            Stream temp_streamIn = temp_socket.InputStream.AsStreamForRead();
            StreamReader temp_reader = new StreamReader(temp_streamIn);
            while (true) {
                string message = await temp_reader.ReadLineAsync();
                foreach (Windows.Networking.Sockets.StreamSocket item in S) {
                    Stream outStream = item.OutputStream.AsStreamForWrite();
                    StreamWriter writer = new StreamWriter(outStream);
                    await writer.WriteLineAsync(message);
                    await writer.FlushAsync();
                }
            }
        }

        private void SetTextSafePost(object text) {
            Rec.message += text.ToString();
        }
        Windows.Networking.Sockets.StreamSocketListener socketListener;
        private async void Button1_Click(object sender, RoutedEventArgs e) {
            try {
                S = new List<Windows.Networking.Sockets.StreamSocket>();
                //Create a StreamSocketListener to start listening for TCP connections.
                socketListener = new Windows.Networking.Sockets.StreamSocketListener();

                //Hook up an event handler to call when connections are received.
                socketListener.ConnectionReceived += SocketListener_ConnectionReceived;

                //Start listening for incoming TCP connections on the specified port. You can specify any port that' s not currently in use.
                await socketListener.BindServiceNameAsync("20000");
                Rec.message += "Create server success\n";
            } catch (Exception ee) {
                Rec.message += ee.Message.ToString() + "\n";
            }
        }


        Windows.Networking.Sockets.StreamSocket clientsocket;
        Windows.Networking.HostName serverHost;
        string serverPort;
        Stream streamIn;
        StreamReader reader;

        private async void Button2_Click(object sender, RoutedEventArgs e) {
            try {
                //Create the StreamSocket and establish a connection to the echo server.
                clientsocket = new Windows.Networking.Sockets.StreamSocket();

                //The server hostname that we will be establishing a connection to. We will be running the server and client locally,
                //so we will use localhost as the hostname.
                serverHost = new Windows.Networking.HostName("localhost");//172.19.107.163

                //Every protocol typically has a standard port number. For example HTTP is typically 80, FTP is 20 and 21, etc.
                //For the echo server/client application we will use a random port 1337.
                serverPort = "20000";//"20000";
                await clientsocket.ConnectAsync(serverHost, serverPort);
                Rec.message += "Connected\n";

                streamIn = clientsocket.InputStream.AsStreamForRead();
                reader = new StreamReader(streamIn);

                while (true) {
                    string response = await reader.ReadLineAsync();
                    Rec.message += response + "\n";
                }
            } catch (Exception ee) {
                Rec.message += ee.Message.ToString() + "\n";
                //Handle exception here.            
            }

        }

        private async void button3_Click(object sender, RoutedEventArgs e) {
            string message = _input.Text;
            _input.Text = "";
            if (message != "" && clientsocket != null) {
                message = user_name + " : " + message;
                Stream streamOut = clientsocket.OutputStream.AsStreamForWrite();
                StreamWriter writer = new StreamWriter(streamOut);
                await writer.WriteLineAsync(message);
                await writer.FlushAsync();
                //await socket.ConnectAsync(serverHost, serverPort);
            }
        }
        

        Client temp;
        ///192.168.43.104   9999
        private async void button4_Click(object sender, RoutedEventArgs e) {
            try {
                temp = new Model.Client("20000", "localhost");
                temp.Listener();
                temp.GotMessage += (from, msg) => {
                    Rec.message += from+":"+msg+"\n";
                };
                temp.GotError += (msg) => {

                };
            } catch (Exception ee) {
                Rec.message += ee.Message.ToString() + "\n";
            }
        }

        private async void button5_Click(object sender, RoutedEventArgs e) {
            string message = _input.Text;
            _input.Text = "";
            if (message != "" && (clientsocket != null || temp.clientsocket != null)) {
                temp.Create_Chat_json(message, 1);
                temp.Send_Message();
                //await socket.ConnectAsync(serverHost, serverPort);
            }
        }
    }
}
