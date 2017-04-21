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
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
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
                // temp = new Model.Client("20000", "localhost");
                temp = new Model.Client("9999", "192.168.43.104");
                temp.Listener();
                temp.GotMessage += (from, msg) => {
                    Rec.message += from+":"+msg+"\n";
                };
                temp.GotError += (msg) => {
                    Rec.message += msg+"\n";
                };
                temp.GotImage +=  async (byteArray) => {
                    /*
                    pictureBox1.Image = Image.FromStream(new MemoryStream(pageData));
                    Bitmap bmp = new Bitmap(new MemoryStream(pageData));
                    string path = Application.StartupPath;
                    string fullPath = path + "\\images\\" + Guid.NewGuid().ToString() + ".png";
                    richTextBox1.Text = fullPath;
                    bmp.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
                    -------------------------------------------------------------------------------
                    MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length);
                    ms.Write(bytes, 0, bytes.Length);
                    ShowImage = Image.FromStream(ms);
                    */
                    var image = new BitmapImage();
                    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream()) {
                        await stream.WriteAsync(byteArray.AsBuffer());
                        stream.Seek(0);
                        await image.SetSourceAsync(stream);
                    }
                    ShowImage.Source = image;

                };
            } catch (Exception ee) {
                Rec.message += ee.Message.ToString() + "\n";
            }
        }

        private void button5_Click(object sender, RoutedEventArgs e) {
            string message = _input.Text;
            _input.Text = "";
            if (message != "" && (clientsocket != null || temp.clientsocket != null)) {
                temp.Create_Chat_json(message, 1);
                //await socket.ConnectAsync(serverHost, serverPort);
            }
        }
        bool imagechange;
        string path;
        private async void button6_Click(object sender, RoutedEventArgs e) {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jepg");
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null) {
                using (var fileStream = await file.OpenSequentialReadAsync()) {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.DecodePixelWidth = 100;
                    bitmapImage.DecodePixelWidth = 100;
                    // await bitmapImage.SetSourceAsync(fileStream);
                    ShowImage.Source = bitmapImage;
                    
                    var readStream = fileStream.AsStreamForRead();
                    var byteArray = new byte[readStream.Length];
                    await readStream.ReadAsync(byteArray, 0, byteArray.Length);
                    temp.Create_Image_json(readStream.Length, 1, byteArray);
                }
                /*imagechange = true;
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                var image = new BitmapImage();
                image.SetSource(stream);
                ShowImage.Source = image;
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync();
                path = new Guid().ToString() + ".jpg";
                Windows.Storage.StorageFolder storageFolder =
                    Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile sampleFile =
                    await storageFolder.CreateFileAsync(path,
                        Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                path = sampleFile.Name;
                SaveSoftwareBitmapToFile(bitmap, sampleFile);
                */
            }
        }

        private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile) {
            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite)) {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.IsThumbnailGenerated = true;
                try {
                    await encoder.FlushAsync();
                } catch (Exception err) {
                    switch (err.HResult) {
                        case unchecked((int)0x88982F81):
                        encoder.IsThumbnailGenerated = false;
                        break;
                        default:
                        throw err;
                    }
                }
                if (encoder.IsThumbnailGenerated == false) {
                    await encoder.FlushAsync();
                }
            }
            
        }
    }
}



/*
 *
byte to string
string result = System.Text.Encoding.UTF8.GetString(byteArray);

stirng to byte
byte[] byteArray = System.Text.Encoding.Default.GetBytes(  str  );

*/
