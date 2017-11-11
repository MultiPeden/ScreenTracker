//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    using System.Drawing;

    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    using System.Threading;

    using System.Configuration;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, int count);

        /// <summary>
        /// Maximum value (as a float) that can be returned by the InfraredFrame
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        /// <summary>
        /// The value by which the infrared source data will be scaled
        /// </summary>
        private const float InfraredSourceScale = 0.75f;

        /// <summary>
        /// Smallest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// Largest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for infrared frames
        /// </summary>
        //private InfraredFrameReader infraredFrameReader = null;

        /// <summary>
        /// Description (width, height, etc) of the infrared frame data
        /// </summary>
        private FrameDescription infraredFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap infraredBitmap = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        //// new vars 20/10/2017

        /// <summary>
        /// Reader for depth/color/body index frames
        /// </summary>
        private MultiSourceFrameReader reader;

        /// <summary>
        /// Lock object for raw pixel access
        /// </summary>
        private object rawDataLock = new object();

        /// <summary>
        /// Description (width, height, etc) of the color frame data
        /// </summary>
        private FrameDescription colorFrameDescription = null;

        /// <summary>
        /// Description (width, height, etc) of the depht frame data
        /// </summary>
        private FrameDescription depthFrameDescription = null;


        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap depthBitmap = null;

        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] depthPixels = null;

        /// <summary>
        /// Settings changable by user at runtime
        /// </summary>
        int minThreshold = Properties.UserSettings.Default.minThreshold;


        /// <summary>
        /// Internally used variables
        /// </summary>

        MCvPoint2D64f[] prevPoints;

        UDPsender udpSender;

        TCPserv commands;
        Thread TCPthread;

        private bool colorClicked;
        private bool thresholdedClicked;
        Random rnd = new Random();





        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the depth frames
            //  this.infraredFrameReader = this.kinectSensor.InfraredFrameSource.OpenReader();

            this.reader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Color | FrameSourceTypes.Depth);

            // wire handler for frame arrival
            //this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;

            // Add an event handler to be called whenever depth and color both have new data
            this.reader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            // get FrameDescription from InfraredFrameSource
            this.infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;

            // create the bitmap to display
            this.infraredBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            //////////////// added 20/10/2017 

            // get FrameDescription from ColorFrameSource
            this.colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            // get FrameDescription from DephtFrameSource
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            // this.colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this.colorBitmap = new WriteableBitmap(this.colorFrameDescription.Width, this.colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgra32, null);

            // create the bitmap to display
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);


            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            this.colorClicked = true;
            this.thresholdedClicked = false;

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText:

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            udpSender = new UDPsender();

            prevPoints = null;

            // AsynchronousSocketListener commmands = new AsynchronousSocketListener();
            // commmands.StartListening();
            commands = new TCPserv(this);

            TCPthread = new Thread(commands.StartListening);
            TCPthread.Start();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.infraredBitmap;
            }
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSourceColor
        {
            get
            {
                if (colorClicked)
                {
                    return this.colorBitmap;
                }
                else
                {
                    return this.depthBitmap;
                }
            }
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
            if (this.reader != null)
            {
                // InfraredFrameReader is IDisposable
                this.reader.Dispose();
                this.reader = null;
            }


            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            if (TCPthread.IsAlive)
            {
                commands.StopRunnning();
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.infraredBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.infraredBitmap));

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
        /// Handles the infrared frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrameReference frameReference = e.FrameReference;
            MultiSourceFrame multiSourceFrame = null;
            InfraredFrame infraredFrame = null;
            ColorFrame colorFrame = null;
            DepthFrame depthFrame = null;
            multiSourceFrame = frameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            try
            {
                ColorFrameReference colorFrameReference = multiSourceFrame.ColorFrameReference;
                InfraredFrameReference infraredFrameReference = multiSourceFrame.InfraredFrameReference;
                DepthFrameReference depthFrameReference = multiSourceFrame.DepthFrameReference;
                depthFrame = depthFrameReference.AcquireFrame();
                colorFrame = colorFrameReference.AcquireFrame();
                infraredFrame = infraredFrameReference.AcquireFrame();

                if ((colorFrame == null) || (infraredFrame == null || depthFrame == null))
                {
                    return;
                }

                // color image
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                // the fastest way to process the color frame data is to directly access 
                // the underlying buffer
                using (Microsoft.Kinect.KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                {
                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == colorBitmap.PixelWidth) && (colorFrameDescription.Height == colorBitmap.PixelHeight))
                    {
                        this.ProcessColorFrameDataEMGU(colorFrame);
                    }
                }
                // We're done with the colorFrame 
                colorFrame.Dispose();
                colorFrame = null;

                // IR image
                FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;

                // the fastest way to process the infrared frame data is to directly access 
                // the underlying buffer
                using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                {
                    // verify data and write the new infrared frame data to the display bitmap
                    if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / this.infraredFrameDescription.BytesPerPixel)) &&
                        (this.infraredFrameDescription.Width == this.infraredBitmap.PixelWidth) && (this.infraredFrameDescription.Height == this.infraredBitmap.PixelHeight))
                    {
                        //      this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                        this.ProcessInfraredFrameDataEMGU(infraredFrame);
                    }
                }
                infraredFrame.Dispose();
                infraredFrame = null;

                // Depht image
                FrameDescription dephtFrameDescription = depthFrame.FrameDescription;
                bool depthFrameProcessed = false;

                // the fastest way to process the depht frame data is to directly access 
                // the underlying buffer
                using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                {
                    // verify data and write the new infrared frame data to the display bitmap
                    if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                        (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                    {
                        // Note: In order to see the full range of depth (including the less reliable far field depth)
                        // we are setting maxDepth to the extreme potential depth threshold
                        ushort maxDepth = ushort.MaxValue;

                        // If you wish to filter by reliable depth distance, uncomment the following line:
                        //// maxDepth = depthFrame.DepthMaxReliableDistance
                        this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                        depthFrameProcessed = true;
                    }
                }

                if (depthFrameProcessed)
                {
                    this.RenderDepthPixels();
                }




            }
            catch (Exception)
            {
                // ignore if the frame is no longer available
                Console.WriteLine("FRAME CHRASHED");

            }
            finally
            {
                // MultiSourceFrame, DepthFrame, ColorFrame, BodyIndexFrame are IDispoable
                if (infraredFrame != null)
                {
                    infraredFrame.Dispose();
                    infraredFrame = null;
                }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                    colorFrame = null;
                }
                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                    depthFrame = null;
                }

                if (multiSourceFrame != null)
                {
                    multiSourceFrame = null;
                }

            }


        }

        /// <summary> EMGU VERSION
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="ColorFrame"> the InfraredFrame image </param>
        private unsafe void ProcessColorFrameDataEMGU(ColorFrame colorFrame)
        {
            colorBitmap.Lock();

            Mat colorMat = new Mat(colorFrameDescription.Height, colorFrameDescription.Width, DepthType.Cv16U, 4);
            colorFrame.CopyConvertedFrameDataToIntPtr(colorMat.DataPointer, (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4), ColorImageFormat.Bgra);

            //uncomment to gray scale
            //   CvInvoke.CvtColor(colorMat, colorMat, Emgu.CV.CvEnum.ColorConversion.Bgra2Gray);
            ////Threshold call
            //    CvInvoke.Threshold(colorMat, colorMat, 220, 255, Emgu.CV.CvEnum.ThresholdType.Binary);

            // CopyMemory(colorBitmap.BackBuffer, colorMat.DataPointer, (int)(colorFrameDescription.Width * colorFrameDescription.Height));

            //comment out if grayscaled
            CopyMemory(colorBitmap.BackBuffer, colorMat.DataPointer, (colorFrameDescription.Width * colorFrameDescription.Height * 4));

            colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));

            colorBitmap.Unlock();
            colorMat.Dispose();
        }

        /// <summary> EMGU VERSION
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrame"> the InfraredFrame image </param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe void ProcessInfraredFrameDataEMGU(InfraredFrame infraredFrame)
        {
            infraredBitmap.Lock();

            // create EMGU and copy the Frame Data into it 
            Mat mat = new Mat(infraredFrameDescription.Height, infraredFrameDescription.Width, DepthType.Cv16U, 1);
            infraredFrame.CopyFrameDataToIntPtr(mat.DataPointer, (uint)(infraredFrameDescription.Width * infraredFrameDescription.Height * 2));

            // nomalize the 16bit vals to 8bit vals (max 255)
            CvInvoke.Normalize(mat, mat, 0, 255, NormType.MinMax);

            // convert to 8bit image
            Image<Gray, Byte> img = new Image<Gray, Byte>(infraredFrameDescription.Width, infraredFrameDescription.Height);
            mat.ConvertTo(img, DepthType.Cv8U);
            mat.Dispose();

            // Threshold for max(97 %, 240) 
            // double[] maxVal;
            // img.MinMax(out _, out maxVal, out _, out _);

            //    Gray average =  img.GetAverage();
            //  double avg = average.Intensity;

            //   Console.WriteLine("math max " + Math.Max(maxVal[0] * .5, minThreshold) + "             img max  " + maxVal[0]);

            Image<Gray, Byte> thresholdImg = new Image<Gray, Byte>(infraredFrameDescription.Width, infraredFrameDescription.Height);

            //  CvInvoke.Threshold(img, thresholdImg, Math.Max(maxVal[0] * .80 , minThreshold), 255, ThresholdType.Binary);
            CvInvoke.Threshold(img, thresholdImg, minThreshold, 255, ThresholdType.Binary);

            // perform opening 
            Mat kernel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(5, 5), new System.Drawing.Point(-1, -1));
            thresholdImg = thresholdImg.MorphologyEx(MorphOp.Dilate, kernel2, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(1.0));

            // find controids of reflective surfaces and mark them on the image 
            img = DrawTrackedData(img, thresholdImg, thresholdedClicked);

            // copy the processed image back into the backbuffer and dispose the EMGU image
            CopyMemory(infraredBitmap.BackBuffer, img.Mat.DataPointer, (int)(infraredFrameDescription.Width * infraredFrameDescription.Height));
            img.Dispose();
            thresholdImg.Dispose();

            // draw entire image and unlock bitmap
            infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, infraredBitmap.PixelWidth, infraredBitmap.PixelHeight));
            infraredBitmap.Unlock();
        }


        private Image<Gray, Byte> DrawTrackedData(Image<Gray, Byte> img, Image<Gray, Byte> thesholdedImg, bool showThesholdedImg)
        {

            int minArea = 2;
            int padding = 10;

            // draw centroids for connected areas 
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            MCvPoint2D64f[] centroidPoints;
            int n;

            n = CvInvoke.ConnectedComponentsWithStats(thesholdedImg, labels, stats, centroids, LineType.EightConnected, DepthType.Cv16U);

            centroidPoints = new MCvPoint2D64f[n];
            centroids.CopyTo(centroidPoints);
            int i = 0;

            if (showThesholdedImg)
            {
                img = thesholdedImg;
            }

            MCvPoint2D64f[] newPoints;
            int index;
            //  prevPoints = new Vector[centroidPoints.Length];

            if (prevPoints == null) //|| prevPoints.Length != newPoints.Length) 
            {
                newPoints = new MCvPoint2D64f[centroidPoints.Length - 1];

                // initialize points
                foreach (MCvPoint2D64f point in centroidPoints)
                {
                    if (i > 0)
                    {
                        //  int cx = stats.GetData(i, 0)[0];
                        //  int cy = stats.GetData(i, 1)[0];
                        int width = stats.GetData(i, 2)[0];
                        int height = stats.GetData(i, 3)[0];
                        int area = stats.GetData(i, 4)[0];

                        // if the area is more than minArea, discard 
                        if (true) // (area > minArea)
                        {
                            Rectangle rect = new Rectangle((int)point.X - (width / 2) - padding, (int)point.Y - (height / 2) - padding, width + padding * 2, height + padding * 2);

                            CvInvoke.Rectangle(img, rect, new Gray(150).MCvScalar, 2);
                            //if (i==0)

                            newPoints[i - 1] = new MCvPoint2D64f((int)point.X, (int)point.Y);
                        }
                    }
                    i++;
                }

            }
            else
            { // update points
                newPoints = prevPoints;

                foreach (MCvPoint2D64f point in centroidPoints)
                {
                    if (i > 0)
                    {
                        //  int cx = stats.GetData(i, 0)[0];
                        //  int cy = stats.GetData(i, 1)[0];
                        int width = stats.GetData(i, 2)[0];
                        int height = stats.GetData(i, 3)[0];
                        int area = stats.GetData(i, 4)[0];

                        // if the area is more than minArea, discard 
                        if (true) // (area > minArea)
                        {
                            Rectangle rect = new Rectangle((int)point.X - (width / 2) - padding, (int)point.Y - (height / 2) - padding, width + padding * 2, height + padding * 2);
                            CvInvoke.Rectangle(img, rect, new Gray(150).MCvScalar, 2);
                            //if (i==0)
                            index = IRUtils.LowDist(point, prevPoints);
                            newPoints[index] = point;

                        }
                        i++;
                    }
                }
                if (centroidPoints.Length > 1)
                {
                    SendJson(newPoints);
                }
                prevPoints = newPoints;
                return img;
            }

            private String PointstoJson(MCvPoint2D64f[] points)
            {
                int i = 0;
                String jSon = "{\"Items\":[";
                foreach (MCvPoint2D64f point in points)
                {
                    // invert y axis
                    jSon += IRUtils.IRPointsJson(i, (int)point.X, this.infraredFrameDescription.Height - (int)point.Y);
                    if (i < points.Length - 1)
                        jSon += ",";
                    i++;
                }
                jSon += "]}";
                return jSon;
            }

            private void SendJson(MCvPoint2D64f[] newPoints)
            {
                String jSon = PointstoJson(newPoints);
                udpSender.WriteToSocket(jSon);
                //Console.WriteLine(jSon);
            }

            /// <summary>
            /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
            /// </summary>
            /// <param name="sender">object sending the event</param>
            /// <param name="e">event arguments</param>
            private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
            {
                // set the status text
                this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                                : Properties.Resources.SensorNotAvailableStatusText;
            }

            /// <summary>
            /// Directly accesses the underlying image buffer of the DepthFrame to 
            /// create a displayable bitmap.
            /// This function requires the /unsafe compiler option as we make use of direct
            /// access to the native memory pointed to by the depthFrameData pointer.
            /// </summary>
            /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
            /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
            /// <param name="minDepth">The minimum reliable depth value for the frame</param>
            /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
            private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
            {
                // depth frame data is a 16 bit value
                ushort* frameData = (ushort*)depthFrameData;

                // convert depth to a visual representation
                for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
                {
                    // Get the depth for this pixel
                    ushort depth = frameData[i];

                    // To convert to a byte, we're mapping the depth value to the byte range.
                    // Values outside the reliable depth range are mapped to 0 (black).
                    this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
                }
            }

            /// <summary>
            /// Renders color pixels into the writeableBitmap.
            /// </summary>
            private void RenderDepthPixels()
            {
                this.depthBitmap.WritePixels(
                    new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                    this.depthPixels,
                    this.depthBitmap.PixelWidth,
                    0);
            }

            private void Button_Click_Depth(object sender, RoutedEventArgs e)
            {
                colorClicked = false;
                StatusText = "Depth clicked";
                rightImg.Source = this.depthBitmap;
            }

            private void Button_Click_Color(object sender, RoutedEventArgs e)
            {
                colorClicked = true;
                StatusText = "color clicked";
                rightImg.Source = this.colorBitmap;
            }


            private void CheckBox_threshold_Checked(object sender, RoutedEventArgs e)
            {
                thresholdedClicked = true;
                StatusText = "threshold on ";
            }

            private void CheckBox_threshold_UnChecked(object sender, RoutedEventArgs e)
            {
                thresholdedClicked = false;
                StatusText = "threshold off ";
            }


            public void ResetMesh()
            {
                prevPoints = null;
            }

        }
    }
}