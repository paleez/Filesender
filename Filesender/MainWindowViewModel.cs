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
        public ICommand StartMyServerCommand { get; }
        public ICommand SendFileCommand { get; }

        public int MyPort { get { return myPort; } set { myPort = value; OnPropertyChanged(nameof(MyPort)); }  }
        private int myPort = 6096;
        public string TheirIp { get { return theirIp; } set { theirIp = value; OnPropertyChanged(nameof(TheirIp)); } }
        private string theirIp = "212.116.64.211";
        public int TheirPort { get { return theirPort; } set { theirPort = value; OnPropertyChanged(nameof(TheirPort)); } }
        private int theirPort = 6096;
        private string myFolder = "C:\\";
        private string fileToSendPath; //mebe

        public bool IsConnected { get { return isConnected; } set { isConnected = value; OnPropertyChanged(nameof(IsConnected)); } }
        private bool isConnected = false;
        Thread listeningThread, connectingThread, sendThread;

        TcpClient tcpClient;
        public Server ActiveServer { get { return server; } set { server = value; OnPropertyChanged(nameof(ActiveServer)); } }
        Server server;
        NetworkStream clientNetworkStream;

        List<Server> servers;
        TcpListener listenerServer;
        Socket socketServer;
        public bool ListenToConnections { get { return listenToConnections; } set { listenToConnections = value; OnPropertyChanged(nameof(ListenToConnections)); } }
        bool listenToConnections = true;

        public MainWindowViewModel()
        {
            ChooseFolderCommand = new Command(ChooseFolder);
            SendFileCommand = new Command(SendFile);
            servers = new List<Server>();
            ThreadPool.QueueUserWorkItem(ServerSetup);
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

        private void ServerSetup(object obj)
        {
            listenerServer = new TcpListener(IPAddress.Any, MyPort);
            listenerServer.Start();

            while (true)
            {
                socketServer = listenerServer.AcceptSocket();
                if (socketServer != null)
                {
                    Server ts = new Server(socketServer, listenerServer, myFolder);
                    //servers.Add(ts);
                    //Console.WriteLine(servers.Count);
                    //ThreadPool.QueueUserWorkItem(servers[servers.Count - 1].ReceiveFile);
                }
            }
        }
       
        private void SendFile()
        {
            ThreadPool.QueueUserWorkItem(SendFileThreadPool);
        }
        private void SendFileThreadPool(object o)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };
            var res = ofd.ShowDialog();

            if ((bool)res)
            {
                TcpClient tempClient = new TcpClient();
                tcpClient = tempClient;
                tcpClient.Connect(IPAddress.Parse(TheirIp), TheirPort);
                tcpClient.Close();
                Console.WriteLine("This is their ip " + IPAddress.Parse(TheirIp));
                Console.WriteLine("this is port " + TheirPort);

                fileToSendPath = ofd.FileName;
                int bufferSize = 1024;
                byte[] data = File.ReadAllBytes(fileToSendPath);
                TcpClient clientForFileTransfer = new TcpClient();
                tcpClient = clientForFileTransfer;
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
                tcpClient.Close();
            }
           
        }
    }
}
