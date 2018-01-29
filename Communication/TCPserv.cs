using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using InfraredKinectData.DataProcessing;

namespace InfraredKinectData.Communication
{
    [CLSCompliant(false)]
    public class TCPserv
    {
        // Incoming data from the client.  
        public static string data = null;
        private bool running;
        private Socket handler;
        private Socket listener;
        private ImageProcessing imageProcessing;

        public TCPserv(ImageProcessing imageProcessing)
        {
            this.imageProcessing = imageProcessing;
            this.running = true;
        }

        public void StartListening()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the   
            // host running the application.  
            IPAddress ipAddress = LocalIPAddress();
            Console.WriteLine(ipAddress);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and   
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);
                
                // Start listening for connections.  
                while (running)
                {
                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.  

                    handler = listener.Accept();
                    data = null;

                    // An incoming connection needs to be processed.  
                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EOF>") > -1)
                        {
                            break;
                        }
                    }
                    // remove <EOF>
                    data = data.Remove(data.Length - 5);
                    // Show the data on the console.  
                    Console.WriteLine("Text received : {0}", data);
                    string responce = ParseCommand(data);

                    // Echo the data back to the client.  
                    byte[] msg = Encoding.ASCII.GetBytes(responce);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private String ParseCommand(String command)
        {
            switch (command)
            {
                case "resetMesh":
                    Console.WriteLine("resetMesh");
                    imageProcessing.ResetMesh();
                    return "Mesh has been recalculated";

                default:
                    Console.WriteLine("Unable to recognize command");
                    return "Unable to recognize command";
            }
        }

        public void StopRunnning()
        {
            this.running = false;
            this.listener.Close();
        }

        private IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}
