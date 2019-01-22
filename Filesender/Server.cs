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
    class Server : ViewModelBase
    {
        public ICommand ChooseFolderCommand { get; }
        private string myFolder = "C:\\Users\\darks\\Desktop\\Test\\";
        public ICommand StartMyServerCommand { get; }
        public int ServerPort { get { return serverPort; } set { serverPort = value; OnPropertyChanged(nameof(ServerPort)); } }
        private int serverPort = 6096;
        public string ConnectionFeedback { get { return connectionFeedback; } set { connectionFeedback = value; OnPropertyChanged(nameof(ConnectionFeedback)); } }
        private string connectionFeedback = "Waiting for connection";

        private bool isConnected = false;
        Socket socket;
        TcpListener listenerServer;
        NetworkStream networkStream;
        TcpClient tcpClient;

        Thread wh;
        public Server(Socket socket, TcpListener listenerServer, string myFolder)
        {
            ChooseFolderCommand = new Command(ChooseFolder);
            this.listenerServer = listenerServer;
            this.socket = socket;
            this.myFolder = myFolder;
            //ThreadPool.QueueUserWorkItem(Listen);
            ReceiveFile(this);
        }

        private void Listen(object obj)
        {
            listenerServer = new TcpListener(IPAddress.Any, ServerPort);
            listenerServer.Start();
            socket = listenerServer.AcceptSocket();
            if (socket != null)
            {
                connectionFeedback = "Connected to server";
                //ReceiveFile();
            }
        }   //not being used




        public void ReceiveFile(object obj)
        {
            Console.WriteLine("we are in");
            connectionFeedback = "Connected";
            tcpClient = listenerServer.AcceptTcpClient();
            networkStream = tcpClient.GetStream();

            
            byte[] fileSizeBytes = new byte[4];
            int bytes = networkStream.Read(fileSizeBytes, 0, 4);
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

                bytes = networkStream.Read(data, bytesRead, currentDataSize);
                bytesRead += currentDataSize;
                bytesLeft -= currentDataSize;
            }
            String myDocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string uh = "" + dataLen + ".rar";
            File.WriteAllBytes(myFolder + uh, data);

            listenerServer.Stop();
            socket.Close();
            tcpClient.Close();
            networkStream.Close();

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

    }
}
