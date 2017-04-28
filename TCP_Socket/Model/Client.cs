using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Storage.Streams;

namespace TCP_Socket.Model {
    public class Client {
        string username, name, badget, created_at, did;
        bool stauts;

        public Windows.Networking.Sockets.StreamSocket clientsocket;
        Windows.Networking.HostName serverHost;
        string serverPort;
        Stream streamIn;
        StreamReader reader;
        bool working;
        string msg;

        int cur_rid;
        
        public delegate void GotMessageHandler(string chat_form, string chat_msg);
        public event GotMessageHandler GotMessage;
        public delegate void GotErrorHandler(string msg);
        public event GotErrorHandler GotError;

        public delegate void GotImageHandler(byte[] bytes);
        public event GotImageHandler GotImage;

        public Client(string _port, string HostIp){
            serverPort = _port;
            working = true;
            clientsocket = new Windows.Networking.Sockets.StreamSocket();
            serverHost = new Windows.Networking.HostName(HostIp);
        }


        public async void Listener() {
            try {
                await clientsocket.ConnectAsync(serverHost, serverPort);
                GotMessage("none", "connect succeed");                                                  ///for console connect status
                streamIn = clientsocket.InputStream.AsStreamForRead();
                reader = new StreamReader(streamIn);
                while (working) {
                    
                    int count = 0;
                    byte[] c = new byte[2000000];
                    await streamIn.ReadAsync(c, 0, c.Length);
                    string str_msg = System.Text.Encoding.UTF8.GetString(c);
                    

                    /*int count = 0;
                    byte[] bb = new byte[1];
                    while (true) {
                        await streamIn.ReadAsync(bb, 0, bb.Length);
                        count++;
                        GotError(bb[0] + "  ");
                        if (count > 500) break;
                    }
                    break;*/
                    string response = "";
                    for (int i = 0; ;i++) {
                        response += str_msg[i];
                        if (str_msg[i] == '\n' && str_msg[i-1] == '\r') break;
                    }
                    if (response == null) continue;
                    JObject list = (JObject)JsonConvert.DeserializeObject(response);
                    if (list["type"].ToString() == "sys") {                                             ///handle system response
                        if (list["detail"].ToString() == "sign in") {                                   ///handle signin
                            stauts = list["status"].ToString() == "true" ? true : false;
                            if (stauts) {
                                did = list["driver"]["did"].ToString();
                                name = list["driver"]["name"].ToString();
                                badget = list["driver"]["badget"].ToString();
                                created_at = list["driver"]["created_at"].ToString();
                            } else {                                                                    ///handle singin error
                                GotError(msg);
                            }
                        } else if (list["detail"].ToString() == "sign up") {                            ///handle signup
                            stauts = list["stauts"].ToString() == "true" ? true : false;
                            msg = list["msg"].ToString();
                            if (!stauts) {                                                              ///handle signup error
                                GotError(msg);
                            }
                        } else if (list["detail"].ToString() == "room list") {

                        }
                    } else if (list["type"].ToString() == "chat") {                                     ///handle chat
                        string chat_from = list["from"].ToString();
                        string chat_to = list["to"].ToString();
                        string chat_msg = list["msg"].ToString();
                        GotMessage(chat_from, chat_msg);
                    } else if (list["type"].ToString() == "file") {                                     ///handle file
                        string format = list["format"].ToString();
                        int length = Convert.ToInt32(list["length"].ToString());
                        //-------------------------------------------------------------------
                        byte[] json_bytes = System.Text.Encoding.UTF8.GetBytes(response);
                        byte[] bytesArray = new byte[length];
                        for (int i = 0; i < length; i++) bytesArray[i] = c[i+json_bytes.Length];
                        GotImage(bytesArray);
                    }
                    
                }
            } catch (Exception ee) {
                GotError(ee.ToString());
            }
        }

        public void Create_Chat_json(string words, int room_num) {                                      ///add from for debug
            msg = "{\"type\": \"chat\", \"msg\": \"" + words + "\", \"to\": \"" + room_num.ToString() + "\"      ,\"from\":\"lzh\"          }";
            this.Send_Message();
        }

        public void Create_Signin_json(string username, string password) {
            msg = "{\"type\": \"sys\",  \"detail\": \"sign in\", \"driver\": { \"username\": \"" + username + "\", \"password\": \"" + password + "\"} }";
            this.Send_Message();
        }

        public void Create_Signup_json(string username, string password, string name, string date) {
            msg = "{ \"type\": \"sys\", \"detail\": \"sign up\", \"driver\": { \"username\": \"" + username + "\", \"password\": \"" + password + "\", \"name\": \"" + name + "\", \"created_at\": \"" + date + "\" } }";
            this.Send_Message();
        }

        public async void Create_Image_json(long len, int room_num, byte[] bytesArray) {
            msg = "{\"type\":\"file\",\"format\":\"image\",\"length\":" +len.ToString() + ",\"to\":" + room_num.ToString() + "}";
            GotMessage("none", msg);
            if (clientsocket == null) return;
            streamOut = clientsocket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut);
            await writer.WriteLineAsync(msg);
            await writer.FlushAsync();

            streamOut = clientsocket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut);
            streamOut.Write(bytesArray, 0, (int)len);

            //await writer.FlushAsync();
            //GotImage(bytesArray);
        }

        Stream streamOut;
        StreamWriter writer;
        async public void Send_Message() {
            if (clientsocket == null) return;
            streamOut = clientsocket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut);
            await writer.WriteLineAsync(msg);
            await writer.FlushAsync();
        }


        /*
            Create_Chat_json(string words, int room_num);
            Create_Signin_json(string username, string password);
            Create_Signup_json(string username, string password, string name, string date);
        */
    }
}
