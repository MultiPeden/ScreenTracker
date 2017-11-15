namespace Microsoft.Samples.Kinect.InfraredKinectData
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




            // create new kinectData object to processes frames from the Kinect camera
            KinectData kinectData = new KinectData(showWindow);

            if (showWindow)
            {
                // Create main application window
                MainWindow mainWindow = new MainWindow(kinectData);
                mainWindow.Show();
            }

        }
    }
}
