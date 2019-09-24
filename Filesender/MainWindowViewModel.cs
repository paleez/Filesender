using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Filesender
{
    class MainWindowViewModel : ViewModelBase
    {
        public ICommand ChooseFolderCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand StartServerCommand { get; }
        public ICommand SendFileCommand { get; }

        public int Local_IP { get { return localIP; } set { localIP = value; OnPropertyChanged(nameof(Local_IP)); } }
        public int LocalPort { get { return localPort; } set { localPort = value; OnPropertyChanged(nameof(LocalPort)); } }
        public string RemoteIP { get { return remoteIP; } set { remoteIP = value; OnPropertyChanged(nameof(RemoteIP)); Console.WriteLine("RemoteIP changed"); } }
        public int RemotePort { get { return remotePort; } set { remotePort = value; OnPropertyChanged(nameof(RemotePort)); Console.WriteLine("RemotePort changed"); } }
        public bool ListenToConnections { get { return listenToConnections; } set { listenToConnections = value; OnPropertyChanged(nameof(ListenToConnections)); } }
        public int Progress { get { return progress; } set { progress = value; OnPropertyChanged(nameof(Progress)); } }
        public int ProgressReceive { get { return progressReceive; } set { progressReceive = value; OnPropertyChanged(nameof(ProgressReceive)); } }
        public string ConnectionFeedback { get { return connectionFeedback; } set { connectionFeedback = value; OnPropertyChanged(nameof(ConnectionFeedback)); Console.WriteLine("connectionFeedback changed to " + connectionFeedback); } }
        public string ReceivedFilesPath { get { return "Receiving files in\n" + myFolder; } set { myFolder = value; OnPropertyChanged(nameof(ReceivedFilesPath)); Console.WriteLine("received pathdir changed to " + myFolder); } }
        private int progress = 0;
        private int progressReceive = 0;
        private int localIP;
        private int localPort = 6096;
        private int remotePort = 6096;
        private string remoteIP = "127.0.0.1";
        private string connectionFeedback = "Waiting for connection...";
        private string myFolder;
        private string fileToSendPath;
        private bool pathSet = false;
        private bool listenToConnections = true;

        TcpListener listenerServer;
        Socket socketServer;
        NetworkStream networkStream;
        TcpClient tcpClient;
        Thread t1;

        public MainWindowViewModel()
        {
            ChooseFolderCommand = new Command(ChooseFolder);
            SendFileCommand = new Command(SendFile);
            t1 = new Thread(ServerSetup);
            t1.Start();
        }

        private void ChooseFolder()
        {
            Console.WriteLine("ChooseFolder for server");
            
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
                ReceivedFilesPath = myFolder;
                pathSet = true;
                Console.WriteLine("Path is set to " + myFolder);
            }
        }

        private void ServerSetup(object obj)
        {
            int counter = 0;
            while (listenToConnections)
            {
                ConnectionFeedback = "Listening for connections on port: " + localPort;
                Console.WriteLine("ServerSetup method has been called " + counter + " times");
                counter++;
                listenerServer = new TcpListener(IPAddress.Any, LocalPort);
                listenerServer.Start();
                socketServer = listenerServer.AcceptSocket();
                listenToConnections = false;
                if (socketServer != null)
                {
                    tcpClient = listenerServer.AcceptTcpClient();
                    networkStream = tcpClient.GetStream();
                    ConnectionFeedback = "Connected";
                    //receive the filename size and filename first
                    byte[] fsb = new byte[4];
                    int b = networkStream.Read(fsb, 0, 4);
                    int s = BitConverter.ToInt32(fsb, 0);
                    byte[] filenameBuf = new byte[s];
                    networkStream.Read(filenameBuf, 0, s);
                    string filename = Encoding.UTF8.GetString(filenameBuf);

                    //receive the filesize
                    byte[] fileSizeBytes = new byte[4];
                    int bytes = networkStream.Read(fileSizeBytes, 0, 4);
                    int dataLen = BitConverter.ToInt32(fileSizeBytes, 0);
                    int bytesLeft = dataLen;
                    byte[] data = new byte[dataLen];
                    int bufferSize = 1024;
                    int bytesRead = 0;

                    //receive file
                    while (bytesLeft > 0)
                    {
                        int currentDataSize = Math.Min(bufferSize, bytesLeft);
                        if (tcpClient.Available < currentDataSize)
                        {
                            currentDataSize = tcpClient.Available;
                        }
                        bytes = networkStream.Read(data, bytesRead, currentDataSize);
                        bytesRead += currentDataSize;
                        bytesLeft -= currentDataSize;
                        double percentage = bytesRead / (double)dataLen;
                        double tmp = percentage * 100;
                        int pr = (int)tmp;
                        Application.Current.Dispatcher.Invoke(() => ProgressReceive = pr );
                    }
                    ConnectionFeedback = "File received";
                    Console.WriteLine("pathset is " + pathSet);

                    if (!pathSet)
                    {
                        String myDocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);                       
                        myFolder = myDocumentPath;
                        ReceivedFilesPath = myFolder;
                        pathSet = true;
                    }

                    Console.WriteLine("receiveFolder set to " + myFolder);
                    File.WriteAllBytes(myFolder + "\\" + filename, data);
                    tcpClient.Close();
                    networkStream.Close();
                    socketServer.Close();
                    listenerServer.Stop();
                    listenToConnections = true;
                    Console.WriteLine("Socket closed, tcpListener closed, listening for new connections");
                }
            }
        }

        private void SendFile()
        {
            Console.WriteLine("Send file to server button");
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
                tempTcp.Connect(IPAddress.Parse(RemoteIP), RemotePort);
                tempTcp.Close();

                TcpClient clientForFileTransfer = new TcpClient();
                NetworkStream clientNetworkStream;
                clientForFileTransfer.Connect(IPAddress.Parse(RemoteIP), RemotePort);
                clientNetworkStream = clientForFileTransfer.GetStream();

                //have to send filename 
                fileToSendPath = ofd.FileName;
                string filename = ofd.SafeFileName;
                int bufferSize = 1024;
                byte[] fname = Encoding.UTF8.GetBytes(filename);
                byte[] fLen = BitConverter.GetBytes(fname.Length);
                clientNetworkStream.Write(fLen, 0, 4);
                clientNetworkStream.Write(fname, 0, fname.Length);

                //sending the file size
                byte[] data = File.ReadAllBytes(fileToSendPath);
                byte[] dataLength = BitConverter.GetBytes(data.Length);
                clientNetworkStream.Write(dataLength, 0, 4);
                int bytesSent = 0;
                int bytesLeft = data.Length;
                int length = data.Length;

                while (bytesLeft > 0) // send the file
                {
                    int currentDataSize = Math.Min(bufferSize, bytesLeft);
                    clientNetworkStream.Write(data, bytesSent, currentDataSize);
                    bytesSent += currentDataSize;
                    bytesLeft -= currentDataSize;
                    double percentage = bytesSent / (double)length; //say filesize is 423 000 and bytesent
                    double tmp = percentage * 100;
                    int con = (int)tmp;
                    Application.Current.Dispatcher.Invoke(() => Progress = con);
                }

                Console.WriteLine("file sent from client");
                clientForFileTransfer.Close();
                clientNetworkStream.Close();
            }
        }
    }
}
