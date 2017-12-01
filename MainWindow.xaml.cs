//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using System.Runtime.InteropServices;


    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Indicates if the color button has been selected
        /// </summary>
        private bool colorClicked;

        /// <summary>
        /// Indicates if the threshold button has been selected
        /// </summary>
        private bool thresholdedClicked;


        /// <summary>
        /// Holds a reference to the kinectData-obejct processesing Kinect-images
        /// </summary>
        KinectData kinectData;

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow(KinectData kinectData)
        {
            // get handle to Kinectdata
            this.kinectData = kinectData;

            //initialize button values
            this.colorClicked = true;
            this.thresholdedClicked = false;

            // set the status text
            this.StatusText = kinectData.SensorAvailable() ? Properties.Resources.RunningStatusText
                                                             : Properties.Resources.NoSensorStatusText;

            // listen for processed frames from the kinectData object
            kinectData.IrframeProcessed += KinectData_IrframeProcessed;
            // listen for status changes from the kinectData object's kinectSensor
            kinectData.ChangeStatusText += KinectData_ChangeStatusText;
            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        /// <summary>
        /// Receives availibility updates from the KinectData object and shows it in the MainWindow.XAML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="IsAvailable"></param>
        private void KinectData_ChangeStatusText(object sender, bool IsAvailable)
        {
            // set the status text
            this.StatusText = IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Recives processes frames from the KinectData object and show them in the MainWindow.XAML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KinectData_IrframeProcessed(object sender, FrameProcessedEventArgs e)
        {
            // show images according to the buttons selected in the GUI
            leftImg.Source = this.thresholdedClicked ? e.ThresholdBitmap : e.InfraredBitmap;
            rightImg.Source = this.colorClicked ? e.ColorBitmap : e.DepthBitmap;
        }



        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }
            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            kinectData.Stop_KinectData();
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.leftImg.Source != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create((WriteableBitmap)this.leftImg.Source));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectScreenshot-Infrared-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }



        /// <summary>
        /// Handles events when the depht button in the GUI is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Depth(object sender, RoutedEventArgs e)
        {
            colorClicked = false;
            StatusText = Properties.Resources.ButtonClickDepth;
        }

        /// <summary>
        /// Handles events when the color button in the GUI is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Color(object sender, RoutedEventArgs e)
        {
            colorClicked = true;
            StatusText = Properties.Resources.ButtonClickColor;
        }

        /// <summary>
        /// Handles events when the threshold checkbox in the GUI is checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_threshold_Checked(object sender, RoutedEventArgs e)
        {
            thresholdedClicked = true;
            StatusText = Properties.Resources.CheckBoxthresholdChecked;
            kinectData.Threshold(true);

        }

        /// <summary>
        /// Handles events when the threshold checkbox in the GUI is un-checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_threshold_UnChecked(object sender, RoutedEventArgs e)
        {
            thresholdedClicked = false;
            StatusText = Properties.Resources.CheckBoxthresholdUnChecked;
            kinectData.Threshold(false);
        }
    }
}
