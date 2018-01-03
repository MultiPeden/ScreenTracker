using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
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
        /// EventHandler for passing on the Kinects availibility status on
        /// </summary>
        public event EventHandler<bool> ChangeStatusText;

        /// <summary>
        /// Bool indicating if z coordinates should be calculated
        /// </summary>
        private bool withZCoodinates;

        /// <summary>
        /// EventHandler for sending events when a frame has been processed
        /// </summary>
        public event EventHandler<FrameProcessedEventArgs> framesProcessed;



        private PointInfo[] pointInfo;

        private ICameraInterface cameraData;

        private MainWindow mainWindow;


        public ImageProcessing(ICameraInterface cameraData,  MainWindow mainWindow)
        {

            if(mainWindow != null && mainWindow.colorClicked)
            {
                
                cameraData.GenerateColorImage(true);
            }


            this.mainWindow = mainWindow;

            this.cameraData = cameraData;

            /// Bool indicating if z coordinates should be calculated
            this.withZCoodinates = true;

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

            // get handle to Kinectdata
            this.cameraData = cameraData;

            /* TODO
            // set the status text
            StatusText = cameraData.SensorAvailable() ? Properties.Resources.RunningStatusText
                                                             : Properties.Resources.NoSensorStatusText;
                                                             */




            /*
            // Initialize the four bitmaps for processed frames
            this.infraredBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            this.infraredThesholdedBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            this.colorBitmap = new WriteableBitmap(this.colorFrameDescription.Width, this.colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            */



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
            Console.WriteLine("godaw");

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

                if (mainWindow.thresholdedClicked)
                {

                }
                else
                {
                    this.SetInfraredImage(e.InfraredImage, e.InfraredFrameDimension);
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

   
            AddToBitmap(this.colorBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 4 ));

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


        private void SetDepthImage(Image<Gray, UInt16> img, FrameDimension frameDimension)
        {
            if (this.depthBitmap == null)
            {
                this.depthBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            }

          ///  img = img.Mul(10);


             CvInvoke.Normalize(img.Mat, img.Mat,0, 65535, NormType.MinMax);
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
        private unsafe ushort[] GetZCoordinatesStep(IntPtr depthFrameData, FrameDimension depthFrameDimension)
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
                    ushort index = (ushort)(depthFrameDimension.Width * x + y);
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


        /*

        /// <summary>
        /// The z-coordinate from the depth-camera is almost always 0 for centroids because it can not 
        /// measure the depth of reflektive surfacres, thus we have to estimate the depth using the 
        /// surrounding pixels.
        /// </summary>
        /// <param name="depthFrameData"></param>
        /// <returns></returns>
        private unsafe ushort[] GetZCoordinatesSurroundingBox(IntPtr depthFrameData, FrameDimension depthFrameDimension)
        {

            // init array to the size of the array with tracked points
            ushort[] zCoordinates = new ushort[this.prevPoints.Length];

            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            double imgwidth = infraredBitmap.Width;

            // list for each point's depth sample points
            List<double> zCoords;

            for (int i = 0; i < prevPoints.Length; i++)
            {
                zCoords = new List<double>();
                PointInfo p = pointInfo[i];

                double x = Math.Round(prevPoints[i][0]);
                double y = Math.Round(prevPoints[i][1]);
                int width = (p.Width / 2) + 1;
                //  width = +5;
                int height = (p.Height / 2) + 1;
                // height = +5;
                int frameWidth = depthFrameDimension.Width;


                // add cardinal points (N,E,S,W)
                AddDephtPixel(x + width, y, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x - width, y, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x, y + height, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x, y - height, imgwidth, ref frameData, ref zCoords);

                // add inter-cardinal points (NE,SE,SW,NW)
                AddDephtPixel(x + width, y + height, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x - width, y - height, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x + width, y - height, imgwidth, ref frameData, ref zCoords);
                AddDephtPixel(x - width, y + height, imgwidth, ref frameData, ref zCoords);

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

            /*

        /*
         *  draw surrounding box for z pixels
        private unsafe void AddDephtPixel(double x, double y, double width, ref ushort* frameData, ref List<double> zCoords)
        {
            try
            {

                zCoords.Add(frameData[(int)(width * y + x)]);

                // draw dot where we mesure the z-coordinate 
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
        */

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


        /*
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



            int i = 0;
            foreach (MCvPoint2D64f point in centroidPoints)
            {
                if (i > 0)
                {
                    centroidPoints2[i - 1] = new double[2] { point.X, point.Y };
                }
                i++;
            }


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


                List<int> indexList = new List<int>();

                foreach (double[] point in centroidPoints2)
                {
                    int j = i + 1;
                    //  int cx = stats.GetData(i, 0)[0];
                    //  int cy = stats.GetData(i, 1)[0];
                    int width = stats.GetData(j, 2)[0];
                    int height = stats.GetData(j, 3)[0];
                    int area = stats.GetData(j, 4)[0];

                    int[] indexArray;

                    // if the area is more than minArea, discard 
                    if (true) // (area > minArea)
                    {
                        Rectangle rect = new Rectangle((int)point[0] - (width / 2) - padding, (int)point[1] - (height / 2) - padding, width + padding * 2, height + padding * 2);

                        CvInvoke.Rectangle(img, rect, new Gray(colorcode).MCvScalar, thickness);
                        //if (i==0)
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
            return img;
        }

*/


/*

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
*/

            /*
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


                newPointsTest[i] = new double[2] { lutValue.X * 1000, lutValue.Y * 1000 };
            }

            return newPointsTest;

        }
        */

            /*
        private double[][] ConvertPoints2(double[][] newPoints, ushort[] zCoordinates)
        {
            CameraSpacePoint cameraPoint;
            DepthSpacePoint depthPoint;
            CoordinateMapper mapper = kinectSensor.CoordinateMapper;
            double[][] newPointsTest = new double[newPoints.Length][];

            for (int i = 0; i < newPoints.Length; i++)
            {

        
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

    */

            /*

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
        */


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


    }
}
