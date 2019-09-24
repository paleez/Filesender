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
using System.Windows;

namespace Filesender
{
    class Server : ViewModelBase
    {
        private string receiveFolder;

        public int ServerPort { get { return serverPort; } set { serverPort = value; OnPropertyChanged(nameof(ServerPort)); Console.WriteLine("Serverport is now " + serverPort); } }
        private int serverPort = 6096;
        public string ConnectionFeedback { get { return connectionFeedback; } set { connectionFeedback = value; OnPropertyChanged(nameof(ConnectionFeedback)); Console.WriteLine("connectionFeedback changed to " + connectionFeedback); } }
        private string connectionFeedback = "Waiting for connection...";
        public int ProgressReceive { get { return progressReceive; } set { progressReceive = value; OnPropertyChanged(nameof(ProgressReceive)); } }
        private int progressReceive = 0;

        private bool pathSet = true;

        Socket serverSocket;
        TcpListener listener;
        NetworkStream networkStream;
        TcpClient tcpClient;

        
        public Server(Socket socket, TcpListener listenerServer, string myFolder, bool isPathSet)
        {
            ConnectionFeedback = "Listening for connections";
            listener = listenerServer;
            serverSocket = socket;
            receiveFolder = myFolder;
            pathSet = isPathSet;
            ReceiveFile(this);
        }

        public void ReceiveFile(object obj)
        {
            tcpClient = listener.AcceptTcpClient();
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

                //Application.Current.Dispatcher.Invoke(() => ProgressReceive = pr, System.Windows.Threading.DispatcherPriority.ContextIdle); //not working as it should

            }
            if (!pathSet)
            {
                String myDocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                receiveFolder = myDocumentPath;
                pathSet = true;
            }
            
            Console.WriteLine("receiveFolder set to " + receiveFolder);
            File.WriteAllBytes(receiveFolder + "\\"  + filename, data);
            
            ConnectionFeedback = "File received";
            listener.Stop();
            serverSocket.Close();
            tcpClient.Close();
            networkStream.Close();
        }

    }
}
