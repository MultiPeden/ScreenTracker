﻿namespace ScreenTracker
{
    using ScreenTracker.DataProcessing;
    using ScreenTracker.DataReceiver;
    using ScreenTracker.GUI;
    using System;
    using System.Windows;

    /// <summary>
    /// Interaction logic for the App
    /// </summary>
    public partial class App : Application
    {

        void App_Startup(object sender, StartupEventArgs e)
        {
            Console.WriteLine("Starting app");


            // bool indicating if the program should start a GUI showing Kinect images(true), 
            //or only send tracked data via UDP(false)
            bool showWindow = false;

            for (int i = 0; i != e.Args.Length; ++i)
            {
                Console.WriteLine(e.Args[i]);
                if (e.Args[i] == "-s")
                {

                    showWindow = true;
                }
            }

            showWindow = true;

            //     LoadAndEstimate erer = new LoadAndEstimate();




            // create new cameraData object to processes frames from the Kinect camera
            ICamera cameraData = new KinectData();




            if (showWindow)
            {
                // Create main application window
                MainWindow mainWindow = new MainWindow();
                ImageProcessing imgProc = new ImageProcessing(cameraData, mainWindow);
                mainWindow.SetProcessor(imgProc);
                mainWindow.Show();


            }
            else
            {
                ImageProcessing imgProc = new ImageProcessing(cameraData, null);

            }



        }
    }
}
