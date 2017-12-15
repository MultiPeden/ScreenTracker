﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    using System.Drawing;
    using System.Xml.Serialization;

    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Accord.Collections;
    using System.Linq;
    using System.Collections.Generic;
    using Accord.Statistics;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class KinectData
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
        /// WriteableBitmaps for holding the processed frames from the Kinect Sensor
        /// infraredBitmap - for the infrared sensor
        /// infraredThesholdedBitmap - For a thresholded version of the infrared sensor
        /// colorBitmap - for the color sensor
        /// depthBitmap - for the depth sensor
        /// </summary>
        private WriteableBitmap infraredThesholdedBitmap = null;
        private WriteableBitmap infraredBitmap = null;
        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap depthBitmap = null;


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
        /// Description (width, height, etc) of the infrared frame data
        /// </summary>
        private FrameDescription infraredFrameDescription = null;

        /// <summary>
        /// Reader for depth/color/ir frames
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
        /// Description (width, height, etc) of the depth frame data
        /// </summary>
        private FrameDescription depthFrameDescription = null;

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
        /// Array for holding points found in the previous frame
        /// </summary>
        double[][] prevPoints;

        /// <summary>
        /// Holds referece to the UDPsender object responsible outgoing data(via UDP socket)
        /// </summary>
        UDPsender udpSender;

        /// <summary>
        /// Holds referece to the TCPserv object responsible ingoing commands(via TCP socket) 
        /// </summary>
        TCPserv commands;

        /// <summary>
        /// Thread for running the TCPserv
        /// </summary>
        Thread TCPthread;

        /// <summary>
        /// bool indicating in the MainWindow should be shown
        /// </summary>
        private bool showWindow;

        /// <summary>
        /// bool indicating if the thresholded image should be passed on
        /// only used if the MainWindow is shown
        /// </summary>
        private bool thresholdedClicked = false;

        /// <summary>
        /// EventHandler for sending events when a frame has been processed
        /// </summary>
        public event EventHandler<FrameProcessedEventArgs> IrframeProcessed;

        /// <summary>
        /// EventHandler for passing on the Kinects availibility status on
        /// </summary>
        public event EventHandler<bool> ChangeStatusText;

        /// <summary>
        /// Bool indicating if z coordinates should be calculated
        /// </summary>
        private bool withZCoodinates;


        private PointInfo[] pointInfo;


        CoordinateMapper mapper;

        OneEuroFilter[] oneEuroFilters;



        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public KinectData(bool showWindow)
        {
            /// Bool indicating if z coordinates should be calculated
            this.withZCoodinates = true;

            //// bool indicating in the MainWindow should be shown
            this.showWindow = showWindow;
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // get Reader for depth/color/ir frames
            this.reader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Color | FrameSourceTypes.Depth);

            // Add an event handler to be called whenever depth and color both have new data
            this.reader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            // get FrameDescription from InfraredFrameSource
            this.infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;

            // get FrameDescription from ColorFrameSource
            this.colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            // get FrameDescription from depthFrameSource
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the Kinect sensor
            this.kinectSensor.Open();

            // create UDPsender object responsible outgoing data(via UDP socket)
            udpSender = new UDPsender();

            // create TCPserv object responsible ingoing commands(via TCP socket)
            commands = new TCPserv();

            // create Thread for running the TCPserv and start it
            TCPthread = new Thread(commands.StartListening);
            TCPthread.Start();

            // Initialize the four bitmaps for processed frames
            this.infraredBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            this.infraredThesholdedBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            this.colorBitmap = new WriteableBitmap(this.colorFrameDescription.Width, this.colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            // get conversiontable between depthframe to cameraspace
             mapper = kinectSensor.CoordinateMapper;

            

            
        }

        /// <summary>
        /// Deconstructor
        /// </summary>
        ~KinectData()
        {
            Stop_KinectData();
        }

        /// <summary>
        /// Sends an event with FrameProcessedEventArgs when a frame has be processed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnIrframeProcessed(FrameProcessedEventArgs e)
        {
            IrframeProcessed?.Invoke(this, e);
        }


        /// <summary>
        /// Sends an event if the Kinect's status has changed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnChangeStatusText(bool e)
        {
            ChangeStatusText?.Invoke(this, e);
        }

        /// <summary>
        /// Getter function to get the sensor availibility
        /// </summary>
        /// <returns></returns>
        public bool SensorAvailable()
        {
            return kinectSensor.IsAvailable;
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void Stop_KinectData()
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
        /// Handles the multisource frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private unsafe void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            FrameProcessedEventArgs frameProcessedEventArgs = new FrameProcessedEventArgs();

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
                InfraredFrameReference infraredFrameReference = multiSourceFrame.InfraredFrameReference;
                infraredFrame = infraredFrameReference.AcquireFrame();

                if (infraredFrame == null)
                {
                    return;
                }


                // IR image
                FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;

                // the fastest way to process the infrared frame data is to directly access 
                // the underlying buffer
                using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                {
                    // verify data and write the new infrared frame data to the display bitmap
                    if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / this.infraredFrameDescription.BytesPerPixel)))
                    {

                        //  ushort* frameData = (ushort*)infraredBuffer.UnderlyingBuffer;

                        //  ushort index = (ushort)(infraredFrameDescription.Width * 100 + 100);

                        //Console.Write("raw buffer: " + frameData[index] + " emgu: ");

                        this.ProcessInfraredFrameDataEMGU(infraredFrame);



                        if (this.withZCoodinates || this.showWindow)
                        {



                            DepthFrameReference depthFrameReference = multiSourceFrame.DepthFrameReference;
                            depthFrame = depthFrameReference.AcquireFrame();


                            if (depthFrame == null)
                            {
                                return;
                            }
                            bool depthFrameProcessed = false;
                            // the fastest way to process the depth frame data is to directly access 
                            // the underlying buffer
                            using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                            {
                                // verify data and write the new infrared frame data to the display bitmap
                                if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) ==
                                    (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)))
                                {


                                    if (this.showWindow)
                                    {
                                        // Note: In order to see the full range of depth (including the less reliable far field depth)
                                        // we are setting maxDepth to the extreme potential depth threshold
                                        ushort maxDepth = ushort.MaxValue;

                                        // If you wish to filter by reliable depth distance, uncomment the following line:
                                        //// maxDepth = depthFrame.DepthMaxReliableDistance

                                        this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                                        depthFrameProcessed = true;

                                        if (depthFrameProcessed)
                                        {
                                            this.RenderDepthPixels();
                                        }
                                    }


                                    if (this.withZCoodinates && this.prevPoints.Length > 1)
                                    {
                                        ushort[] zCoodinates = GetZCoordinatesSurroundingBox(depthBuffer.UnderlyingBuffer);

                                        SendPoints(this.prevPoints, zCoodinates);


                                    }

                                }
                                depthFrame.Dispose();
                                depthFrame = null;

                            }
                        }
                        else
                        {                        
                            if (this.prevPoints.Length > 1)
                            {

                                SendPoints(this.prevPoints);
                            }
                        }

                    }

                }

                infraredFrame.Dispose();
                infraredFrame = null;

                // only get color and depthframe if the mainwindow is shown 
                if (this.showWindow)
                {
                    ColorFrameReference colorFrameReference = multiSourceFrame.ColorFrameReference;
                    colorFrame = colorFrameReference.AcquireFrame();

                    if (colorFrame == null)
                    {
                        return;
                    }


                    // color image
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    // the fastest way to process the color frame data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {

                        this.ProcessColorFrameDataEMGU(colorFrame);

                    }
                    // We're done with the colorFrame 
                    colorFrame.Dispose();
                    colorFrame = null;






                }
            }
            catch (Exception ex)
            {
                // ignore if the frame is no longer available
                Console.WriteLine("FRAME CHRASHED: " + ex.ToString());
            }
            finally
            {
                // generate event at send writeable bitmaps for each frame, and cleanup

                // only generate event if the mainwindow is shown
                if (this.showWindow)
                {
                    frameProcessedEventArgs.ColorBitmap = this.colorBitmap;
                    frameProcessedEventArgs.InfraredBitmap = this.infraredBitmap;
                    frameProcessedEventArgs.ThresholdBitmap = this.infraredThesholdedBitmap;
                    frameProcessedEventArgs.DepthBitmap = this.depthBitmap;

                    OnIrframeProcessed(frameProcessedEventArgs);

                    // DepthFrame, ColorFrame are IDispoable
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
                }
                // infraredFrame is IDispoable
                if (infraredFrame != null)
                {
                    infraredFrame.Dispose();
                    infraredFrame = null;
                }
                if (multiSourceFrame != null)
                {
                    multiSourceFrame = null;
                }

            }


        }
        /// <summary>
        /// The z-coordinate from the depth-camera is almost always 0 for centroids because it can not 
        /// measure the depth of reflektive surfacres, thus we have to estimate the depth using the 
        /// surrounding pixels.
        /// </summary>
        /// <param name="depthFrameData"></param>
        /// <returns></returns>
        private unsafe ushort[] GetZCoordinatesStep(IntPtr depthFrameData)
        {


            ushort[] zCoordinates = new ushort[this.prevPoints.Length];

            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;
            // Console.WriteLine("hej");
            // Console.WriteLine(frameData[5]);

            int i = 0;
            int j = 0;
            foreach (double[] point in this.prevPoints)
            {
                double x = point[0];
                double y = point[1];
                while (y >= 0)
                {
                    ushort index = (ushort)(depthFrameDescription.Width * x + y);
                    ushort zval = frameData[index];
                    if (zval > 0)
                    {

                        if (j > 4)
                        {
                            zCoordinates[i] = zval;
                            break;
                        }
                        j++;
                    }
                    y--;

                }

                i++;
            }
            return zCoordinates;
        }


        /// <summary>
        /// The z-coordinate from the depth-camera is almost always 0 for centroids because it can not 
        /// measure the depth of reflektive surfacres, thus we have to estimate the depth using the 
        /// surrounding pixels.
        /// </summary>
        /// <param name="depthFrameData"></param>
        /// <returns></returns>
        private unsafe ushort[] GetZCoordinatesSurroundingBox(IntPtr depthFrameData)
        {

            ushort[] zCoordinates = new ushort[this.prevPoints.Length];

            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            double imgwidth = infraredBitmap.Width;

            List<double> zCoords = new List<double>();

            for (int i = 0; i < prevPoints.Length; i++)
            {
                PointInfo p = pointInfo[i];

                double x = Math.Round(prevPoints[i][0]);
                double y = Math.Round(prevPoints[i][1]);
                int width = (p.Width / 2) + 1;
                //  width = +5;
                int height = (p.Height / 2) + 1;
                // height = +5;
                int frameWidth = depthFrameDescription.Width;


                // add cardinal points (N,E,S,W)
                AddDephtPixel(x + width, y, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x - width, y, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x, y + height, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x, y - height, imgwidth, ref frameData, ref zCoords);

                // add itercardinal points (NE,SE,SW,NW)
                AddDephtPixel(x + width, y + height, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x - width, y - height, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x + width, y - height, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x - width, y + height, imgwidth, ref frameData, ref zCoords);

                if (zCoords.Count != 0)
                {
                    //
                    double zval = Measures.Median(zCoords.ToArray());
         

                    // non filtered z-val
                   // zCoordinates[i] = (ushort)zval;
                    // filtered z val
                    zCoordinates[i] = (ushort)oneEuroFilters[i].Filter(zval, 60);





                }
                else
                {
                    zCoordinates[i] = 0;
                }

            }

            return zCoordinates;
        }


        private unsafe void AddDephtPixel(double x, double y, double width, ref ushort* frameData, ref List<double> zCoords)
        {
            try
            {

                zCoords.Add(frameData[(int)(width * y + x)]);


                this.depthPixels[(int)(width * y + x)] = 255;
                this.depthPixels[(int)(width * (y + 1) + x)] = 255;
                this.depthPixels[(int)(width * y + x + 1)] = 255;
                this.depthPixels[(int)(width * (y - 1) + x)] = 255;
                this.depthPixels[(int)(width * y + x - 1)] = 255;


            }
            catch (Exception)
            {

                // do nothing
            }


        }


        private ushort From2DTo1DArray(double x, double y, int width)
        {
            return (ushort)(width * x + y);
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

        /// <summary>
        /// Copies the "data" into the "bitmap" with datasize "dataSize"
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="data"></param>
        /// <param name="dataSize"></param>
        private void AddToBitmap(WriteableBitmap bitmap, Mat data, int dataSize)
        {
            bitmap.Lock();
            CopyMemory(bitmap.BackBuffer, data.DataPointer, dataSize);
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
            data.Dispose();
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
            Mat colorMat = new Mat(colorFrameDescription.Height, colorFrameDescription.Width, DepthType.Cv16U, 4);
            colorFrame.CopyConvertedFrameDataToIntPtr(colorMat.DataPointer, (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4), ColorImageFormat.Bgra);
            AddToBitmap(colorBitmap, colorMat, (colorFrameDescription.Width * colorFrameDescription.Height * 4));
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

            // create EMGU and copy the Frame Data into it 
            Mat mat = new Mat(infraredFrameDescription.Height, infraredFrameDescription.Width, DepthType.Cv16U, 1);
            infraredFrame.CopyFrameDataToIntPtr(mat.DataPointer, (uint)(infraredFrameDescription.Width * infraredFrameDescription.Height * 2));



            // convert to 8bit image
            Image<Gray, Byte> img = new Image<Gray, Byte>(infraredFrameDescription.Width, infraredFrameDescription.Height);
            Image<Gray, Byte> thresholdImg = new Image<Gray, Byte>(infraredFrameDescription.Width, infraredFrameDescription.Height);

            // nessesary for calling  CvInvoke.Threshold because it only supports 8 and 32-bit datatypes  
            mat.ConvertTo(img, DepthType.Cv32F);
            mat.Dispose();


            // find max val of the 16 bit ir-image
            img.MinMax(out _, out double[] maxVal, out _, out _);

            // apply threshold with 98% of maxval || minThreshold
            // to obtain binary image with only 0's & 255
            float percentageThreshold = Properties.UserSettings.Default.PercentageThreshold;
            int minThreshold = Properties.UserSettings.Default.minThreshold;
            CvInvoke.Threshold(img, thresholdImg, Math.Max(maxVal[0] * percentageThreshold, minThreshold), 255, ThresholdType.Binary);


            // nomalize the 16bit vals to 8bit vals (max 255)
            //  CvInvoke.Normalize(img.Mat, img.Mat, 0, 255, NormType.MinMax, DepthType.Cv8U);
            img.Mat.ConvertTo(img, DepthType.Cv16U);

            // convert back to 8 bit for showing as a bitmap
            thresholdImg.Mat.ConvertTo(thresholdImg, DepthType.Cv8U);

            // perform opening 
            int kernelSize = Properties.UserSettings.Default.kernelSize;
            Mat kernel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(kernelSize, kernelSize), new System.Drawing.Point(-1, -1));
            thresholdImg = thresholdImg.MorphologyEx(MorphOp.Dilate, kernel2, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(1.0));

            // find controids of reflective surfaces and mark them on the image 
            img = DrawTrackedData(img, thresholdImg, thresholdedClicked);


            // only generate writeable bitmap if the mainwindow is shown
            if (this.showWindow)
            {
                // copy the processed image back into a writeable bitmap and dispose the EMGU image
                if (thresholdedClicked)
                {
                    AddToBitmap(infraredThesholdedBitmap, img.Mat, (int)(infraredFrameDescription.Width * infraredFrameDescription.Height));
                }
                else
                {
                    AddToBitmap(infraredBitmap, img.Mat, (int)(infraredFrameDescription.Width * infraredFrameDescription.Height * 2));
                }
            }

            // cleanup
            thresholdImg.Dispose();
            img.Dispose();
        }


        /// <summary>
        /// Finds connected components in the thresholded image(Binary) and draws rectangles around them
        /// returns the thesholded image if "showThesholdedImg" is true, and the non-thresholded otherwise
        /// </summary>
        /// <param name="img"></param>
        /// <param name="thresholdImg"></param>
        /// <param name="showThesholdedImg"></param>
        /// <returns></returns>
        private Image<Gray, Byte> DrawTrackedData(Image<Gray, Byte> img, Image<Gray, Byte> thresholdImg, bool showThesholdedImg)
        {

            int minArea = Properties.UserSettings.Default.DataIndicatorMinimumArea;
            int padding = Properties.Settings.Default.DataIndicatorPadding;


            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();

            MCvPoint2D64f[] centroidPoints;
            int n;

            n = CvInvoke.ConnectedComponentsWithStats(thresholdImg, labels, stats, centroids, LineType.EightConnected, DepthType.Cv16U);



            centroidPoints = new MCvPoint2D64f[n];
            centroids.CopyTo(centroidPoints);

            double[][] centroidPoints2 = new double[n - 1][];
            pointInfo = new PointInfo[n - 1];


            int i = 0;
            foreach (MCvPoint2D64f point in centroidPoints)
            {
                if (i > 0)
                {
                    centroidPoints2[i - 1] = new double[2] { point.X, point.Y };
                }
                i++;
            }






            //            arr = (int[,])ResizeArray(arr, new int[] { 12, 2 });



            int colorcode;
            if (showThesholdedImg)
            {
                img = thresholdImg;
                colorcode = Properties.Settings.Default.DataIndicatorColor8bit;
            }
            else
            {
                colorcode = Properties.Settings.Default.DataIndicatorColor;
            }

            double[][] newPoints;
            int index;

            int thickness = Properties.UserSettings.Default.DataIndicatorThickness;

            i = 0;
            if (prevPoints == null) //|| prevPoints.Length != newPoints.Length) 
            {
                newPoints = centroidPoints2;

                // add z filters
                oneEuroFilters = new OneEuroFilter[centroidPoints2.Length];
                // initialize points
                foreach (double[] point in centroidPoints2)
                {
                    int j = i + 1;
                    //  int cx = stats.GetData(i, 0)[0];
                    //  int cy = stats.GetData(i, 1)[0];
                    int width = stats.GetData(j, 2)[0];
                    int height = stats.GetData(j, 3)[0];
                    int area = stats.GetData(j, 4)[0];

                    pointInfo[i] = new PointInfo(width, height);
                    oneEuroFilters[i] = new OneEuroFilter(1,0);


                    // if the area is more than minArea, discard 
                    if (true) // (area > minArea)
                    {
                        // only draw rectangles if the MainWindow is shown
                        if (this.showWindow)
                        {
                            Rectangle rect = new Rectangle((int)point[0] - (width / 2) - padding, (int)point[0] - (height / 2) - padding, width + padding * 2, height + padding * 2);
                            CvInvoke.Rectangle(img, rect, new Gray(colorcode).MCvScalar, thickness); // 2 pixel box thick
                        }

                        //if (i==0)

                        //      newPoints[i] = new double[] { (int)point[0], (int)point[1] };

                    }
                    i++;
                }

            }
            else
            { // update points
                newPoints = prevPoints;

                KDTree<int> tree = KDTree.FromData<int>(prevPoints, Enumerable.Range(0, prevPoints.Length).ToArray());




                foreach (double[] point in centroidPoints2)
                {
                    int j = i + 1;
                    //  int cx = stats.GetData(i, 0)[0];
                    //  int cy = stats.GetData(i, 1)[0];
                    int width = stats.GetData(j, 2)[0];
                    int height = stats.GetData(j, 3)[0];
                    int area = stats.GetData(j, 4)[0];

                    // if the area is more than minArea, discard 
                    if (true) // (area > minArea)
                    {
                        Rectangle rect = new Rectangle((int)point[0] - (width / 2) - padding, (int)point[1] - (height / 2) - padding, width + padding * 2, height + padding * 2);

                        CvInvoke.Rectangle(img, rect, new Gray(colorcode).MCvScalar, thickness);
                        //if (i==0)
                        index = tree.Nearest(point).Value;

                        pointInfo[index] = new PointInfo(width, height);




                        newPoints[index] = point;

                    }

                    i++;
                }

            }



            // send the identified points via UDP in Json format


            //  Console.WriteLine(newPoints.Length);

            prevPoints = newPoints;
            return img;
        }







        /// <summary>
        /// Converts a list of points to Json and sends it via UDP socket
        /// </summary>
        /// <param name="newPoints"></param>
        private void SendPoints(double[][] newPoints, ushort[] zCoordinates = null)
        {


            double[][] newPointsTest = ConvertPoints(newPoints, zCoordinates);

            String jSon = IRUtils.PointstoJson(newPointsTest, zCoordinates, this.infraredFrameDescription.Width, this.infraredFrameDescription.Width);
            udpSender.WriteToSocket(jSon);

            RenderDepthPixels();
        }

        private double[][] ConvertPoints(double[][] newPoints, ushort[] zCoordinates)
        {

            double[][] newPointsTest = new double[newPoints.Length][];

            for (int i = 0; i < newPoints.Length; i++)
            {

  

                DepthSpacePoint depthSpacePoint = new DepthSpacePoint
                {
                    X = (float)newPoints[i][0],
                    Y = (float)newPoints[i][1]
                };
                CameraSpacePoint lutValue = mapper.MapDepthPointToCameraSpace(depthSpacePoint, zCoordinates[i]);

     
                newPointsTest[i] = new double[2] { lutValue.X *1000 , lutValue.Y  *1000 };
            }

            return newPointsTest;

        }


        private double[][] ConvertPoints2(double[][] newPoints, ushort[] zCoordinates)
        {
            CameraSpacePoint cameraPoint;
            DepthSpacePoint depthPoint;
            CoordinateMapper mapper = kinectSensor.CoordinateMapper;
            double[][] newPointsTest = new double[newPoints.Length][];

            for (int i = 0; i < newPoints.Length; i++)
            {

                /*
                cameraPoint = new CameraSpacePoint
                {
                    X = (float) newPoints[i][0],
                    Y = (float) newPoints[i][1],
                    Z = zCoordinates[i]

                };
                */
                depthPoint = new DepthSpacePoint
                {
                    X = (float)newPoints[i][0],
                    Y = (float)newPoints[i][1]
                };
                //       depthPoint = mapper.MapCameraPointToDepthSpace(cameraPoint);
                cameraPoint = mapper.MapDepthPointToCameraSpace(depthPoint, zCoordinates[i]);

                newPointsTest[i] = new double[2] { cameraPoint.X, cameraPoint.Y };

            }

            return newPointsTest;

        }



        private double[][] ConvertPoints1(double[][] newPoints, int[] zCoordinates)
        {
            CameraSpacePoint cameraPoint;
            DepthSpacePoint depthPoint;
            CoordinateMapper mapper = kinectSensor.CoordinateMapper;
            double[][] newPointsTest = new double[newPoints.Length][];

            for (int i = 0; i < newPoints.Length; i++)
            {


                cameraPoint = new CameraSpacePoint
                {
                    X = (float)newPoints[i][0],
                    Y = (float)newPoints[i][1],
                    Z = zCoordinates[i]

                };


                depthPoint = mapper.MapCameraPointToDepthSpace(cameraPoint);

                newPointsTest[i] = new double[2] { depthPoint.X, depthPoint.Y };

            }

            return newPointsTest;

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
        /// setter function for the thresholdedClicked bool
        /// </summary>
        /// <param name="threshold"></param>
        public void Threshold(bool threshold)
        {
            thresholdedClicked = threshold;


        }

        /// <summary>
        /// sets prevPoints to null, resetting the point list.
        /// </summary>
        public void ResetMesh()
        {
            prevPoints = null;
        }


        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            OnChangeStatusText(e.IsAvailable);
        }

    }
}
