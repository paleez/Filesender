﻿using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
        public string RemoteIP { get { return remoteIP; } set { remoteIP = value; OnPropertyChanged(nameof(RemoteIP)); /*Console.WriteLine("RemoteIP changed"); */ } }
        public int RemotePort { get { return remotePort; } set { remotePort = value; OnPropertyChanged(nameof(RemotePort)); /*Console.WriteLine("RemotePort changed");*/ } }
        public bool ListenToConnections { get { return listenToConnections; } set { listenToConnections = value; OnPropertyChanged(nameof(ListenToConnections)); } }
        public int Progress { get { return progress; } set { progress = value; OnPropertyChanged(nameof(Progress)); } }
        public int ProgressReceive { get { return progressReceive; } set { progressReceive = value; OnPropertyChanged(nameof(ProgressReceive)); } }
        public string ConnectionFeedback { get { return connectionFeedback; } set { connectionFeedback = value; OnPropertyChanged(nameof(ConnectionFeedback)); /*Console.WriteLine("connectionFeedback changed to " + connectionFeedback); */ } }
        public string ReceivedFilesPath { get { return "Receiving files in\n" + myFolder; } set { myFolder = value; OnPropertyChanged(nameof(ReceivedFilesPath));  } }
        private int progress = 0;
        private int progressReceive = 0;
        private int localIP;
        private int localPort = 6096;
        private int remotePort = 6096;
        private int ccCounter = 0;
        private string remoteIP = "127.0.0.1";
        private string connectionFeedback = "Waiting for connection...";
        private string myFolder;
        private string fileToSendPath;
        private bool pathSet = false;
        private bool listenToConnections = true;
        Thread clientInitThread;

        public string ConnectedClients { get { return connectedClients; } set { connectedClients = value; OnPropertyChanged(nameof(ConnectedClients)); } }
        private string connectedClients = "Connected clients: ";
        public MainWindowViewModel()
        {
            ChooseFolderCommand = new Command(ChooseFolder);
            SendFileCommand = new Command(SendFile);
            ThreadPool.QueueUserWorkItem(ServerSetup);
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
            TcpListener listenerServer = new TcpListener(IPAddress.Any, LocalPort);
            listenerServer.Start();
            while (listenToConnections)
            {
                ConnectionFeedback = "Listening for connections on port: " + localPort;
                Console.WriteLine("ServerSetup method has been called " + counter + " times");
                counter++;

                listenerServer.Start();
                Socket socketServer = listenerServer.AcceptSocket();
                listenToConnections = false;
                if (socketServer != null)
                {
                    ccCounter++;
                    ConnectedClients = "Connected clients: " + ccCounter;
                    TcpClient tcpClient = listenerServer.AcceptTcpClient();
                    NetworkStream networkStream = tcpClient.GetStream();
                    ConnectionFeedback = "Connected";
                    //receive the filename size and filename first
                    byte[] fsb = new byte[4];
                    int b = networkStream.Read(fsb, 0, 4);
                    int s = BitConverter.ToInt32(fsb, 0);
                    byte[] filenameBuf = new byte[s];
                    networkStream.Read(filenameBuf, 0, s);
                    string filename = Encoding.UTF8.GetString(filenameBuf);
                    Console.WriteLine("Filename: " + filename + "Filesize: " + b + "File Buffer Size: " + s);
                    //receive the filesize
                    byte[] fileSizeBytes = new byte[4];
                    int bytes = networkStream.Read(fileSizeBytes, 0, 4);
                    int dataLen = BitConverter.ToInt32(fileSizeBytes, 0);
                    int bytesLeft = dataLen;
                    byte[] data = new byte[dataLen];
                    int bufferSize = 1024;
                    int bytesRead = 0;
                    Console.WriteLine("This is file size received: " + dataLen);
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
                        Application.Current.Dispatcher.Invoke(() => ProgressReceive = pr);
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
                    File.WriteAllBytes(Path.Combine(myFolder, ToSafeFileName(filename)), data);
                    //File.WriteAllBytes(myFolder + "\\" + filename, data);
                    tcpClient.Close();
                    networkStream.Close();
                    socketServer.Close();
                    //listenerServer.Stop();
                    listenToConnections = true;
                    ccCounter--;
                    ConnectedClients = "Connected clients: " + ccCounter;
                    Console.WriteLine("Socket closed, tcpListener closed, listening for new connections");
                }
            }
            listenerServer.Stop();
        }

        public string ToSafeFileName(string s)
        {
            return s
                .Replace("\\", "")
                .Replace("/", "")
                .Replace("\"", "")
                .Replace("*", "")
                .Replace(":", "")
                .Replace("?", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("|", "");
        }
        private void SendFile()
        {
            Console.WriteLine("Send file to server button");
            clientInitThread = new Thread(SendFileThreadAsync);
            clientInitThread.Start();
        }

        private void SendFileThreadAsync()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };
            var res = ofd.ShowDialog();

            if ((bool)res)
            {
                fileToSendPath = ofd.FileName;
                //if the size is larger than 1GB, split it in 4 chunks
                int onegb = 1000000000;
                long fileSize = new FileInfo(fileToSendPath).Length;
                Console.WriteLine("File size: " + fileSize);

                //this is the point to decide
                if (fileSize > onegb)
                {
                    string inputFile = fileToSendPath;
                    FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                    FileStream outputFile;
                    MemoryStream ms = new MemoryStream();
                    int numberOfFiles = 10;
                    int sizeOfEachFile = (int)Math.Ceiling((double)fs.Length / numberOfFiles);

                    //split files into chunks of 10
                    for (int i = 0; i < 10; i++)
                    {
                        string baseFileName = Path.GetFileNameWithoutExtension(inputFile);
                        string extension = Path.GetExtension(inputFile);
                        outputFile = new FileStream(Path.GetDirectoryName(inputFile) + "\\" + baseFileName + "." + i.ToString().PadLeft(5, Convert.ToChar("0")) + extension + ".tmp", FileMode.Create, FileAccess.Write);
                        int bytesRead = 0;
                        byte[] buffer = new byte[sizeOfEachFile]; //create a buffer to write data into til its full then close and repeat
                        Console.WriteLine("This is i: " + i);
                        if ((bytesRead = fs.Read(buffer, 0, sizeOfEachFile)) > 0) outputFile.Write(buffer, 0, bytesRead);
                        outputFile.Close();
                    }
                    fs.Close();

                    //send files
                    string outPath = "C:\\Users\\paleez\\Desktop";
                    string[] tempFiles = Directory.GetFiles(outPath, "*.tmp");
                    for (int i = 0; i < tempFiles.Length; i++)
                    {
                        Console.WriteLine("tempfileAddres" + tempFiles[i]);
                    }

                    for (int i = 0; i < tempFiles.Length; i++)
                    {
                        SendFileMeth(tempFiles[i]);
                    }
                    //string fileName = Path.GetFileNameWithoutExtension(tempFiles[i]);
                    //string baseFileName = fileName.Substring(0, fileName.IndexOf(Convert.ToChar(".")));
                    //string extension = Path.GetExtension(fileName);

                    Console.WriteLine("file sent from client");
                }
                else
                {
                    //TcpClient tempTcp = new TcpClient();
                    //tempTcp.Connect(IPAddress.Parse(RemoteIP), RemotePort);
                    //tempTcp.Close();

                    TcpClient clientForFileTransfer = new TcpClient();
                    NetworkStream clientNetworkStream;
                    clientForFileTransfer.Connect(IPAddress.Parse(RemoteIP), RemotePort);
                    clientNetworkStream = clientForFileTransfer.GetStream();

                    //send filename
                    int bufferSize = 1024;
                    byte[] fname = Encoding.UTF8.GetBytes(ofd.SafeFileName);
                    byte[] fLen = BitConverter.GetBytes(fname.Length);
                    clientNetworkStream.Write(fLen, 0, 4);
                    clientNetworkStream.Write(fname, 0, fname.Length);

                    //send the filesize
                    byte[] data = File.ReadAllBytes(fileToSendPath);
                    byte[] dataLength = BitConverter.GetBytes(data.Length);
                    clientNetworkStream.Write(dataLength, 0, 4);
                    int bytesSent = 0;
                    int bytesLeft = data.Length;
                    int length = data.Length;

                    //send files
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

        private void SendFileMeth(string filePath)
        {
            TcpClient tempTcp = new TcpClient();
            tempTcp.Connect(IPAddress.Parse(RemoteIP), RemotePort);
            tempTcp.Close();

            TcpClient clientForFileTransfer = new TcpClient();
            NetworkStream clientNetworkStream;
            clientForFileTransfer.Connect(IPAddress.Parse(RemoteIP), RemotePort);
            clientNetworkStream = clientForFileTransfer.GetStream();

            //send filename
            int bufferSize = 1024;
            byte[] fname = Encoding.UTF8.GetBytes(filePath);
            byte[] fLen = BitConverter.GetBytes(fname.Length);
            clientNetworkStream.Write(fLen, 0, 4);
            clientNetworkStream.Write(fname, 0, fname.Length);

            //send the filesize
            byte[] data = File.ReadAllBytes(filePath);
            byte[] dataLength = BitConverter.GetBytes(data.Length);
            clientNetworkStream.Write(dataLength, 0, 4);
            int bytesSent = 0;
            int bytesLeft = data.Length;
            int length = data.Length;
            Console.WriteLine("datalength: " + data.Length);

            //send files
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
