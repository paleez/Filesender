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

        public int MyPort { get { return myPort; } set { myPort = value; OnPropertyChanged(nameof(MyPort)); }  }
        private int myPort;
        public string TheirIp { get { return theirIp; } set { theirIp = value; OnPropertyChanged(nameof(TheirIp)); } }
        private string theirIp;
        public int TheirPort { get { return theirPort; } set { theirPort = value; OnPropertyChanged(nameof(TheirPort)); } }
        private int theirPort;
        private string myFolder = "C:\\";
        private string fileToSendPath; //mebe

        public string IsConnected { get { return isConnected; } set { isConnected = value; OnPropertyChanged(nameof(IsConnected)); } }
        private string isConnected;
        Thread listeningThread, connectingThread, sendThread;
        TcpListener tcp;
        Socket listenSocket;
        //string filePath, string hostName, int port

        public MainWindowViewModel()
        {
            ChooseFolderCommand = new Command(ChooseFolder);
            ConnectCommand = new Command(Connect);
            SendFileCommand = new Command(SendFile);
            StartMyServerCommand = new Command(StartMyServer);
            listeningThread = new Thread(Listen);
            connectingThread = new Thread(ConnectThread);
            sendThread = new Thread(SendThread);
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
            connectingThread.Start();
        }
        private void ConnectThread()
        {
            TcpClient tc = new TcpClient();
            Console.WriteLine(IPAddress.Parse(TheirIp));
            tc.Connect(IPAddress.Parse(TheirIp), TheirPort);
        }
        private void StartMyServer()
        {
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ip = ipEntry.AddressList[1];
            TcpListener tcpListener = new TcpListener(ip, myPort);
            tcp = tcpListener;
            tcpListener.Start();
            Console.WriteLine("this is ip " + ip);
            Console.WriteLine("this is port " + myPort);
            listeningThread.Start();
        }
        private void Listen()
        {
            //listenSocket = tcp.AcceptSocket();
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
        }
        private void SendFile()
        {
            sendThread.Start();
        }
        private void SendThread()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };

            var res = ofd.ShowDialog();
            if ((bool)res)
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(IPAddress.Parse(TheirIp), TheirPort);
                Console.WriteLine("this is filename " + ofd.FileName);
                client.SendFile(ofd.FileName);
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                //listenSocket.SendFile(ofd.FileName);
                //listenSocket.Shutdown(SocketShutdown.Both);
                //listenSocket.Close();
                Console.WriteLine("what");
            }
            
            
        }
        public void SendTCP(string M, string IPA, Int32 PortN)
        {
            byte[] SendingBuffer = null;
            TcpClient client = null;
            NetworkStream netstream = null;
            int BufferSize = 1024;
            try
            {
                //client = new TcpClient(IPA, PortN);
                isConnected = "Connected to the Server...\n";
                netstream = client.GetStream();
                FileStream Fs = new FileStream(M, FileMode.Open, FileAccess.Read);
                
                int NoOfPackets = Convert.ToInt32
             (Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(BufferSize)));
                //progressBar1.Maximum = NoOfPackets;
                int TotalLength = (int)Fs.Length, CurrentPacketLength, counter = 0;
                for (int i = 0; i < NoOfPackets; i++)
                {
                    if (TotalLength > BufferSize)
                    {
                        CurrentPacketLength = BufferSize;
                        TotalLength = TotalLength - CurrentPacketLength;
                    }
                    else
                        CurrentPacketLength = TotalLength;
                    SendingBuffer = new byte[CurrentPacketLength];
                    Fs.Read(SendingBuffer, 0, CurrentPacketLength);
                    netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                    //if (progressBar1.Value >= progressBar1.Maximum)
                      //  progressBar1.Value = progressBar1.Minimum;
                   // progressBar1.PerformStep();
                }

                //lblStatus.Text = lblStatus.Text + "Sent " + Fs.Length.ToString() + " 
        
                        //bytes to the server";
                     Fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                netstream.Close();
                client.Close();
            }
        }
    }
}
