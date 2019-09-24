﻿using Microsoft.Win32;
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

        

        public int MyIP { get { return myip; } set { myip = value; OnPropertyChanged(nameof(MyIP)); } }
        private int myip;
        //run method - lookup ip on host

        public int LocalPort { get { return localPort; } set { localPort = value; OnPropertyChanged(nameof(LocalPort)); } }
        private int localPort = 6096;
        public string RemoteIP { get { return remoteIP; } set { remoteIP = value; OnPropertyChanged(nameof(RemoteIP)); } }
        private string remoteIP = "127.0.0.1";
        
        public int RemotePort { get { return remotePort; } set { remotePort = value; OnPropertyChanged(nameof(RemotePort)); } }
        private int remotePort = 6096;

        private string myFolder;
        private string fileToSendPath;
        
        private string receiveFolder;
        private bool pathSet = true;

           
        TcpListener listenerServer;
        Socket socketServer;
        public bool ListenToConnections { get { return listenToConnections; } set { listenToConnections = value; OnPropertyChanged(nameof(ListenToConnections)); } }
        bool listenToConnections = true;
        bool isPathSet = false;

        public int Progress { get { return progress; } set { progress = value; OnPropertyChanged(nameof(Progress)); } }
        private int progress = 0;

        
        
        NetworkStream networkStream;
        TcpClient tcpClient;

        Thread st;





        public int ServerPort { get { return serverPort; } set { serverPort = value; OnPropertyChanged(nameof(ServerPort)); Console.WriteLine("Serverport is now " + serverPort); } }
        private int serverPort = 6096;
        public string ConnectionFeedback { get { return connectionFeedback; } set { connectionFeedback = value; OnPropertyChanged(nameof(ConnectionFeedback)); Console.WriteLine("connectionFeedback changed to " + connectionFeedback); } }
        private string connectionFeedback = "Waiting for connection...";
        public int ProgressReceive { get { return progressReceive; } set { progressReceive = value; OnPropertyChanged(nameof(ProgressReceive)); } }
        private int progressReceive = 0;



        public MainWindowViewModel()
        {
            ChooseFolderCommand = new Command(ChooseFolder);
            SendFileCommand = new Command(SendFile);
           
            // Start a thread that calls a parameterized instance method.
           
            st = new Thread(ServerSetup);
            st.Start();
            //ThreadPool.QueueUserWorkItem(ServerSetup);
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
                isPathSet = true;
            }
        }

        private void ServerSetup(object obj)
        {
            int counter = 0;
            while (listenToConnections)
            {
                
                Console.WriteLine("Server is listening for connections...");
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

                        double percentage = bytesRead / (double)dataLen; //say filesize is 423 000 and bytesent
                        double tmp = percentage * 100;
                        int pr = (int)tmp;

                        Application.Current.Dispatcher.Invoke(() => ProgressReceive = pr );

                    }
                    if (!pathSet)
                    {
                        String myDocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        receiveFolder = myDocumentPath;
                        pathSet = true;
                    }

                    Console.WriteLine("receiveFolder set to " + receiveFolder);
                    File.WriteAllBytes(receiveFolder + "\\" + filename, data);

                   
                   
                    
                    tcpClient.Close();
                    networkStream.Close();
                    //servers.Add(currentServer);
                    Console.WriteLine("Server added, listening to port " + ServerPort);
                    
                    //ActiveServer = servers.ElementAt(0);
                    socketServer.Close();
                    listenerServer.Stop();
                    
                    //servers.RemoveAt(0);
                    Console.WriteLine("Socket closed, tcpListener closed, server removed");
                    listenToConnections = true;
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
