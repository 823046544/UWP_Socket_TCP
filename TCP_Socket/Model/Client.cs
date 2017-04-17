﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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


        /*public delegate void GotMessage(string did, string message);
        public event GotMessage Got;

        void GotMessage(string a, string b) {

        }*/


        public delegate void GotMessageHandler(string chat_form, string chat_msg);
        public event GotMessageHandler GotMessage;
        public delegate void GotErrorHandler(string msg);
        public event GotErrorHandler GotError;

        public Client(string _port, string HostIp){
            serverPort = _port;
            working = true;
            clientsocket = new Windows.Networking.Sockets.StreamSocket();
            serverHost = new Windows.Networking.HostName(HostIp);
        }

        public async void Listener() {
            try {
                await clientsocket.ConnectAsync(serverHost, serverPort);
                GotMessage("none", "connect succeed");///fake
                streamIn = clientsocket.InputStream.AsStreamForRead();
                reader = new StreamReader(streamIn);
                while (working) {
                    string response = await reader.ReadLineAsync();
                    if (response == null) continue;
                    if (response == "testing") {
                        GotMessage("Server", response);
                        msg = "ok, gun!";
                        this.Send_Message();
                        continue;
                    }
                    JObject list = (JObject)JsonConvert.DeserializeObject(response);
                    if (list["type"].ToString() == "sys") {///handle system response
                        if (list["detail"].ToString() == "sign in") {///handle signin
                            stauts = list["status"].ToString() == "true" ? true : false;
                            if (stauts) {
                                did = list["driver"]["did"].ToString();
                                name = list["driver"]["name"].ToString();
                                badget = list["driver"]["badget"].ToString();
                                created_at = list["driver"]["created_at"].ToString();
                            } else {

                            }
                        } else if (list["detail"].ToString() == "sign up") {///handle signup
                            stauts = list["stauts"].ToString() == "true" ? true : false;
                            msg = list["msg"].ToString();
                            if (!stauts) {
                                GotError(msg);
                            }
                        }
                    } else if (list["type"].ToString() == "chat") {///handle chat
                        string chat_from = list["from"].ToString();
                        string chat_to = list["to"].ToString();
                        string chat_msg = list["msg"].ToString();
                        GotMessage(chat_from, chat_msg);
                    }
                }
            } catch (Exception ee) {
                GotMessage("err",ee.ToString());
            }
        }

        public void Create_Chat_json(string words, int room_num) {///add from for debug
            msg = "{\"type\": \"chat\", \"msg\": \"" + words + "\", \"to\": \"" + room_num.ToString() + "\"      ,\"from\":\"lzh\"          }";
        }

        public void Create_Signin_json(string username, string password) {
            msg = "{\"type\": \"sys\",  \"detail\": \"sign in\", \"driver\": { \"username\": \"" + username + "\", \"password\": \"" + password + "\"} }";
        }

        public void Create_Signup_json(string username, string password, string name, string date) {
            msg = "{ \"type\": \"sys\", \"detail\": \"sign up\", \"driver\": { \"username\": \"" + username + "\", \"password\": \"" + password + "\", \"name\": \"" + name + "\", \"created_at\": \"" + date + "\" } }";
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
    }
}
