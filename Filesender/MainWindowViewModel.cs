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

        public int LocalPort { get { return localPort; } set { localPort = value; OnPropertyChanged(nameof(LocalPort)); } }
        private int localPort = 6096;
        public string TheirIp { get { return theirIp; } set { theirIp = value; OnPropertyChanged(nameof(TheirIp)); } }
        private string theirIp = "212.116.64.211";
        public int TheirPort { get { return theirPort; } set { theirPort = value; OnPropertyChanged(nameof(TheirPort)); } }
        private int theirPort = 6096;
        private string myFolder = "C:\\Users\\darks\\Desktop\\Test\\";
        private string fileToSendPath; //mebe

        public Server ActiveServer { get { return server; } set { server = value; OnPropertyChanged(nameof(ActiveServer)); } }
        Server server;

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
            while (true)
            {
                listenerServer = new TcpListener(IPAddress.Any, LocalPort);
                listenerServer.Start();
                socketServer = listenerServer.AcceptSocket();
                if (socketServer != null)
                {
                    servers.Add(new Server(socketServer, listenerServer, myFolder));
                    server = servers.ElementAt(0);
                    socketServer.Close();
                    listenerServer.Stop();
                    servers.RemoveAt(0);
                }
            }
        }

        private void SendFile()
        {
            ThreadPool.QueueUserWorkItem(SendFileThread);
        }
        private void SendFileThread(object obj)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };
            var res = ofd.ShowDialog();

            if ((bool)res)
            {
                TcpClient tempTcp = new TcpClient();
                tempTcp.Connect(IPAddress.Parse(TheirIp), TheirPort);
                tempTcp.Close();

                TcpClient clientForFileTransfer = new TcpClient();
                NetworkStream clientNetworkStream;
                clientForFileTransfer.Connect(IPAddress.Parse(TheirIp), TheirPort);
                clientNetworkStream = clientForFileTransfer.GetStream();

                //have to send filename 
                fileToSendPath = ofd.FileName;
                string filename = ofd.SafeFileName;
                int bufferSize = 1024;
                byte[] fname = File.ReadAllBytes(filename);
                byte[] fLen = BitConverter.GetBytes(fname.Length);
                int bs = 0;
                int bl = fname.Length;
                while (bl > 0) // send the file
                {
                    int cds = Math.Min(bufferSize, bl);
                    clientNetworkStream.Write(fname, bs, cds);
                    bs += cds;
                    bl -= cds;
                }

                //sending the file size
                byte[] data = File.ReadAllBytes(fileToSendPath);
                byte[] dataLength = BitConverter.GetBytes(data.Length);
                clientNetworkStream.Write(dataLength, 0, 4);
                int bytesSent = 0;
                int bytesLeft = data.Length;
                while (bytesLeft > 0) // send the file
                {
                    int currentDataSize = Math.Min(bufferSize, bytesLeft);
                    //clientNetworkStream.Write(data, bytesSent, currentDataSize);
                    bytesSent += currentDataSize;
                    bytesLeft -= currentDataSize;
                }
                //clientForFileTransfer.Close();
                //clientNetworkStream.Close();
            }
        }
    }
}
