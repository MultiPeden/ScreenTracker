using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Accord.Collections;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Accord.Statistics;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    [CLSCompliant(false)]
    public class ImageProcessing
    {
        
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, int count);


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
        /// Info for each detected point i the frame.
        /// </summary>
        private PointInfo[] pointInfo;

        private ICameraInterface cameraData;

        private MainWindow mainWindow;


        public ImageProcessing(ICameraInterface cameraData, MainWindow mainWindow)
        {

            if (mainWindow != null && mainWindow.colorClicked)
            {

                cameraData.GenerateColorImage(true);
            }


            this.mainWindow = mainWindow;

            this.cameraData = cameraData;



            //// bool indicating in the MainWindow should be shown
            //this.showWindow = showWindow;


            // create UDPsender object responsible outgoing data(via UDP socket)
            udpSender = new UDPsender();

            // create TCPserv object responsible ingoing commands(via TCP socket)
            commands = new TCPserv();

            // create Thread for running the TCPserv and start it
            TCPthread = new Thread(commands.StartListening);
            TCPthread.Start();

            cameraData.emguArgsProcessed += KinectData_EmguImageReceived;

            // listen for status changes from the camera object's kinectSensor
            cameraData.ChangeStatusText += KinectData_ChangeStatusText;

            // get handle to Kinectdata
            this.cameraData = cameraData;

            /* TODO
            // set the status text
            StatusText = cameraData.SensorAvailable() ? Properties.Resources.RunningStatusText
                                                             : Properties.Resources.NoSensorStatusText;
                                                             */

            showWindow = true;

        }


        /// <summary>
        /// Deconstructor
        /// </summary>
        ~ImageProcessing()
        {
            Stop_ImageProcessing();
        }




        /// <summary>
        /// Recives processes frames from the KinectData object and show them in the MainWindow.XAML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KinectData_EmguImageReceived(object sender, EMGUargs e)
        {

            // Process infrared image and track points
            this.ProcessInfraredFrame(e.InfraredImage, e.InfraredFrameDimension);
            // get z-coordinates
            ushort[] zCoordinates = this.GetZCoordinatesSurroundingBox(e.DepthImage);
            // Send point via UDP
            SendPoints(prevPoints, zCoordinates);



            if (mainWindow != null)
            {

                if (mainWindow.colorClicked)
                {
                    this.SetColorImage(e.Colorimage, e.ColorFrameDimension);

                }
                else
                {
                    this.SetDepthImage(e.DepthImage, e.DepthFrameDimension);
                }

            }




            // show images according to the buttons selected in the GUI
            //    leftImg.Source = this.thresholdedClicked ? e.ThresholdBitmap : e.InfraredBitmap;
            //   rightImg.Source = this.colorClicked ? e.ColorBitmap : e.DepthBitmap;
        }

        public void GenerateColorImage(bool enable)
        {
            cameraData.GenerateColorImage(enable);
        }


        private void SetColorImage(Image<Bgr, UInt16> img, FrameDimension frameDimension)
        {

            if (this.colorBitmap == null)
            {
                this.colorBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            }


            AddToBitmap(this.colorBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 4));

            mainWindow.SetRightImage(this.colorBitmap);
        }


        private void SetInfraredImage(Image<Gray, UInt16> img, FrameDimension frameDimension)
        {
            if (this.infraredBitmap == null)
            {
                this.infraredBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            }

            AddToBitmap(this.infraredBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 2));

            mainWindow.SetLeftImage(this.infraredBitmap);

        }


        private void SetThresholdedInfraredImage(Image<Gray, Byte> img, FrameDimension frameDimension)
        {
            if (this.infraredThesholdedBitmap == null)
            {
                this.infraredThesholdedBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            }

            AddToBitmap(this.infraredThesholdedBitmap, img.Mat, (frameDimension.Width * frameDimension.Height));

            mainWindow.SetLeftImage(this.infraredThesholdedBitmap);

        }


        private void SetDepthImage(Image<Gray, UInt16> img, FrameDimension frameDimension)
        {
            if (this.depthBitmap == null)
            {
                this.depthBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            }

            ///  img = img.Mul(10);


            CvInvoke.Normalize(img.Mat, img.Mat, 0, 65535, NormType.MinMax);
            // CvInvoke.Normalize(_histogram, _histogram, 0, 255, NormType.MinMax);

            AddToBitmap(this.depthBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 2));

            mainWindow.SetRightImage(this.depthBitmap);

        }






        /// <summary>
        /// The z-coordinate from the depth-camera is almost always 0 for centroids because it can not 
        /// measure the depth of reflektive surfacres, thus we have to estimate the depth using the 
        /// surrounding pixels.
        /// </summary>
        /// <param name="depthFrameData"></param>
        /// <returns></returns>
        private unsafe ushort[] GetZCoordinatesSurroundingBox(Image<Gray, UInt16> depthImage)
        {

            // init array to the size of the array with tracked points
            ushort[] zCoordinates = new ushort[this.prevPoints.Length];


            //double imgwidth = infraredBitmap.Width;

            // list for each point's depth sample points
            List<double> zCoords;

            for (int i = 0; i < prevPoints.Length; i++)
            {
                zCoords = new List<double>();
                PointInfo p = pointInfo[i];

                int x = (int)prevPoints[i][0];
                int y = (int)prevPoints[i][1];

                int width = (p.Width / 2) + 1;
                //  width = +5;
                int height = (p.Height / 2) + 1;
                // height = +5;
                // int frameWidth = depthFrameDimension.Width;


                zCoords.Add(depthImage.Data[(x + width), y, 0]);

                // add cardinal points (N,E,S,W)
                AddDephtPixel(x + width, y, ref depthImage, ref zCoords);
                AddDephtPixel(x - width, y, ref depthImage, ref zCoords);
                AddDephtPixel(x, y + height, ref depthImage, ref zCoords);
                AddDephtPixel(x, y - height, ref depthImage, ref zCoords);

                // add inter-cardinal points (NE,SE,SW,NW)
                AddDephtPixel(x + width, y + height, ref depthImage, ref zCoords);
                AddDephtPixel(x - width, y - height, ref depthImage, ref zCoords);
                AddDephtPixel(x + width, y - height, ref depthImage, ref zCoords);
                AddDephtPixel(x - width, y + height, ref depthImage, ref zCoords);

                if (zCoords.Count != 0)
                {

                    // find the z-val by calc the median of the cardinal and inter-cardinal points
                    double zval = Measures.Median(zCoords.ToArray());
                    // apply one-euro-filter
                    zCoordinates[i] = (ushort)p.Filter(zval);

                }
                else
                {
                    zCoordinates[i] = 0;
                }

            }

            return zCoordinates;
        }


        private void AddDephtPixel(int x, int y, ref Image<Gray, UInt16> depthImage, ref List<double> zCoords)
        {

            try
            {
                if (x >= 0 && y >= 0)
                {
                    ushort zCoord = depthImage.Data[x, y, 0];
                    if(zCoord > 0)
                    {
                        zCoords.Add(zCoord);
                    }
                    
                }
            }
            catch (Exception)
            {

                // do nothing - out of frame
            }

        }



        /// <summary>
        /// Copies the "data" into the "bitmap" with datasize "dataSize"
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="data"></param>
        /// <param name="dataSize"></param>
        private void EMGUToBitmap(WriteableBitmap bitmap, Mat data, int dataSize)
        {
            bitmap.Lock();
            CopyMemory(bitmap.BackBuffer, data.DataPointer, dataSize);
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
            data.Dispose();
        }



        /// <summary>
        /// Finds connected components in the thresholded image(Binary) and draws rectangles around them
        /// returns the thesholded image if "showThesholdedImg" is true, and the non-thresholded otherwise
        /// </summary>
        /// <param name="img"></param>
        /// <param name="thresholdImg"></param>
        /// <param name="showThesholdedImg"></param>
        /// <returns></returns>
        private void TrackedData(Image<Gray, Byte> thresholdImg)
        {

            int minArea = Properties.UserSettings.Default.DataIndicatorMinimumArea;


            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();

            MCvPoint2D64f[] centroidPoints;
            int n;

            n = CvInvoke.ConnectedComponentsWithStats(thresholdImg, labels, stats, centroids, LineType.EightConnected, DepthType.Cv16U);



            centroidPoints = new MCvPoint2D64f[n];
            centroids.CopyTo(centroidPoints);

            double[][] centroidPoints2 = new double[n - 1][];



            int i = 0;
            foreach (MCvPoint2D64f point in centroidPoints)
            {
                if (i > 0)
                {
                    centroidPoints2[i - 1] = new double[2] { point.X, point.Y };
                }
                i++;
            }




            double[][] newPoints;
            int index;



            i = 0;
            if (prevPoints == null)// || prevPoints.Length != centroidPoints2.Length) 
            {
                newPoints = centroidPoints2;

                pointInfo = new PointInfo[n - 1];


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



                    i++;
                }

            }
            else
            { // update points
                newPoints = prevPoints;

                KDTree<int> tree = KDTree.FromData<int>(prevPoints, Enumerable.Range(0, prevPoints.Length).ToArray());


                // List<int> indexList = new List<int>();

                foreach (double[] point in centroidPoints2)
                {
                    int j = i + 1;
                    //  int cx = stats.GetData(i, 0)[0];
                    //  int cy = stats.GetData(i, 1)[0];
                    int width = stats.GetData(j, 2)[0];
                    int height = stats.GetData(j, 3)[0];
                    int area = stats.GetData(j, 4)[0];

                    //  int[] indexArray;

                    // if the area is more than minArea, discard 
                    if (true) // (area > minArea)
                    {

                        KDTreeNode<int> nearest = tree.Nearest(point);
                        index = nearest.Value;



                        // update info for the point
                        PointInfo pInfo = pointInfo[index];
                        pInfo.Width = width;
                        pInfo.Height = height;

                        newPoints[index] = point;

                    }

                    i++;
                }

            }



            // send the identified points via UDP in Json format


            //  Console.WriteLine(newPoints.Length);

            prevPoints = newPoints;
        }





        /// <summary>
        /// Converts a list of points to Json and sends it via UDP socket
        /// </summary>
        /// <param name="newPoints"></param>
        private void SendPoints(double[][] newPoints, ushort[] zCoordinates)
        {


            double[][] worldCoordinates = cameraData.ScreenToWorldCoordinates(newPoints, zCoordinates);

            String jSon = IRUtils.PointstoJson(worldCoordinates, zCoordinates);
            udpSender.WriteToSocket(jSon);


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


        public void Stop_ImageProcessing()
        {
            if (TCPthread.IsAlive)
            {
                commands.StopRunnning();
            }
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
        /// <param name="infraredFrame"> the InfraredFrame image </param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private void ProcessInfraredFrame(Image<Gray, UInt16> infraredFrame, FrameDimension infraredFrameDimension)
        {

            Image<Gray, Byte> thresholdImg = new Image<Gray, Byte>(infraredFrameDimension.Width, infraredFrameDimension.Height);

            // nessesary for calling  CvInvoke.Threshold because it only supports 8 and 32-bit datatypes  
            infraredFrame.Mat.ConvertTo(infraredFrame, DepthType.Cv32F);



            // find max val of the 16 bit ir-image
            infraredFrame.MinMax(out _, out double[] maxVal, out _, out _);

            // apply threshold with 98% of maxval || minThreshold
            // to obtain binary image with only 0's & 255
            float percentageThreshold = Properties.UserSettings.Default.PercentageThreshold;
            int minThreshold = Properties.UserSettings.Default.minThreshold;
            CvInvoke.Threshold(infraredFrame, thresholdImg, Math.Max(maxVal[0] * percentageThreshold, minThreshold), 255, ThresholdType.Binary);


            // nomalize the 16bit vals to 8bit vals (max 255)
            //  CvInvoke.Normalize(img.Mat, img.Mat, 0, 255, NormType.MinMax, DepthType.Cv8U);
            infraredFrame.Mat.ConvertTo(infraredFrame, DepthType.Cv16U);

            // convert back to 8 bit for showing as a bitmap
            thresholdImg.Mat.ConvertTo(thresholdImg, DepthType.Cv8U);

            // perform opening 
            int kernelSize = Properties.UserSettings.Default.kernelSize;
            Mat kernel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(kernelSize, kernelSize), new System.Drawing.Point(-1, -1));
            thresholdImg = thresholdImg.MorphologyEx(MorphOp.Dilate, kernel2, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(1.0));

            // find controids of reflective surfaces and mark them on the image 
            TrackedData(thresholdImg);





            // only generate writeable bitmap if the mainwindow is shown
            if (this.showWindow)
            {

                // copy the processed image back into a writeable bitmap and dispose the EMGU image
                if (thresholdedClicked)
                {
                    DrawTrackedData(thresholdImg);
                    SetThresholdedInfraredImage(thresholdImg, infraredFrameDimension);

                }
                else
                {
                    DrawTrackedData(infraredFrame);
                    SetInfraredImage(infraredFrame, infraredFrameDimension);
                }
            }

            // cleanup
            thresholdImg.Dispose();
            infraredFrame.Dispose();
        }

        private void DrawTrackedData(Image<Gray, UInt16> infraredImage)
        {

            int thickness = Properties.UserSettings.Default.DataIndicatorThickness;
            int colorcode = Properties.Settings.Default.DataIndicatorColor;
            int padding = Properties.Settings.Default.DataIndicatorPadding;

            for (int i = 0; i < prevPoints.Length; i++)
            {
                int width = pointInfo[i].Width;
                int height = pointInfo[i].Height;
                Rectangle rect = new Rectangle((int)prevPoints[i][0] - (width / 2) - padding, (int)prevPoints[i][1] - (height / 2) - padding, width + padding * 2, height + padding * 2);
                CvInvoke.Rectangle(infraredImage, rect, new Gray(colorcode).MCvScalar, thickness); // 2 pixel box thick

            }

        }

        private void DrawTrackedData(Image<Gray, Byte> ThresholdedInfraredImage)
        {

            int thickness = Properties.UserSettings.Default.DataIndicatorThickness;
            int colorcode = Properties.Settings.Default.DataIndicatorColor8bit;
            int padding = Properties.Settings.Default.DataIndicatorPadding;

            for (int i = 0; i < prevPoints.Length; i++)
            {
                int width = pointInfo[i].Width;
                int height = pointInfo[i].Height;
                Rectangle rect = new Rectangle((int)prevPoints[i][0] - (width / 2) - padding, (int)prevPoints[i][1] - (height / 2) - padding, width + padding * 2, height + padding * 2);
                CvInvoke.Rectangle(ThresholdedInfraredImage, rect, new Gray(colorcode).MCvScalar, thickness); // 2 pixel box thick

            }

        }


        /// <summary>
        /// Receives availibility updates from the KinectData object and shows it in the MainWindow.XAML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="IsAvailable"></param>
        private void KinectData_ChangeStatusText(object sender, bool IsAvailable)
        {


            String statusText = "Kinect Status: ";
            statusText += IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
            if (showWindow)
            {
                // set the status text in the mainwindow
                mainWindow.StatusText = statusText;
            }
           

            Console.WriteLine(statusText);
        }


    }
}
