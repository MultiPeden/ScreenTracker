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
using Accord.Math.Optimization;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    [CLSCompliant(false)]
    /// <summary>
    /// Class for receiving frames from a camera via. an eventlistener.
    /// The frames are processed and reflective markers are tracked.
    /// If the MainWindow is present ImageProcessing updates the images in the window.
    /// </summary>
    public class ImageProcessing
    {

        /// <summary>
        /// Copies memory from src to dest
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="src"></param>
        /// <param name="count"></param>
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
        private PointInfoRelation[] pointInfo;

        /// <summary>
        /// Holds a reference to the camera
        /// </summary>
        private ICameraInterface cameraData;

        /// <summary>
        /// Holds a reference to to mainWindow if it is set visible
        /// </summary>
        private MainWindow mainWindow;

        /// <summary>
        ///  Constructor for the ImageProcessing class
        /// </summary>
        /// <param name="cameraData"></param>
        /// <param name="mainWindow"></param>
        public ImageProcessing(ICameraInterface cameraData, MainWindow mainWindow)
        {
            // only genrate colorimage if the mainwindow is present and the color option is clicked
            if (mainWindow != null && mainWindow.colorClicked)
            {
                cameraData.GenerateColorImage(true);
            }

            this.mainWindow = mainWindow;
            this.cameraData = cameraData;

            // create UDPsender object responsible outgoing data(via UDP socket)
            udpSender = new UDPsender();

            // create TCPserv object responsible ingoing commands(via TCP socket)
            commands = new TCPserv(this);

            // create Thread for running the TCPserv and start it
            TCPthread = new Thread(commands.StartListening);
            TCPthread.Start();


            // listen for images from the Camera
            cameraData.emguArgsProcessed += KinectData_EmguImageReceived;

            // listen for status changes from the camera object's kinectSensor
            cameraData.ChangeStatusText += KinectData_ChangeStatusText;

            // get handle to Kinectdata
            this.cameraData = cameraData;

            // show the window
            showWindow = true;

        }


        /// <summary>
        /// TODO Deconstructor
        /// </summary>
        ~ImageProcessing()
        {
            Stop_ImageProcessing();
        }




        /// <summary>
        /// Recives processes frames from the CameraData object, processes the frames, track 
        /// reflective markers, and if the Mainwindow is present, show them in the MainWindow.XAML
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

            // only show images if mainwindow is present
            if (mainWindow != null)
            {
                // check which button is clicked and show the corresponding image
                if (mainWindow.colorClicked)
                {
                    this.SetColorImage(e.Colorimage, e.ColorFrameDimension);
                }
                else
                {
                    this.SetDepthImage(e.DepthImage, e.DepthFrameDimension);
                }
            }

            e.Colorimage.Dispose();
            e.DepthImage.Dispose();
            e.InfraredImage.Dispose();


        }

        /// <summary>
        /// Setter for bool deciding if the color image 
        /// </summary>
        /// <param name="enable"></param>
        public void GenerateColorImage(bool enable)
        {
            cameraData.GenerateColorImage(enable);
        }

        /// <summary>
        ///  Sets the colorimage in the MainWindow
        /// </summary>
        /// <param name="img"></param>
        /// <param name="frameDimension"></param>
        private void SetColorImage(Image<Bgr, UInt16> img, FrameDimension frameDimension)
        {

            if (this.colorBitmap == null)
            {
                this.colorBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            }

            // Convert to writablebitmao
            AddToBitmap(this.colorBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 4));
            // Send to Mainwindow
            mainWindow.SetRightImage(this.colorBitmap);
        }

        /// <summary>
        /// Sets the Infrared in the MainWindow
        /// </summary>
        /// <param name="img"></param>
        /// <param name="frameDimension"></param>
        private void SetInfraredImage(Image<Gray, UInt16> img, FrameDimension frameDimension)
        {
            if (this.infraredBitmap == null)
            {
                this.infraredBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            }
            // Convert to writablebitmao
            AddToBitmap(this.infraredBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 2));
            // Send to Mainwindow
            mainWindow.SetLeftImage(this.infraredBitmap);

        }


        /// <summary>
        /// Sets the Infrared in the MainWindow
        /// </summary>
        /// <param name="img"></param>
        /// <param name="frameDimension"></param>
        private void SetInfraredImage(Image<Bgr, Byte> img, FrameDimension frameDimension)
        {
            if (this.infraredBitmap == null)
            {
                this.infraredBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Bgr24, null);
            }
            // Convert to writablebitmao
            AddToBitmap(this.infraredBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 3));
            // Send to Mainwindow
            mainWindow.SetLeftImage(this.infraredBitmap);

        }

        /// <summary>
        /// Sets the Thresholded image in the MainWindow
        /// </summary>
        /// <param name="img"></param>
        /// <param name="frameDimension"></param>
        private void SetThresholdedInfraredImage(Image<Gray, Byte> img, FrameDimension frameDimension)
        {
            if (this.infraredThesholdedBitmap == null)
            {
                this.infraredThesholdedBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            }
            // Convert to writablebitmao
            AddToBitmap(this.infraredThesholdedBitmap, img.Mat, (frameDimension.Width * frameDimension.Height));
            // Send to Mainwindow
            mainWindow.SetLeftImage(this.infraredThesholdedBitmap);

        }

        /// <summary>
        ///  Sets the Depth image in the MainWindow
        /// </summary>
        /// <param name="img"></param>
        /// <param name="frameDimension"></param>
        private void SetDepthImage(Image<Gray, UInt16> img, FrameDimension frameDimension)
        {
            if (this.depthBitmap == null)
            {
                this.depthBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            }
            // Normalize for better visibility
            CvInvoke.Normalize(img.Mat, img.Mat, 0, 65535, NormType.MinMax);
            // Convert to  writablebitmao
            AddToBitmap(this.depthBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 2));
            // Send to Mainwindow
            mainWindow.SetRightImage(this.depthBitmap);

        }



        /// <summary>
        /// The z-coordinate from the depth-camera is almost always 0 for centroids because it can not 
        /// measure the depth of reflektive surfacres, thus we have to estimate the depth using the 
        /// surrounding pixels. We use the Media of the cardinal and inter-cardinal points as the estimate
        /// </summary>
        /// <param name="depthFrameData"></param>
        /// <returns> The estimated z-coodinates </returns>
        private unsafe ushort[] GetZCoordinatesSurroundingBox(Image<Gray, UInt16> depthImage)
        {

            // init array to the size of the array with tracked points
            ushort[] zCoordinates = new ushort[this.prevPoints.Length];



            // list for each point's depth sample points
            List<double> zCoords;

            // find the cardinal and inter-candinal points and their values
            // to the list zCoords.
            for (int i = 0; i < prevPoints.Length; i++)
            {
                zCoords = new List<double>();
                PointInfoRelation p = pointInfo[i];

                int x = (int)prevPoints[i][0];
                int y = (int)prevPoints[i][1];

                // We want to go half the with and hight(+ padding) 
                // in each direction to get the estimate
                int width = (p.Width / 2) + 1;
                int height = (p.Height / 2) + 1;

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

                // if we did not find any valid estimate the z-coordinate is set to 0
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

        /// <summary>
        /// Finds the z-coordinate in the depthImage using the x and y-coordinates
        /// and adds it to zCoords.
        /// Does not add anything if x or is outside the bounds, or if the z-val is 0.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="depthImage"></param>
        /// <param name="zCoords"></param>
        private void AddDephtPixel(int x, int y, ref Image<Gray, UInt16> depthImage, ref List<double> zCoords)
        {
            if (x >= 0 && x < depthImage.Width && y >= 0 && y < depthImage.Height)
            {
                ushort zCoord = depthImage.Data[y, x, 0];
                if (zCoord > 0)
                {
                    zCoords.Add(zCoord);
                }
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




        private double GetAvgY(double[][] centroidPoints, int from, int to)
        {
            if (from == to)
            {
                return centroidPoints[from][1];
            }

            int numbElem = 0;
            double acc = 0;
            for (int i = from; i < to; i++)
            {
                acc = +centroidPoints[i][1];
                numbElem++;
            }
            return acc / numbElem;
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

            //int minArea = Properties.UserSettings.Default.DataIndicatorMinimumArea;

            // Get Connected component in the frame
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            MCvPoint2D64f[] centroidPointsEmgu;
            int n;
            n = CvInvoke.ConnectedComponentsWithStats(thresholdImg, labels, stats, centroids, LineType.EightConnected, DepthType.Cv16U);

            // Copy centroid points to point array
            centroidPointsEmgu = new MCvPoint2D64f[n];
            centroids.CopyTo(centroidPointsEmgu);

            // Convert to jagged array
            double[][] centroidPoints = PointArrayToJaggedArray(centroidPointsEmgu, n);




            // Jagged array for new points
            double[][] newPoints;
            // index for array
            int index;



            int i = 0;
            // if we have no previous points we add the conneted components as the tracked points
            if (prevPoints == null)
            {
             
                

                /*
                Array.Sort(centroidPoints,
                    (left, right) => left[1].CompareTo(right[1]) != 0
                                ? left[1].CompareTo(right[1])
                                : left[0].CompareTo(right[0]));
                                */


                int cols = Properties.UserSettings.Default.GridColums;
                int rows = Properties.UserSettings.Default.GridRows;

                double[][] orderedCentroidPoints = new double[rows*cols][];
                pointInfo = new PointInfoRelation[rows * cols];

                Array.Sort(centroidPoints, (left, right) => left[1].CompareTo(right[1]));

                int count = 0;

                for (int k = 0; k < rows; k++)
                {



                    double[][] colArray = centroidPoints.Skip(k * cols).Take(cols).ToArray();
                    Array.Sort(colArray, (left, right) => left[0].CompareTo(right[0]));

                    foreach (double[] p in colArray)
                    {
                        orderedCentroidPoints[count] = p;
                        count++;
                    }



                }
                newPoints = orderedCentroidPoints;
                //Console.WriteLine("cat: " + cat);




                // initialize points
                foreach (double[] point in orderedCentroidPoints)
                {
                    int j = i + 1;
                    int width = stats.GetData(j, 2)[0];
                    int height = stats.GetData(j, 3)[0];
                    int area = stats.GetData(j, 4)[0];
                    // set info for each point, used later to get z-coordinate
                    pointInfo[i] = new PointInfoRelation(width, height,i);
                    i++;
                    Console.WriteLine("X: " + point[0] + " Y: " + point[1]);
                }

                // assign kardinal points to pointInfo
                for (int k = 0; k < pointInfo.Length; k++)
                {
                    pointInfo[k].AssignCardinalPoints(pointInfo, k, cols, rows);
                }




            }
            else
            { // If we have previous points, we search for their nearest neighbours in the new frame.

                // copy previous points to new point to avoid loosing any points
                // newPoints = prevPoints;
                 double[][] newPointsSparse = new double[prevPoints.Length][];


                if(prevPoints.Length != centroidPoints.Length )
                {
                 
                }

                // build KD-tree for nearest neighbour search
                //      KDTree<int> tree = KDTree.FromData<int>(prevPoints, Enumerable.Range(0, prevPoints.Length).ToArray());



                /*
                if (prevPoints.Length <= centroidPoints.Length)
                {
                    costMatrix = IRUtils.GetCostMatrix2(prevPoints, centroidPoints);
                }
                else
                {
                     costMatrix = IRUtils.GetCostMatrix(prevPoints, centroidPoints);
                }
                */

                //  costMatrix = IRUtils.GetCostMatrix3(prevPoints, centroidPoints);



                // int[,] costMatrix = IRUtils.GetCostMatrixArray(prevPoints, centroidPoints);




                    int[,] costMatrix = IRUtils.GetCostMatrixArray(centroidPoints, prevPoints);

                Hungarian hung = new Hungarian(costMatrix);
                int[,] M = hung.M;
              //  hung.ShowCostMatrix();
              //  hung.ShowMaskMatrix();
                // Create a new Hungarian algorithm
                // Munkres m = new Munkres(costMatrix);
                int[] minInd = hung.GetMinimizedIndicies();

                /*
                Console.Write("\n");
                foreach (int mini in minInd)
                {
                    Console.Write(mini + " "); 
                }
                */

                if (prevPoints.Length != centroidPoints.Length)
                {

                }

                double[][] rearranged = IRUtils.RearrangeArray2(centroidPoints, minInd, prevPoints.Length);



                //Update points
                foreach (double[] point in rearranged)
                {
                  
                   // index = minInd[i] +1;
                    if (point != null)
                    {
                        index = minInd[i] + 1;

                      //  ArrangedPoints[indices[i]] = points[i];


                        int width = stats.GetData(index, 2)[0];
                        int height = stats.GetData(index , 3)[0];
                        int area = stats.GetData(index, 4)[0];

                        // if the area is more than minArea, discard 
                        if (true) // (area > minArea)
                        {

                            // find nearest neighbour
                            //         KDTreeNode<int> nearest = tree.Nearest(point);



                            // get its index
                            //   index = nearest.Value;

                            // update info for the point
                            PointInfoRelation pInfo = pointInfo[i];
                            pInfo.Width = width;
                            pInfo.Height = height;
                            pInfo.Visible = true;
                            newPointsSparse[i] = point;
                        }
                    }

                    i++;
                }
                newPoints = newPointsSparse;



                for (int k = 0; k < newPointsSparse.Length; k++)
                {
                    if (newPoints[k] == null)
                    {
                        newPoints[k] = pointInfo[k].EstimatePostition(newPointsSparse);
                        pointInfo[k].Visible = false;
                    }

                }

   


            }



            // Update the previous points
            prevPoints = newPoints;
        }

        /// <summary>
        ///  Converts from EMGUs MCvPoint2D64f array to a jagged double array
        /// </summary>
        /// <param name="points"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private double[][] PointArrayToJaggedArray(MCvPoint2D64f[] points, int n)
        {
            double[][] centroidPoints2 = new double[n - 1][];
            int i = 0;
            foreach (MCvPoint2D64f point in points)
            {
                if (i > 0)
                {
                    centroidPoints2[i - 1] = new double[2] { point.X, point.Y };
                }
                i++;
            }

            return centroidPoints2;
        }


        /// <summary>
        /// Converts a list of points to Json and sends it via UDP socket
        /// </summary>
        /// <param name="newPoints"></param>
        private void SendPoints(double[][] newPoints, ushort[] zCoordinates)
        {
            // Convert to world-coordinates
            double[][] worldCoordinates = cameraData.ScreenToWorldCoordinates(newPoints, zCoordinates);
            // Convert to Json
            String jSon = IRUtils.PointstoJson(worldCoordinates, zCoordinates);
            // Send to socket via.
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

        /// <summary>
        /// TODO
        /// Stops the processes in ImageProcessing 
        /// </summary>
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


        /// <summary> 
        /// Processes the infrared frame:
        /// 1. Thesholds the infrared image
        /// 2. Opens the thesholded image
        /// 3. Tracks refletive markers in the thresholded image.
        /// 4. Show infrared/thresholded image if mainwindow is present 
        /// </summary>
        /// <param name="infraredFrame"> the InfraredFrame image </param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private void ProcessInfraredFrame(Image<Gray, UInt16> infraredFrame, FrameDimension infraredFrameDimension)
        {
            // init threshold image variable
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

                    Image<Bgr, Byte> colImg = DrawTrackedData(infraredFrame);
                    SetInfraredImage(colImg, infraredFrameDimension);
                    colImg.Dispose();
                }
            }

            // cleanup
            thresholdImg.Dispose();
            infraredFrame.Dispose();
        }

        /// <summary>
        /// Draws tracked data on a Uint16 infraredImage
        /// </summary>
        /// <param name="infraredImage"></param>
        private Image<Bgr, Byte> DrawTrackedData(Image<Gray, UInt16> infraredImage)
        {

            int thickness = Properties.UserSettings.Default.DataIndicatorThickness;
            int colorcode = Properties.Settings.Default.DataIndicatorColor;
            int padding = Properties.Settings.Default.DataIndicatorPadding;

          //  Image<Bgr, ushort> colImg = new Image<Bgr, Byte>(infraredImage.Width, infraredImage.Height);

            //  CvInvoke.CvtColor(infraredImage, colImg, ColorConversion.Gray2Bgr,3);

            Image<Bgr, Byte> colImg = infraredImage.Convert<Bgr, Byte>();


            for (int i = 0; i < prevPoints.Length; i++)
            {
                int width = pointInfo[i].Width;
                int height = pointInfo[i].Height;
                Rectangle rect = new Rectangle((int)prevPoints[i][0] - (width / 2) - padding, (int)prevPoints[i][1] - (height / 2) - padding, width + padding * 2, height + padding * 2);
                /*
                CvInvoke.Rectangle(infraredImage, rect, new Gray(colorcode).MCvScalar, thickness); // 2 pixel box thick

                CvInvoke.PutText(infraredImage,
                                i.ToString(),
                                new System.Drawing.Point((int)prevPoints[i][0], (int)prevPoints[i][1]),
                                FontFace.HersheyComplex,
                                1.0,
                                new Gray(colorcode).MCvScalar);
*/
                if (pointInfo[i].Visible)
                {
                    CvInvoke.Rectangle(colImg, rect, new Bgr(0, 255, 0).MCvScalar, thickness); // 2 pixel box thick
                }
                else
                {
                    CvInvoke.Rectangle(colImg, rect, new Bgr(0, 0, 255).MCvScalar, thickness); // 2 pixel box thick
                }

                CvInvoke.PutText(colImg,
                i.ToString(),
                new System.Drawing.Point((int)prevPoints[i][0] -width, (int)prevPoints[i][1] + height),
                FontFace.HersheyComplex,
                .6,
                new Bgr(255, 255, 0).MCvScalar);

            }
            return colImg;

        }

        /// <summary>
        /// Draws tracked data on a Byte infraredImage
        /// </summary>
        /// <param name="ThresholdedInfraredImage"></param>
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

        /// <summary>
        /// row-major order
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
        /// <param name="numberOfColumns"></param>
        /// <returns></returns>
        private int From2Dto1D(int rowIndex, int columnIndex, int numberOfColumns)
        {
            return rowIndex * numberOfColumns + columnIndex;
        }


    }
}
