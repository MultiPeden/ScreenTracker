﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace ScreenTracker.Communication
{
    class UDPsender
    {
        Boolean exception_thrown = false;
        Socket sending_socket;
        IPEndPoint sending_end_point;
        bool debug = false;



        public UDPsender()
        {
            sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPAddress send_to_address = IPAddress.Parse(localIPAddress());
            this.sending_end_point = new IPEndPoint(send_to_address, 11000);
      
            if (debug)
            {
                Console.WriteLine("Enter text to broadcast via UDP.");
                Console.WriteLine("Enter a blank line to exit the program.");
            }
        }

        ~UDPsender()
        {
            if (sending_socket != null)
            {
                sending_socket.Dispose();
                sending_socket = null;
            }
        }

        public void WriteToSocket(double[][] points)
        {
            // the socket object must have an array of bytes to send.
            // this loads the string entered by the user into an array of bytes.


            float[] flattened = points.SelectMany(i => i.Select(j => (float)j)).ToArray();



            var byteArray = new byte[flattened.Length * 4];
            Buffer.BlockCopy(flattened, 0, byteArray, 0, byteArray.Length);






            //  byte[] send_buffer = Encoding.ASCII.GetBytes(text_to_send);



            // Remind the user of where this is going.
            if (debug)
                Console.WriteLine("sending to address: {0} port: {1}", sending_end_point.Address, sending_end_point.Port);
            try
            {
                sending_socket.SendTo(byteArray, sending_end_point);

            }
            catch (Exception send_exception)
            {
                exception_thrown = true;
                if (debug)
                    Console.WriteLine(" Exception {0}", send_exception.Message);
            }
            if (exception_thrown == false)
            {
                if (debug)
                    Console.WriteLine("Message has been sent to the broadcast address");
            }
            else
            {
                exception_thrown = false;
                if (debug)
                    Console.WriteLine("The exception indicates the message was not sent.");
            }
        }

        private static string localIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                localIP = ip.ToString();

                string[] temp = localIP.Split('.');

                if (ip.AddressFamily == AddressFamily.InterNetwork && temp[0] == "192")
                {
                    break;
                }
                else
                {
                    localIP = null;
                }
            }
            return localIP;
        }
    }
}
