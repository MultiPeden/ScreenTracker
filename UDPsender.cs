﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    class UDPsender
    {

        Boolean exception_thrown = false;
        Socket sending_socket;
        IPEndPoint sending_end_point;
        bool debug = false;

        public UDPsender()
        {

            this.sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress send_to_address = IPAddress.Parse(localIPAddress());
            this.sending_end_point = new IPEndPoint(send_to_address, 11000);

            if (debug) { 
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






        public void WriteToSocket(String text_to_send)
        {

     


            // the socket object must have an array of bytes to send.
            // this loads the string entered by the user into an array of bytes.
            byte[] send_buffer = Encoding.ASCII.GetBytes(text_to_send);


            // Remind the user of where this is going.
            if (debug)
                Console.WriteLine("sending to address: {0} port: {1}", sending_end_point.Address, sending_end_point.Port);
/*
            IRPoints iRPoints = new IRPoints(2);
            iRPoints.insert(new IRPoint(1, 2, 3), 0);
            iRPoints.insert(new IRPoint(3, 4, 5), 1);
            String jSonString = iRPoints.toJson();
            Console.WriteLine(jSonString);
            */

            try
            {
                sending_socket.SendTo(send_buffer, sending_end_point);
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
