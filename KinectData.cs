﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    using System;
    using Microsoft.Kinect;

    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;


    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public class KinectData : ICameraInterface
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
        /// Settings changable by user at runtime
        /// </summary>
        int minThreshold = Properties.UserSettings.Default.minThreshold;





        /// <summary>
        /// EventHandler for sending events when a frame has been processed
        /// </summary>
        public event EventHandler<EMGUargs> emguArgsProcessed;




        private bool generateColorImage;



        CoordinateMapper mapper;




        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public KinectData()
        {

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


            // get conversiontable between depthframe to cameraspace
            this.mapper = kinectSensor.CoordinateMapper;

            // do not genrate color images if the debug window is not shown
            this.generateColorImage = false;

        }

        public void GenerateColorImage(bool enable)
        {
            this.generateColorImage = enable;
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
        protected virtual void OnEmguArgsProcessed(EMGUargs e)
        {
            emguArgsProcessed?.Invoke(this, e);
        }


        /*
        /// <summary>
        /// Sends an event if the Kinect's status has changed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnChangeStatusText(bool e)
        {
            ChangeStatusText?.Invoke(this, e);
        }

            */

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

        }



        /// <summary>
        /// Handles the multisource frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private unsafe void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            EMGUargs emguArgs = new EMGUargs();

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


                DepthFrameReference depthFrameReference = multiSourceFrame.DepthFrameReference;
                depthFrame = depthFrameReference.AcquireFrame();





                if (infraredFrame == null || depthFrame == null )
                {
                    return;
                }



                // the fastest way to process the depth frame data is to directly access 
                // the underlying buffer
                using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                {
                    // verify data and write the new infrared frame data to the display bitmap
                    if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) ==
                        (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)))
                    {


                        Image<Gray, UInt16> depthImage = this.ProcessDepthFrameData(depthFrame);
                        emguArgs.DepthImage = depthImage;
                        emguArgs.DepthFrameDimension = new FrameDimension(depthFrameDescription.Width, depthFrameDescription.Height);
                //        depthImage.Dispose();


                    }
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
                        Image<Gray, UInt16> infraredImage = this.ProcessInfraredFrameDataEMGU(infraredFrame);
                        emguArgs.InfraredImage = infraredImage;
                        emguArgs.InfraredFrameDimension = new FrameDimension(infraredFrameDescription.Width, infraredFrameDescription.Height);
                      //  infraredImage.Dispose();
                    }

                    infraredFrame.Dispose();
                    infraredFrame = null;




                    if (generateColorImage)
                    {
                        ColorFrameReference colorFrameReference = multiSourceFrame.ColorFrameReference;
                        colorFrame = colorFrameReference.AcquireFrame();
                        if(colorFrame == null) {
                            return;
                                }


                        // color image
                        FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                        // the fastest way to process the color frame data is to directly access 
                        // the underlying buffer
                        using (Microsoft.Kinect.KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                        {


                            Image<Bgr, UInt16> colorImage = this.ProcessColorFrameDataEMGU(colorFrame);
                            emguArgs.Colorimage = colorImage;
                            emguArgs.ColorFrameDimension = new FrameDimension(colorFrameDescription.Width, colorFrameDescription.Height);
                            //   colorImage.Dispose();
                        }
                        // We're done with the colorFrame 
                        colorFrame.Dispose();
                        colorFrame = null;


                    }



                }

                          OnEmguArgsProcessed(emguArgs);

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


        /// <summary> EMGU VERSION
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="ColorFrame"> the InfraredFrame image </param>
        private unsafe Image<Bgr, UInt16> ProcessColorFrameDataEMGU(ColorFrame colorFrame)
        {
            Mat colorMat = new Mat(colorFrameDescription.Width, colorFrameDescription.Height, DepthType.Cv16U, 3);
            colorFrame.CopyConvertedFrameDataToIntPtr(colorMat.DataPointer, (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4), ColorImageFormat.Bgra);
            //    AddToBitmap(colorBitmap, colorMat, (colorFrameDescription.Width * colorFrameDescription.Height * 4));
            Image<Bgr, UInt16> EmguImg = colorMat.ToImage<Bgr, UInt16>();
            colorMat.Dispose();
            return EmguImg;
        }

        /// <summary> EMGU VERSION
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrame"> the InfraredFrame image </param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe Image<Gray, UInt16> ProcessInfraredFrameDataEMGU(InfraredFrame infraredFrame)
        {
            // create EMGU and copy the Frame Data into it 
            Mat mat = new Mat(infraredFrameDescription.Height, infraredFrameDescription.Width, DepthType.Cv16U, 1);
            infraredFrame.CopyFrameDataToIntPtr(mat.DataPointer, (uint)(infraredFrameDescription.Width * infraredFrameDescription.Height * 2));
            Image<Gray, UInt16> EmguImg = mat.ToImage<Gray, UInt16>();

            mat.Dispose();

            return EmguImg;
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
        private unsafe Image<Gray, UInt16> ProcessDepthFrameData(DepthFrame depthFrame)
        {

            // create EMGU and copy the Frame Data into it 
            Mat mat = new Mat(depthFrameDescription.Height, depthFrameDescription.Width, DepthType.Cv16U, 1);
            depthFrame.CopyFrameDataToIntPtr(mat.DataPointer, (uint)(depthFrameDescription.Width * depthFrameDescription.Height * 2));
            Image<Gray, UInt16> EmguImg = mat.ToImage<Gray, UInt16>();

            mat.Dispose();

            return EmguImg;

        }




        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
          //  OnChangeStatusText(e.IsAvailable);
        }





        /// <summary>
        ///  use the cameras mapper function to convert X and y's camara coordinates to world coordinates  
        /// </summary>
        /// <param name="points"></param>
        /// <param name="zCoordinates"></param>
        /// <returns></returns>
        public double[][] ScreenToWorldCoordinates(double[][] points, ushort[] zCoordinates)
        {
            double[][] newPointsTest = new double[points.Length][];
            for (int i = 0; i < points.Length; i++)
            {
                DepthSpacePoint depthSpacePoint = new DepthSpacePoint
                {
                    X = (float)points[i][0],
                    Y = (float)points[i][1]
                };
                CameraSpacePoint lutValue = mapper.MapDepthPointToCameraSpace(depthSpacePoint, zCoordinates[i]);
                newPointsTest[i] = new double[2] { lutValue.X * 1000, lutValue.Y * 1000 };
            }
            return newPointsTest;
        }
    }
}
