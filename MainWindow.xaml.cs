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


        private bool colorClicked;
        private bool thresholdedClicked;
        Random rnd = new Random();

        UDPsender udpSender;

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

            //////////////////////////////


            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            udpSender = new UDPsender();


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



            // this.StatusText = "billede farve ";

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
            //

            CvInvoke.Normalize(mat, mat, 0, 255, NormType.MinMax);

            // convert to 8bit image
            Image<Gray, Byte> img = new Image<Gray, Byte>(infraredFrameDescription.Width, infraredFrameDescription.Height);
            mat.ConvertTo(img, DepthType.Cv8U);
            mat.Dispose();



            // Threshold for 90 % of max values
            double[] maxVal;
            img.MinMax(out _, out maxVal, out _, out _);

            Image<Gray, Byte> thresholdImg = new Image<Gray, Byte>(infraredFrameDescription.Width, infraredFrameDescription.Height);
            CvInvoke.Threshold(img, thresholdImg, maxVal[0] * .9, 255, ThresholdType.Binary);




            //    ConvolutionKernelF kernel1 = new ConvolutionKernelF(4,4);

            //   CvInvoke.Dilate(img, img, kernel1, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(1));

            Mat kernel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));

            //Mat kernel3 = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));

            thresholdImg = thresholdImg.MorphologyEx(MorphOp.Open, kernel2, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(1.0));


            /*
                        // draw examples

                        int aaa = (int)100;
                        int bbb = (int)100;
                        int radius = 20;
                        System.Drawing.Point start = new System.Drawing.Point(1, 1);
                        System.Drawing.Point second = new System.Drawing.Point(100, 100);
                        LineSegment2D line5 = new LineSegment2D(start, second);
                        CvInvoke.Line(img, start, second, new Gray(150).MCvScalar, 2);
                       */



            /*
            if(centroidPoints.Length > 1)
            {
                this.StatusText = "ole " + centroidPoints.Length + " " + stats.GetData(1,4)[0];
            } 
            */

            img = DrawTrackedData(img, thresholdImg, thresholdedClicked);



            // copy the processed image back into the backbuffer and dispose the EMGU image
            CopyMemory(infraredBitmap.BackBuffer, img.Mat.DataPointer, (int)(infraredFrameDescription.Width * infraredFrameDescription.Height));
            img.Dispose();

            // draw entire image and unlock bitmap
            infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, infraredBitmap.PixelWidth, infraredBitmap.PixelHeight));
            infraredBitmap.Unlock();




        }


        private Image<Gray, Byte> DrawTrackedData(Image<Gray, Byte> img, Image<Gray, Byte> thesholdedImg, bool showThesholdedImg)
        {


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


            foreach (MCvPoint2D64f point in centroidPoints)
            {


                if (i > 0)
                {
                    int cx = stats.GetData(i, 0)[0];
                    int cy = stats.GetData(i, 1)[0];
                    int width = stats.GetData(i, 2)[0];
                    int height = stats.GetData(i, 3)[0];
                    int area = stats.GetData(i, 4)[0];


                    if (area > 2)
                    {
                        Rectangle rect = new Rectangle((int)point.X - (width / 2) - 5, (int)point.Y - (height / 2) - 5, width + 10, height + 10);

                        CvInvoke.Rectangle(img, rect, new Gray(150).MCvScalar, 2);
                    }
                }
                i++;
            }

            if (centroidPoints.Length > 1)
            {
                udpSender.WriteToSocket("" + centroidPoints.Length + " Obejcts detected");
            }
            else
            {
                udpSender.WriteToSocket("No objects detected");
            }
            return img;

        }



        /// <summary>
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrameData">Pointer to the InfraredFrame image data</param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            this.infraredBitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this.infraredBitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / this.infraredFrameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            this.infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, this.infraredBitmap.PixelWidth, this.infraredBitmap.PixelHeight));

            // unlock the bitmap
            this.infraredBitmap.Unlock();
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
    }
}
