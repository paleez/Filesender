using Microsoft.Win32;
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
        public string ReceivedFilesPath { get { return "Receiving files in\n" + myFolder; } set { myFolder = value; OnPropertyChanged(nameof(ReceivedFilesPath)); } }
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
        private int numFilesCounter = 0;
        Thread clientInitThread;

        public string ConnectedClients { get { return connectedClients; } set { connectedClients = value; OnPropertyChanged(nameof(ConnectedClients)); } }
        private string connectedClients = "Connected clients: ";
        public MainWindowViewModel()
        {
            ChooseFolderCommand = new Command(ChooseFolder);
            SendFileCommand = new Command(StartSendFileThread);
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
        private string ReceiveFilename(NetworkStream networkStream)
        {
            byte[] fsb = new byte[4];
            int b = networkStream.Read(fsb, 0, 4);
            int s = BitConverter.ToInt32(fsb, 0);
            byte[] filenameBuf = new byte[s];
            networkStream.Read(filenameBuf, 0, s);
            return Encoding.UTF8.GetString(filenameBuf);
        }
        private int ReceiveFileSize(NetworkStream networkStream)
        {
            byte[] fileSizeBytes = new byte[4];
            int bytes = networkStream.Read(fileSizeBytes, 0, 4);
            int dataLen = BitConverter.ToInt32(fileSizeBytes, 0);
            return bytes;
        }
        private byte[] ReceiveFile(TcpClient tcpClient, NetworkStream networkStream)
        {
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
                double percentage = bytesRead / (double)dataLen;
                double tmp = percentage * 100;
                int pr = (int)tmp;
                Application.Current.Dispatcher.Invoke(() => ProgressReceive = pr);
            }
            return data;
        }
        private string SetPath()
        {
            if (!pathSet)
            {
               //string myDocumentPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
                string myDocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                myFolder = myDocumentPath;
                ReceivedFilesPath = myFolder;
                pathSet = true;
            }
            return myFolder;
        }
        private void WriteToDisc(string path, string filename, byte[] data)
        {
            File.WriteAllBytes(Path.Combine(myFolder, Path.GetFileName(filename)), data);
        }
        private void CloseConnection(TcpClient tcpClient, NetworkStream networkStream, Socket socket)
        {
            tcpClient.Close();
            networkStream.Close();
            socket.Close();
        }
        private void ServerSetup(object obj)
        {
            TcpListener listenerServer = new TcpListener(IPAddress.Any, LocalPort);
            listenerServer.Start();

            while (listenToConnections)
            {
                ConnectionFeedback = "Listening for connections on port: " + localPort;
                listenerServer.Start();
                Socket socketServer = listenerServer.AcceptSocket();
                if (socketServer != null)
                {
                    ConnectionFeedback = "Connected";
                    ConnectedClients = "Connected clients: " + ccCounter++;
                    TcpClient tcpClient = listenerServer.AcceptTcpClient();
                    NetworkStream networkStream = tcpClient.GetStream();

                    string filename = ReceiveFilename(networkStream);
                    byte[] data = ReceiveFile(tcpClient, networkStream);
                    string path = SetPath();
                    WriteToDisc(path, filename, data);
                    ConnectionFeedback = "File received";
                    numFilesCounter++;
                    CloseConnection(tcpClient, networkStream, socketServer); // listenerServer should be sent as a parameter but have to change architecture first
                    ConnectedClients = "Connected clients: " + ccCounter--;
                }
                if (numFilesCounter == 10)
                {
                    MergeFiles();
                    numFilesCounter = 0;
                }
            }
            //if (fileSize > fileSizeLimit)
            //{
            //    outputFile = new FileStream(Path.GetDirectoryName(inputFile) 
            //        + "\\" + baseFileName 
            //        + "." + i.ToString().PadLeft(5, Convert.ToChar("0")) 
            //        + extension + ".tmp", FileMode.Create, FileAccess.Write);

            //    MergeFiles(myFolder);
            //    ConnectionFeedback = "File has been merged";
            //}
            //listenerServer.Stop();
            //MergeFiles(myFolder);
        }
        private void StartSendFileThread()
        {
            clientInitThread = new Thread(SendFile);
            clientInitThread.Start();
        }

        //need to send how many files are to be sent
        private void SendFile()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };
            var res = ofd.ShowDialog();

            if ((bool)res)
            {
                fileToSendPath = ofd.FileName;
                int onegb = 1000000000;  // to test with bible file thats like 950mb, (9 = 10)
                long fileSize = new FileInfo(fileToSendPath).Length;
                //if (fileSize > onegb)
                //{
                string inputFile = fileToSendPath;
                FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                FileStream outputFile;
                MemoryStream ms = new MemoryStream();

                int numberOfFiles = 10;
                int sizeOfEachFile = (int)Math.Ceiling((double)fs.Length / numberOfFiles);


                for (int i = 0; i < numberOfFiles; i++)
                {
                    string baseFileName = Path.GetFileNameWithoutExtension(inputFile);
                    string extension = Path.GetExtension(inputFile);
                    outputFile = new FileStream(Path.GetDirectoryName(inputFile) + "\\" + baseFileName + "." + i.ToString().PadLeft(5, Convert.ToChar("0")) + extension + ".tmp", FileMode.Create, FileAccess.Write);
                    int bytesRead = 0;
                    byte[] buffer = new byte[sizeOfEachFile]; //create a buffer to write data into til its full then close and repeat
                    Console.WriteLine("This is i: " + i);
                    if ((bytesRead = fs.Read(buffer, 0, sizeOfEachFile)) > 0) outputFile.Write(buffer, 0, bytesRead);
                    outputFile.Close();
                    TransferFile(outputFile.Name);
                    File.Delete(outputFile.Name);
                }
                //}
                //else
                //{
                //    TransferFile(ofd.SafeFileName);
                //}
            }
        }


        private void TransferFile(/*TcpClient tcpClient, NetworkStream networkStream*/string filePath)
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
            clientForFileTransfer.Close();
            clientNetworkStream.Close();
        }

        private void MergeFiles()
        {
            string fp = @"C:\Users\paleez\Pictures\";
            //string fp = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
            string outPath = fp;
            Console.WriteLine("fp: " + fp);
            string[] tmpFiles = Directory.GetFiles(outPath, "*.tmp");
            for (int i = 0; i < tmpFiles.Length; i++)
            {
                Console.WriteLine("tmpfles:" + tmpFiles[i]);
            }
            FileStream outputFile = null;
            string prevFileName = "";

            foreach (string tempFile in tmpFiles)
            {

                string fileName = Path.GetFileNameWithoutExtension(tempFile);
                string baseFileName = fileName.Substring(0, fileName.IndexOf(Convert.ToChar(".")));
                string extension = Path.GetExtension(fileName);

                if (!prevFileName.Equals(baseFileName))
                {
                    if (outputFile != null)
                    {
                        outputFile.Flush();
                        outputFile.Close();
                    }
                    outputFile = new FileStream(outPath + baseFileName + extension, FileMode.OpenOrCreate, FileAccess.Write);
                }

                int bytesRead = 0;
                byte[] buffer = new byte[1024];
                FileStream inputTempFile = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Read);

                while ((bytesRead = inputTempFile.Read(buffer, 0, 1024)) > 0)
                    outputFile.Write(buffer, 0, bytesRead);

                inputTempFile.Close();
                File.Delete(tempFile);
                prevFileName = baseFileName;
            }
            Console.WriteLine("Closing file");
            outputFile.Close();
        }
    }
}
