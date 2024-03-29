﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace ScreenTracker.DataReceiver
{
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Microsoft.Kinect;
    using System;

    [CLSCompliant(false)]

    /// <summary>
    /// KinectData is the class that upholds the ICameraInterface
    /// This allows for retrival of information from the camera
    /// Aswell as conversion from camera output to EMGU images for further processing
    /// </summary>
    public class KinectData : ICamera
    {
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
        /// EventHandler for passing on the Kinects availibility status on
        /// </summary>
        public event EventHandler<bool> ChangeStatusText;

        /// <summary>
        /// EventHandler for sending events when a frame has been processed
        /// </summary>
        public event EventHandler<EMGUargs> EmguArgsProcessed;

        /// <summary>
        /// generateColorImage determines wether the color image should be created
        /// color image is only for the debugging window
        /// </summary>
        private bool generateColorImage;

        /// <summary>
        /// CoordinateMapper holds the information that allows image coordinates to
        /// be converted to worldcoordinates
        /// </summary>
        CoordinateMapper mapper;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public KinectData()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // get Reader for depth/color/ir frames
            this.reader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Depth);

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

            // get conversiontable between depthframe to cameraspace
            this.mapper = kinectSensor.CoordinateMapper;

            // do not genrate color images if the debug window is not shown
            this.generateColorImage = false;
        }
        /// <summary>
        /// GenerateColorImage is a set function for the generateColorImage boolean 
        /// </summary>
        /// <param name="enable"></param>
        public void GenerateColorImage(bool enable)
        {
            this.generateColorImage = enable;
            if (this.reader != null)
            {
                this.reader.Dispose();

            }
            if (enable)
            {

                this.reader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Color | FrameSourceTypes.Depth);
            }
            else
            {
                this.reader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Depth);
            }

            this.reader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;
        }

        /// <summary>
        /// Deconstructor for the KinectData class
        /// </summary>
        ~KinectData()
        {
            Stop_KinectData();
        }

        /// <summary>
        /// Sends an event with FrameProcessedEventArgs when a frame has be processed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnEmguArgsProcessed(EMGUargs e)
        {
            EmguArgsProcessed?.Invoke(this, e);
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
        /// Execute shutdown tasks for the KinectData class
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void Stop_KinectData()
        {
            if (this.reader != null)
            {
                // InfraredFrameReader is Disposable
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
        /// Handles the multisource frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private unsafe void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // Create instance of EMGUargs which holds the output of data from the kinect
            EMGUargs emguArgs = new EMGUargs();
            MultiSourceFrameReference frameReference = e.FrameReference;
            // Variables initialized to null for easy check of camera failures
            MultiSourceFrame multiSourceFrame = null;
            InfraredFrame infraredFrame = null;
            ColorFrame colorFrame = null;
            DepthFrame depthFrame = null;
            // Acquire frame from the Kinect
            multiSourceFrame = frameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }
            try
            {
                /*
                DepthSpacePoint dp = new DepthSpacePoint
                {
                    X = 50,
                    Y = 20
                };
                DepthSpacePoint[] dps = new DepthSpacePoint[] { dp };
                ushort[] depths = new ushort[] { 2000 };
                CameraSpacePoint[] ameraSpacePoints = new CameraSpacePoint[1];
                
                mapper.MapDepthPointsToCameraSpace(dps, depths, ameraSpacePoints);
                */
                InfraredFrameReference infraredFrameReference = multiSourceFrame.InfraredFrameReference;
                infraredFrame = infraredFrameReference.AcquireFrame();

                DepthFrameReference depthFrameReference = multiSourceFrame.DepthFrameReference;
                depthFrame = depthFrameReference.AcquireFrame();

                // Check whether needed frames are avaliable
                if (infraredFrame == null || depthFrame == null)
                {
                    return;
                }

                // the fastest way to process the depth frame data is to directly access 
                // the underlying buffer
                using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                {
                    // verify data and write the new depth frame data to the display bitmap
                    if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) ==
                        (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)))
                    {
                        // Conversion to needed EMGU image
                        Mat depthImage = this.ProcessDepthFrameData(depthFrame);

                        emguArgs.DepthImage = depthImage;
                        emguArgs.DepthFrameDimension = new FrameDimension(depthFrameDescription.Width, depthFrameDescription.Height);
                    }

                    //BgrToDephtPixel(depthBuffer.UnderlyingBuffer, depthBuffer.Size);

                    depthFrame.Dispose();
                    depthFrame = null;

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
                        // Conversion to needed EMGU image
                        Mat infraredImage = this.ProcessInfaredFrameData(infraredFrame);
                        emguArgs.InfraredImage = infraredImage;
                        emguArgs.InfraredFrameDimension = new FrameDimension(infraredFrameDescription.Width, infraredFrameDescription.Height);
                        //  infraredImage.Dispose();
                    }
                    infraredFrame.Dispose();
                    infraredFrame = null;

                    // Check as to whether or not the color image is needed for mainwindow view
                    if (generateColorImage)
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
                            // Conversion to needed EMGU image
                            Mat colorImage = this.ProcessColorFrameData(colorFrame);
                            emguArgs.Colorimage = colorImage;
                            emguArgs.ColorFrameDimension = new FrameDimension(colorFrameDescription.Width, colorFrameDescription.Height);
                        }
                        // We're done with the colorFrame 
                        colorFrame.Dispose();
                        colorFrame = null;
                    }
                }
                // Call the processing finished event for the conversion to EMGU images
                OnEmguArgsProcessed(emguArgs);
            }
            catch (Exception ex)
            {
                // ignore if the frame is no longer available
                Console.WriteLine("FRAME CHRASHED: " + ex.ToString());
            }
            finally
            {
                // generate event at send writeable bitmaps for each frame, and cleanup.
                // only generate event if the mainwindow is shown.

                // DepthFrame, ColorFrame are Disposable.
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
                // infraredFrame is Disposable
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

        /// <summary> EMGU VERSION
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="ColorFrame"> the InfraredFrame image </param>
        private unsafe Mat ProcessColorFrameData(ColorFrame colorFrame)
        {
            // create EMGU and copy the Frame Data into it 

            // Generate Mat used for EMGU images
            Mat colorMat = new Mat(colorFrameDescription.Height, colorFrameDescription.Width, DepthType.Cv8U, 4);

            // Move data to new Mat
            colorFrame.CopyConvertedFrameDataToIntPtr(colorMat.DataPointer, (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4), ColorImageFormat.Bgra);
            // Image<Bgr, UInt16> EmguImg = colorMat.ToImage<Bgr, UInt16>();
            //   colorMat.Dispose();
            return colorMat;
        }

        /// <summary> EMGU VERSION
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrame"> the InfraredFrame image </param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe Mat ProcessInfaredFrameData(InfraredFrame infraredFrame)
        {
            // create EMGU and copy the Frame Data into it 

            // Generate Mat used for EMGU images
            Mat mat = new Mat(infraredFrameDescription.Height, infraredFrameDescription.Width, DepthType.Cv16U, 1);
            // Move data to new Mat
            infraredFrame.CopyFrameDataToIntPtr(mat.DataPointer, (uint)(infraredFrameDescription.Width * infraredFrameDescription.Height * 2));


            return mat;
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
        private unsafe Mat ProcessDepthFrameData(DepthFrame depthFrame)
        {
            // create EMGU and copy the Frame Data into it 

            // Generate Mat used for EMGU images
            Mat mat = new Mat(depthFrameDescription.Height, depthFrameDescription.Width, DepthType.Cv16U, 1);
            // Move data to new Mat
            depthFrame.CopyFrameDataToIntPtr(mat.DataPointer, (uint)(depthFrameDescription.Width * depthFrameDescription.Height * 2));




            return mat;
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

        /// <summary>
        /// Sends an event if the Kinect's status has changed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnChangeStatusText(bool e)
        {
            ChangeStatusText?.Invoke(this, e);
        }




        /// <summary>
        ///  use the cameras mapper function to convert X and y's camara coordinates to world coordinates  
        /// </summary>
        /// <param name="points"></param>
        /// <param name="zCoordinates"></param>
        /// <returns></returns>
        public void ScreenToWorldCoordinates(double[][] points)
        {
            if (points != null)
            {
                DepthSpacePoint depthSpacePoint;
                CameraSpacePoint lutValue;

                double[] point;

                for (int i = 0; i < points.Length; i++)
                {
                    point = points[i];
                    depthSpacePoint = new DepthSpacePoint
                    {
                        X = (float)point[0],
                        Y = (float)point[1]
                    };
                    // Find the lutValue for the given set of coordinates
                    lutValue = mapper.MapDepthPointToCameraSpace(depthSpacePoint, (ushort)(point[2]));

                    // Convert coordinates using the found lutValue
                    points[i][0] = lutValue.X;
                    points[i][1] = lutValue.Y;
                    points[i][2] = lutValue.Z;


                }

            }
        }


        public double[][] CameraToIR(double[][] points)
        {
            if (points != null)
            {
                CameraSpacePoint camPoint = new CameraSpacePoint();
                DepthSpacePoint depthSpacePoint;
                double[][] depthSpacePoints = new double[points.Length][];
                double[] point;


                for (int i = 0; i < points.Length; i++)
                {
                    point = points[i];
                    if (point == null)
                    {
                        depthSpacePoints[i] = null;

                    }
                    else
                    {
                        camPoint.X = (float)point[0];
                        camPoint.Y = (float)point[1];
                        camPoint.Z = (float)point[2];
                        depthSpacePoint = mapper.MapCameraPointToDepthSpace(camPoint);
                        depthSpacePoints[i] = new double[] {depthSpacePoint.X,
                                                            depthSpacePoint.Y};
                    }

                }

                return depthSpacePoints;
            }
            else
            {
                return null;
            }

        }

        public double[][] CameraToColor(double[][] points)
        {


            CameraSpacePoint[] cameraSpacePoints = new CameraSpacePoint[points.Length];
            ColorSpacePoint[] colorSpacePoints = new ColorSpacePoint[points.Length];
            CameraSpacePoint cameraSpacePoint;
            ColorSpacePoint colorSpacePoint;
            double[] point;
            double[][] colorSpacePointsRes = new double[colorSpacePoints.Length][];

            for (int i = 0; i < points.Length; i++)
            {

                point = points[i];
                if (point != null)
                {


                    cameraSpacePoint.X = (float)point[0];
                    cameraSpacePoint.Y = (float)point[1];
                    cameraSpacePoint.Z = (float)point[2];
                    cameraSpacePoints[i] = cameraSpacePoint;
                }


            }


            mapper.MapCameraPointsToColorSpace(cameraSpacePoints, colorSpacePoints);

            for (int i = 0; i < colorSpacePoints.Length; i++)
            {




                colorSpacePoint = colorSpacePoints[i];
                if (colorSpacePoint != null && points[i] != null)
                {

                    point = new double[3];

                    point[0] = colorSpacePoint.X;
                    point[1] = colorSpacePoint.Y;
                    point[2] = points[i][2];


                    colorSpacePointsRes[i] = point;
                }
                else
                {
                    colorSpacePointsRes[i] = null;
                }

            }


            return colorSpacePointsRes;
        }




        public int[] IRFrameDImensions()
        {
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            return new int[] { frameDescription.Height, frameDescription.Width };
        }

    }
}
