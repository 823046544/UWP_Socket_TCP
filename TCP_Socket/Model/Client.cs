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


        public delegate void GotRoomHandler(int rid, string name, string direction, int activeness, string created_at);
        public event GotRoomHandler GotRoom;


       // public delegate void GotDriverHandler(int rid, int did, string nickname, string badge, ava);
//        public event GotRoomHandler GotDrier;

        public Client(string _port, string HostIp){
            serverPort = _port;
            working = true;
            clientsocket = new Windows.Networking.Sockets.StreamSocket();
            serverHost = new Windows.Networking.HostName(HostIp);
        }

        int min(int x, int y) {return x<y?x:y; }

        public async void Listener() {
            try {
                await clientsocket.ConnectAsync(serverHost, serverPort);
                GotMessage("none", "connect succeed");                                                  ///for console connect status
                streamIn = clientsocket.InputStream.AsStreamForRead();
                reader = new StreamReader(streamIn);
                while (working) {
                    
                    int count, st = 0;
                    byte[] c = new byte[1024*10240+300];
                    byte[] first = new byte[1024];
                    bool flag = false;
                    count = await streamIn.ReadAsync(first, 0, first.Length);
                    for (int i = 0; i < 1024; i++) c[st * 1024 + i] = first[i];
                    st++;
                    string str_msg = System.Text.Encoding.UTF8.GetString(first);
                    if (str_msg[0] != '{' || str_msg == null) continue;
                    string response = "";
                    while (!flag) {
                        response = "";
                        for (int i = 0; i < str_msg.Length; i++) {
                            response += str_msg[i];
                            if (str_msg[i] == '\n') {
                                flag = true; break;
                            }
                        }
                        if (flag) break;
                        count = await streamIn.ReadAsync(first, 0, first.Length);
                        for (int i = 0; i < 1024; i++) c[st * 1024 + i] = first[i];
                        st++;
                        str_msg = System.Text.Encoding.UTF8.GetString(c);
                    }
                    if (response == null) continue;
                    JObject list = (JObject)JsonConvert.DeserializeObject(response);
                    if (list["type"].ToString() == "sys") {                                         ///handle system response
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
                            foreach (var item in list["room"]) {
                                GotRoom(Convert.ToInt32(item["rid"].ToString()), item["name"].ToString(), item["direction"].ToString(),
                                        Convert.ToInt32(item["activeness"].ToString()), item["created-at"].ToString());
                            }
                        }
                    } else if (list["type"].ToString() == "chat") {                                     ///handle chat
                        string chat_from = list["from"].ToString();
                        string chat_to = list["to"].ToString();
                        string chat_msg = list["msg"].ToString();
                        GotMessage(chat_from, chat_msg);
                    } else if (list["type"].ToString() == "file") {                                     ///handle file
                        string format = list["format"].ToString();
                        int length = Convert.ToInt32(list["length"].ToString());
                        byte[] json_bytes = System.Text.Encoding.UTF8.GetBytes(response);
                        int index = json_bytes.Length % 1024;
                        int res_first = count-index;
                        int res_len = length-res_first;
                        byte[] Imgbytes = new byte[length];
                        for (int i = 0; i < res_first; i++) Imgbytes[i] = c[i + index];
                        index = res_first;
                        while (res_len > 0) {
                            count = await streamIn.ReadAsync(first, 0, min(first.Length, res_len));
                            res_len -= count;
                            for (int i = 0; i < count; i++) Imgbytes[index++] = first[i];
                        }
                        GotImage(Imgbytes);
                    }
                    
                }
            } catch (Exception ee) {
                GotError(ee.ToString());
            }
        }

        public void Create_Chat_json(string words, int room_num) {                                      ///add from for debug
            msg = "{\"type\": \"chat\", \"msg\": \"" + words + "\", \"to\": \"" + room_num.ToString() + "\"      ,\"from\":\"lzh\"          }" + "\r\n";
            this.Send_Message();
        }

        public void Create_Signin_json(string username, string password) {
            msg = "{\"type\": \"sys\",  \"detail\": \"sign in\", \"driver\": { \"username\": \"" + username + "\", \"password\": \"" + password + "\"} }" + "\r\n";
            this.Send_Message();
        }

        public void Create_Signup_json(string username, string password, string name, string date) {
            msg = "{ \"type\": \"sys\", \"detail\": \"sign up\", \"driver\": { \"username\": \"" + username + "\", \"password\": \"" + password + "\", \"name\": \"" + name + "\", \"created_at\": \"" + date + "\" } }" + "\r\n";
            this.Send_Message();
        }

        public async void Create_Image_json(long len, int room_num, byte[] Imgbytes) {
            msg = "{\"type\":\"file\",\"format\":\"image\",\"length\":" +len.ToString() + ",\"to\":" + room_num.ToString() + "}"+"\r\n";
            if (clientsocket == null) return;
            streamOut = clientsocket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut);

            byte[] json_bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            byte[] combine = new byte[json_bytes.Length+len];
            int json_count = json_bytes.Length;
            for (int i = 0; i < json_count; i++) combine[i] = json_bytes[i];
            for (int i = 0; i < len; i++) combine[i+json_count] = Imgbytes[i];

            await streamOut.WriteAsync(combine, 0, combine.Length);
            await streamOut.FlushAsync();
        }

        Stream streamOut;
        StreamWriter writer;
        async public void Send_Message() {
            if (clientsocket == null) return;
            streamOut = clientsocket.OutputStream.AsStreamForWrite();
            byte[] json_bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            await streamOut.WriteAsync(json_bytes, 0, json_bytes.Length);
            await streamOut.FlushAsync();
        }


        /*
            Create_Chat_json(string words, int room_num);
            Create_Signin_json(string username, string password);
            Create_Signup_json(string username, string password, string name, string date);
        */
    }
}
