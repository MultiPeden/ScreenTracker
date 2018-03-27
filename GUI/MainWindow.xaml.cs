//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace ScreenTracker.GUI
{
    using ScreenTracker.DataProcessing;
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media.Imaging;

    [CLSCompliant(false)]

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Current status text to display
        /// Init null to check for changes
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Indicates if the color button has been selected
        /// </summary>
        public bool colorClicked { get; set; }

        /// <summary>
        /// Indicates if the threshold button has been selected
        /// </summary>
        public bool thresholdedClicked { get; set; }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Object which is used for image proccessing
        /// Provides images shown in main window
        /// </summary>
        public ImageProcessing imageProcessing;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            //initialize button values
            this.colorClicked = true;
            this.thresholdedClicked = false;

            // use the window object as the view model
            this.DataContext = this;
            
            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        /// <summary>
        /// set function for choosing correct imageproccessor
        /// </summary>
        /// <param name="imageProcessing"></param>
        public void SetProcessor(ImageProcessing imageProcessing)
        {
            this.imageProcessing = imageProcessing;
        }

        /// <summary>
        /// Set function for showing the right most image on the main window
        /// </summary>
        /// <param name="bitmap"></param>
        public void SetRightImage(WriteableBitmap bitmap)
        {
            rightImg.Source = bitmap;
        }

        /// <summary>
        /// Set function for showing the left most image on the main window
        /// </summary>
        /// <param name="bitmap"></param>
        public void SetLeftImage(WriteableBitmap bitmap)
        {
            leftImg.Source = bitmap;
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
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
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
           //TODO kinectData.Stop_KinectData(); - CALL DESTRUCTOR
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
            imageProcessing.Threshold(true);
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

            imageProcessing.Threshold(false);
        }
    }
}
