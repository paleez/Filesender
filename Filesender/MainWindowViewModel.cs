using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Filesender
{
    class MainWindowViewModel : ViewModelBase
    {
        public ICommand ChooseFolderCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand SetMyPortCommand { get; }
        public ICommand StartMyServerCommand { get; }
        public ICommand SendFileCommand { get; }

        //public int MyPort { get { return myPort; } set { myPort = value; OnPropertyChanged(nameof(MyPort)); }  }
        //private int myPort = 6096;
        public string TheirIp { get { return theirIp; } set { theirIp = value; OnPropertyChanged(nameof(TheirIp)); } }
        private string theirIp = "212.116.64.211";
        public int TheirPort { get { return theirPort; } set { theirPort = value; OnPropertyChanged(nameof(TheirPort)); } }
        private int theirPort = 6096;
        private string myFolder = "C:\\";
        private string fileToSendPath; //mebe

        public bool IsConnected { get { return isConnected; } set { isConnected = value; OnPropertyChanged(nameof(IsConnected)); } }
        private bool isConnected = false;
        Thread listeningThread, connectingThread, sendThread;
        TcpListener tcpListener, tcpListener2;
        TcpClient tcpClient;
        //string filePath, string hostName, int port

        TcpClient clientTCP;
        NetworkStream clientNetworkStream, clientNetworkStream2;
        BinaryReader readerClient;
        BinaryWriter writerClient;

        
        Socket listenSocket;
        public Server ActiveServer { get { return server; } set { server = value; OnPropertyChanged(nameof(ActiveServer)); } }
        Server server;

        public MainWindowViewModel()
        {
            ChooseFolderCommand = new Command(ChooseFolder);
            ConnectCommand = new Command(Connect);
            SendFileCommand = new Command(SendFile);
            StartMyServerCommand = new Command(StartServerThread);
           

            //listeningThread = new Thread(StartServerThread);
            //connectingThread = new Thread(ConnectThread);
            sendThread = new Thread(SendFileThread);

            //server = new Server();
        }

        private void ChooseFolder()
        {
            var dialog = new CommonOpenFileDialog()
            {
                Title = "Select Folder",
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                myFolder = dialog.FileName;
            }
        }

        private void Connect()
        {
            //connectingThread.Start();
            ConnectThread();
        }
        private void ConnectThread()
        {
            IPHostEntry ipEntry = Dns.GetHostEntry(TheirIp);
            IPAddress ip = ipEntry.AddressList[0];
            TcpClient tc = new TcpClient();
            tcpClient = tc;
            Console.WriteLine("This is their ip " + IPAddress.Parse(TheirIp));
            tcpClient.Connect(IPAddress.Parse(TheirIp), TheirPort);


            //Socket clientSocket = new Socket(AddressFamily.InterNetwork,
            //SocketType.Stream, ProtocolType.Tcp);
            //clientSocket.Connect(new IPEndPoint(IPAddress.Parse(TheirIp), theirPort));

        }
        private void StartServer()
        {
            listeningThread.Start();
            //StartServerThread();
        }
        private void StartServerThread()
        {
            //listenSocket = tcp.AcceptSocket();
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ip = ipEntry.AddressList[1];
            //TcpListener tc = new TcpListener(ip, myPort);
            //tcpListener = tc;
            //tcpListener.Start();
            Console.WriteLine("this is ip " + ip);
            //Console.WriteLine("this is port " + myPort);
            //listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
        }
        private void SendFile()
        {
            sendThread.Start();
            //SendFileThread();
        }
        private void SendFileThread()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };

            var res = ofd.ShowDialog();
            if ((bool)res)
            {
                TcpClient tc = new TcpClient();
                tcpClient = tc;
                Console.WriteLine("This is their ip " + IPAddress.Parse(TheirIp));
                tcpClient.Connect(IPAddress.Parse(TheirIp), TheirPort);

                tcpClient.Close();
                //server is started, so tcpClient should be available

                Console.WriteLine("this is ip " + TheirIp);
                Console.WriteLine("this is port " + TheirPort);
                
                fileToSendPath = ofd.FileName;
                int bufferSize = 1024;
                byte[] data = File.ReadAllBytes(fileToSendPath);

                TcpClient t = new TcpClient();
                tcpClient = t;
                tcpClient.Connect(IPAddress.Parse(TheirIp), TheirPort);
                clientNetworkStream = tcpClient.GetStream();
               
                byte[] dataLength = BitConverter.GetBytes(data.Length);
                clientNetworkStream.Write(dataLength, 0, 4);
                int bytesSent = 0;
                int bytesLeft = data.Length;

                

                while (bytesLeft > 0)
                {
                    int currentDataSize = Math.Min(bufferSize, bytesLeft);
                    clientNetworkStream.Write(data, bytesSent, currentDataSize);
                    bytesSent += currentDataSize;
                    bytesLeft -= currentDataSize;
                    
                }


                //listenSocket = tcpListener.AcceptSocket();
                //clientNetworkStream2 = clientNetworkStream;

                //int bs = 1024;
                //byte[] d = File.ReadAllBytes(fileToSendPath);
                //clientNetworkStream2.ReadTimeout = 300;
                //byte[] dl = new byte[4];
                //Thread.Sleep(500);
                //clientNetworkStream2.Read(dl, 0, 4);

                //int bytesS = 0;
                //int bytesL = d.Length;

                //Thread.Sleep(600);

                //while (bytesL > 0)
                //{
                //    int cds = Math.Min(bs, bytesL);
                //    clientNetworkStream2.Read(d, bytesS, cds);
                //    bytesS += cds;
                //    bytesL -= cds;

                //}

                Console.WriteLine("this is my folder " + myFolder);
                //File.SetAttributes(myFolder, FileAttributes.Normal);
                //File.WriteAllBytes(@"myFolder", data);  //access denied
                //isConnected = true;
                //ThreadPool.QueueUserWorkItem(ReceiveFile);
                
                //Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //client.Connect(IPAddress.Parse(TheirIp), TheirPort);
                Console.WriteLine("this is filename " + ofd.FileName);
              
                Console.WriteLine("exiting SendThread");
            }
           
        }

        public void ReceiveFile(object obj)
        {
            Console.WriteLine(isConnected);
            if (isConnected)
            {
                tcpClient = tcpListener.AcceptTcpClient();
                //listenSocket = tcpListener.AcceptSocket();
                NetworkStream stream = tcpClient.GetStream();
                byte[] fileSizeBytes = new byte[4];
                int bytes = stream.Read(fileSizeBytes, 0, 4);
                int dataLen = BitConverter.ToInt32(fileSizeBytes, 0);

                int bytesLeft = dataLen;
                byte[] data = new byte[dataLen];

                int bufferSize = 1024;
                int bytesRead = 0;
                while (bytesLeft > 0)
                {
                    int currentDataSize = Math.Min(bufferSize, bytesLeft);
                    if (tcpClient.Available < currentDataSize)
                    {
                        currentDataSize = tcpClient.Available;
                    }

                    bytes = clientNetworkStream.Read(data, bytesRead, currentDataSize);
                    bytesRead += currentDataSize;
                    bytesLeft -= currentDataSize;
                }
                File.WriteAllBytes(@"myFolder", data);
            }
        }
    }
}
