//------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InfraredBasics
{
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
            bool showWindow = true;



            KinectData kinectData = new KinectData();

            if (showWindow)
            {
                // Create main application window
                MainWindow mainWindow = new MainWindow(kinectData);
                mainWindow.Show();
            }


   

            

        }
    }
}
