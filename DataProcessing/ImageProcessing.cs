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
using System.Diagnostics;
using ScreenTracker.GUI;
using ScreenTracker.Communication;
using ScreenTracker.DataReceiver;

namespace ScreenTracker.DataProcessing
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
        private WriteableBitmap colorThesholdedBitmap = null;

        private WriteableBitmap infraredBitmap = null;
        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap depthBitmap = null;




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


        private IScreen screen;

        /// <summary>
        /// Holds a reference to the camera
        /// </summary>
        private ICamera cameraData;

        /// <summary>
        /// Holds a reference to to mainWindow if it is set visible
        /// </summary>
        private MainWindow mainWindow;

        private bool firstDetected = true;


        double[] missingx = new double[] { 0, 0 };


        /// <summary>
        ///  Constructor for the ImageProcessing class
        /// </summary>
        /// <param name="cameraData"></param>
        /// <param name="mainWindow"></param>
        public ImageProcessing(ICamera cameraData, MainWindow mainWindow)
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
            cameraData.EmguArgsProcessed += KinectData_EmguImageReceived;

            // listen for status changes from the camera object's kinectSensor
            cameraData.ChangeStatusText += KinectData_ChangeStatusText;

            // get handle to Kinectdata
            this.cameraData = cameraData;

            // show the window
            showWindow = true;

            this.screen = new DisplacementScreen();




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

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Process infrared image and track points
            //  this.ProcessInfraredFrame(e.InfraredImage, e.InfraredFrameDimension);
            this.ProcessRGBFrame(e.Colorimage, e.ColorFrameDimension);


            // get z-coordinates
            ushort[] zCoordinates = this.GetZCoordinatesSurroundingBox(e.DepthImage);
            // Send point via UDP
            SendPoints(screen.PrevPoints, zCoordinates);

            stopwatch.Stop();
            //  Console.WriteLine(stopwatch.ElapsedMilliseconds);

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
        private void SetColorImage(Mat img, FrameDimension frameDimension)
        {

            if (this.colorBitmap == null)
            {
                this.colorBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            }

            // Convert to writablebitmao
            AddToBitmap(this.colorBitmap, img, (frameDimension.Width * frameDimension.Height * 4));





            // Send to Mainwindow
            mainWindow.SetRightImage(this.colorBitmap);
        }

        /// <summary>
        /// Sets the Infrared in the MainWindow
        /// </summary>
        /// <param name="img"></param>
        /// <param name="frameDimension"></param>
        private void SetInfraredImage(Mat img, FrameDimension frameDimension)
        {
            if (this.infraredBitmap == null)
            {
                this.infraredBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Bgr24, null);
            }
            // Convert to writablebitmao
            AddToBitmap(this.infraredBitmap, img, (frameDimension.Width * frameDimension.Height * 3));
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
        private void SetThresholdedInfraredImage(Mat img, FrameDimension frameDimension)
        {
            if (this.colorThesholdedBitmap == null)
            {




                this.colorThesholdedBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            }

            // Convert to writablebitmao
            AddToBitmap(this.colorThesholdedBitmap, img, (frameDimension.Width * frameDimension.Height));
            // Send to Mainwindow
            mainWindow.SetLeftImage(this.colorThesholdedBitmap);

        }


        private void SetThresholdedInfraredImage(Image<Gray, Byte> img, FrameDimension frameDimension)
        {
            if (this.colorThesholdedBitmap == null)
            {

                this.colorThesholdedBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            }

            // Convert to writablebitmao
            AddToBitmap(this.colorThesholdedBitmap, img.Mat, (frameDimension.Width * frameDimension.Height));
            // Send to Mainwindow
            mainWindow.SetLeftImage(this.colorThesholdedBitmap);

        }


        /// <summary>
        /// Sets the Thresholded image in the MainWindow
        /// </summary>
        /// <param name="img"></param>
        /// <param name="frameDimension"></param>
        private void SetThresholdedcolorImage(Image<Bgr, ushort> img, FrameDimension frameDimension)
        {
            if (this.infraredThesholdedBitmap == null)
            {
                this.infraredThesholdedBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            }
            // Convert to writablebitmao
            AddToBitmap(this.infraredThesholdedBitmap, img.Mat, (frameDimension.Width * frameDimension.Height * 4));
            // Send to Mainwindow
            mainWindow.SetLeftImage(this.infraredThesholdedBitmap);

        }

        /// <summary>
        ///  Sets the Depth image in the MainWindow
        /// </summary>
        /// <param name="img"></param>
        /// <param name="frameDimension"></param>
        private void SetDepthImage(Mat img, FrameDimension frameDimension)
        {
            if (this.depthBitmap == null)
            {
                this.depthBitmap = new WriteableBitmap(frameDimension.Width, frameDimension.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            }
            // Normalize for better visibility
            CvInvoke.Normalize(img, img, 0, 65535, NormType.MinMax);
            // Convert to  writablebitmao
            AddToBitmap(this.depthBitmap, img, (frameDimension.Width * frameDimension.Height * 2));
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
        private unsafe ushort[] GetZCoordinatesSurroundingBox(Mat depthImage)
        {
            if (screen.PrevPoints != null)
            {
                // init array to the size of the array with tracked points
                ushort[] zCoordinates = new ushort[this.screen.PrevPoints.Length];



                // list for each point's depth sample points
                List<double> zCoords;

                // find the cardinal and inter-candinal points and their values
                // to the list zCoords.
                for (int i = 0; i < screen.PrevPoints.Length; i++)
                {
                    zCoords = new List<double>();
                    PointInfo p = screen.PointInfo[i];

                    int x = (int)screen.PrevPoints[i][0];
                    int y = (int)screen.PrevPoints[i][1];

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
            else
            {
                return null;
            }

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
        private void AddDephtPixel(int x, int y, ref Mat depthImage, ref List<double> zCoords)
        {
            if (x >= 0 && x < depthImage.Width && y >= 0 && y < depthImage.Height)
            {

                /// todo check correct z vals
                // ushort zCoord = (ushort) depthImage.GetData(x, y)[0];//.Data[y, x, 0];
                ushort zCoord = 5;


                if (zCoord > 0)
                {
                    zCoords.Add(zCoord);
                }
            }

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
        private void TrackedData(Mat thresholdImg)
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
            if (screen.PrevPoints == null)
            {



                int cols = Properties.UserSettings.Default.GridColums;
                int rows = Properties.UserSettings.Default.GridRows;

                if (rows * cols != n - 1)
                {
                    mainWindow.StatusText = "Unable to detect a r: " + rows + " c: " + cols + " grid in the image";

                    return;
                }

                double[][] orderedCentroidPoints = new double[rows * cols][];



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


                screen.Initialize(newPoints, stats);


            }
            else
            { // If we have previous points, we search for their nearest neighbours in the new frame.

                // copy previous points to new point to avoid loosing any points
                // newPoints = prevPoints;
                newPoints = new double[screen.PrevPoints.Length][];






                if (firstDetected)
                {
                    int cols = Properties.UserSettings.Default.GridColums;
                    int rows = Properties.UserSettings.Default.GridRows;

                    mainWindow.StatusText = "Detected a r: " + rows + " c: " + cols + " grid in the image";

                    firstDetected = false;
                }


                // build KD-tree for nearest neighbour search
                //      KDTree<int> tree = KDTree.FromData<int>(prevPoints, Enumerable.Range(0, prevPoints.Length).ToArray());

                // Use Hungarian algorithm to find points from the old frame, in the new frame
                int[] minInd = GetPointsIndices(centroidPoints);

                double[][] rearranged = IRUtils.RearrangeArray2(centroidPoints, minInd, screen.PrevPoints.Length);



                //Update points
                foreach (double[] point in rearranged)
                {

                    if (point != null)
                    {
                        index = minInd[i] + 1;

                        int width = stats.GetData(index, 2)[0];
                        int height = stats.GetData(index, 3)[0];
                        int area = stats.GetData(index, 4)[0];

                        // if the area is more than minArea, discard 
                        if (true) // (area > minArea)
                        {
                            PointInfo pInfo = screen.PointInfo[i];
                            pInfo.Width = width;
                            pInfo.Height = height;
                            pInfo.Visible = true;
                            newPoints[i] = point;
                        }
                    }

                    i++;
                }


                PointInfoDisplacement sprinInfo = (PointInfoDisplacement)screen.PointInfo[12];
                if (sprinInfo.Visible)
                {
                    screen.UpdateScreen(newPoints);

                    double[] estimatedPos = sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1);
                    double[] orgPost = sprinInfo.orignalPos;

                    double estimateDiffx = estimatedPos[0] - orgPost[0];
                    double aactualDiffx = screen.PrevPoints[12][0] - orgPost[0];

                    //   double estimateDiffy = estimatedPos[1] - orgPost[1];
                    //   double aactualDiffy = screen.PrevPoints[12][1] - orgPost[1];


                    Console.Write(estimateDiffx + ",");

                    Console.WriteLine(aactualDiffx + ",");

                    //    Console.Write(estimateDiffy + ",");

                    //  Console.WriteLine(aactualDiffy);

                    /*
                                        Console.Write(sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1)[0] + ",");
                                        Console.Write(sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1)[1] + ",");
                                        Console.Write(screen.PrevPoints[12][0] + ",");
                                        Console.WriteLine(screen.PrevPoints[12][0]);
                                        */

                    missingx = new double[]
                    {
                        sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1)[0],
                        sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1)[1]
                    };

                }

                //do estimation using the displacement model
                ///                DisplacementEstimation(double[][] newPointsSparse, double[][] newPoints)

            }


        }

        /// <summary>
        /// Uses the Hungarian algorithm to recognize points from the old frame, in the new frame.
        /// </summary>
        /// <param name="centroidPoints"></param>
        /// <returns></returns>
        private int[] GetPointsIndices(double[][] centroidPoints)
        {
            int[,] costMatrix = IRUtils.GetCostMatrixArray(centroidPoints, screen.PrevPoints);

            Hungarian hung = new Hungarian(costMatrix);
            int[,] M = hung.M;
            // hung.ShowCostMatrix();
            //  hung.ShowMaskMatrix();
            // Create a new Hungarian algorithm
            // Munkres m = new Munkres(costMatrix);
            return hung.GetMinimizedIndicies();

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
            if (jSon != null)
            {
                // Send to socket via.
                udpSender.WriteToSocket(jSon);
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
            screen.PrevPoints = null;
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
        private void ProcessInfraredFrame(Mat infraredFrame, FrameDimension infraredFrameDimension)
        {
            // init threshold image variable
            //            Image<Gray, Byte> thresholdImg = new Image<Gray, Byte>(infraredFrameDimension.Width, infraredFrameDimension.Height);

            Mat thresholdImg = new Mat(infraredFrameDimension.Width, infraredFrameDimension.Height, DepthType.Cv8U, 1);

            // nessesary for calling  CvInvoke.Threshold because it only supports 8 and 32-bit datatypes  
            infraredFrame.ConvertTo(infraredFrame, DepthType.Cv32F);



            // find max val of the 16 bit ir-image
            infraredFrame.MinMax(out _, out double[] maxVal, out _, out _);

            // apply threshold with 98% of maxval || minThreshold
            // to obtain binary image with only 0's & 255
            float percentageThreshold = Properties.UserSettings.Default.PercentageThreshold;
            int minThreshold = Properties.UserSettings.Default.minThreshold;

            CvInvoke.Threshold(infraredFrame, thresholdImg, Math.Max(maxVal[0] * percentageThreshold, minThreshold), 255, ThresholdType.Binary);


            //  CvInvoke.Threshold(infraredFrame, thresholdImg, 10000, 255, ThresholdType.Trunc);
            //     thresholdImg = infraredFrame.InRange(new Gray(5000), new Gray(5500));

            // nomalize the 16bit vals to 8bit vals (max 255)
            //  CvInvoke.Normalize(img.Mat, img.Mat, 0, 255, NormType.MinMax, DepthType.Cv8U);
            infraredFrame.ConvertTo(infraredFrame, DepthType.Cv16U);

            // convert back to 8 bit for showing as a bitmap
            thresholdImg.ConvertTo(thresholdImg, DepthType.Cv8U);

            // perform opening 
            int kernelSize = Properties.UserSettings.Default.kernelSize;
            Mat kernel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(kernelSize, kernelSize), new System.Drawing.Point(-1, -1));


            //            thresholdImg = thresholdImg.MorphologyEx(MorphOp.Dilate, kernel2, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(1.0));
            CvInvoke.MorphologyEx(thresholdImg, thresholdImg, MorphOp.Dilate, kernel2, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(1.0));


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

                    Mat colImg = DrawTrackedData(infraredFrame);
                    //  CvInvoke.Normalize(colImg, colImg, 0, 255, NormType.MinMax, DepthType.Cv8U);
                    SetInfraredImage(colImg, infraredFrameDimension);
                    colImg.Dispose();
                }
            }

            // cleanup
            thresholdImg.Dispose();
            infraredFrame.Dispose();
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
        private void ProcessRGBFrame(Mat colorFrame, FrameDimension colorFrameDimension)
        {


            // init threshold image variable
            //    Image<Gray, Byte> thresholdImg = new Image<Gray, Byte>(colorFrame.Bitmap);


            Mat grayImage = new Mat(colorFrameDimension.Height, colorFrameDimension.Width, DepthType.Cv8U, 1);

            CvInvoke.CvtColor(colorFrame, grayImage, ColorConversion.Bgr2Gray);

            Mat thresholdImg = new Mat( colorFrameDimension.Height, colorFrameDimension.Width, DepthType.Cv8U, 1);




            //  Mat hsvImg = new Mat(colorFrameDimension.Width, colorFrameDimension.Width, DepthType.Cv8U, 3);


            //  Mat[] channels  = colorFrame.Split();


            //   Mat imgprocessed = colorFrame.InRange(new Bgr(0, 0, 175),  // min filter value ( if color is greater than or equal to this)
            //                                      new Bgr(100, 100, 256)); // max filter value ( if color is less than or equal to this)

            /*
            CvInvoke.CvtColor(colorFrame, hsvImg, ColorConversion.Bgr2Hsv);


             Mat imageHSVDest = new Mat(colorFrameDimension.Width, colorFrameDimension.Width, DepthType.Cv8U, 3);

            CvInvoke.InRange(hsvImg, new ScalarArray(new MCvScalar(20, 100, 100)), new ScalarArray(new MCvScalar(20, 255, 255)), imageHSVDest);


            Mat thresholdImg2 = new Mat(colorFrameDimension.Width, colorFrameDimension.Width, DepthType.Cv8U, 1);
            Mat colorFrame2 = new Mat(colorFrameDimension.Width, colorFrameDimension.Width, DepthType.Cv8U, 3);
            
            CvInvoke.CvtColor(imageHSVDest, colorFrame2, ColorConversion.Hsv2Bgr);
            CvInvoke.CvtColor(colorFrame2, thresholdImg2, ColorConversion.Bgr2Gray);

            */

            Mat colorFrame2 = new Mat(colorFrameDimension.Height, colorFrameDimension.Width, DepthType.Cv8U, 4);
            CvInvoke.InRange(colorFrame, new ScalarArray(new MCvScalar(0, 0, 130, 255)), new ScalarArray(new MCvScalar(50, 50, 255, 255)), colorFrame2);

            //    Mat thresholdImg2 = new Mat(colorFrameDimension.Width, colorFrameDimension.Width, DepthType.Cv8U, 1);
            //CvInvoke.CvtColor(colorFrame2, thresholdImg2, ColorConversion.Bgra2Gray,1);



            int kernelSize = Properties.UserSettings.Default.kernelSize;
            // int kernelSize = 3;
            Mat kernel2 = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new System.Drawing.Size(kernelSize, kernelSize), new System.Drawing.Point(-1, -1));


            //            thresholdImg = thresholdImg.MorphologyEx(MorphOp.Dilate, kernel2, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(1.0));
            CvInvoke.MorphologyEx(colorFrame2, colorFrame2, MorphOp.Dilate, kernel2, new System.Drawing.Point(-1, -1), 5, BorderType.Default, new MCvScalar(1.0));



            Mat mask = new Mat(colorFrameDimension.Height, colorFrameDimension.Width, DepthType.Cv8U, 1);
            // Get Connected component in the frame
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            MCvPoint2D64f[] centroidPointsEmgu;
            int n;
            n = CvInvoke.ConnectedComponentsWithStats(colorFrame2, labels, stats, centroids, LineType.FourConnected, DepthType.Cv16U);

            if (n > 2)
            {
                // Copy centroid points to point array
                centroidPointsEmgu = new MCvPoint2D64f[n];
                centroids.CopyTo(centroidPointsEmgu);
                int width = 20;
                int height = 20;


                int padding = 2;

                int[] areas = new int[centroidPointsEmgu.Length - 1];


                Console.Write("Areas: ");
                for (int i = 1; i < centroidPointsEmgu.Length; i++)
                {
                    areas[i - 1] = stats.GetData(i, 4)[0];

                    Console.Write(stats.GetData(i, 4)[0] + " ");
                }
                Console.WriteLine(" ");

                int[] twoMaxArea = findNLargest(areas, 2);


                for (int i = 0; i < twoMaxArea.Length; i++)
                {
                    int index = twoMaxArea[i] + 1;
                    Rectangle rect = new Rectangle((int)centroidPointsEmgu[index].X - (width / 2) - padding, (int)centroidPointsEmgu[index].Y - (height / 2) - padding, width + padding * 2, height + padding * 2);

                    Console.Write(index + " ");
                    CvInvoke.Rectangle(colorFrame2, rect, new Gray(50).MCvScalar, 2); // 2 pixel box thick


                }

                MCvPoint2D64f p1 = centroidPointsEmgu[twoMaxArea[0] + 1];
                MCvPoint2D64f p2 = centroidPointsEmgu[twoMaxArea[1] + 1];

                Console.WriteLine("");



                Rectangle rect1 = new Rectangle((int)p1.X, (int)p1.Y, 200,200);


                CvInvoke.Rectangle(mask, rect1, new MCvScalar(255), -1); // 2 pixel box thick


                              CvInvoke.BitwiseAnd(mask, grayImage, grayImage);

              //  colorFrame2.CopyTo(colorFrame2, mask);

                SetThresholdedInfraredImage(grayImage, colorFrameDimension);
            }
            labels.Dispose();
            stats.Dispose();
            centroids.Dispose();







            

            //     this.SetColorImage(colorFrame2, colorFrameDimension);



            // find max val of the grat scaled image from the RGB cam
        //    grayImage.MinMax(out _, out double[] maxVal, out _, out _);

            // apply threshold with 98% of maxval || minThreshold
            // to obtain binary image with only 0's & 255
         //   float percentageThreshold = Properties.UserSettings.Default.PercentageThreshold;
         //   int minThreshold = Properties.UserSettings.Default.minThreshold;

            // thresholdImg = colorFrame.Convert<Gray, byte>();




       //     CvInvoke.Threshold(grayImage, thresholdImg, 40, 255, ThresholdType.BinaryInv);




            //     thresholdImg.Mat.ConvertTo(thresholdImg, DepthType.Cv16U);



            // perform opening 

            // find controids of reflective surfaces and mark them on the image 
            //  TrackedData(thresholdImg);


            //  SetThresholdedInfraredImage(thresholdImg, colorFrameDimension);

            colorFrame2.Dispose();
            // thresholdImg2.Dispose();
            grayImage.Dispose();

            thresholdImg.Dispose();
            //  colorImage.Dispose();   

        }

        private int[] findNLargest(int[] numbers, int n)
        {

            int N = numbers.Length;
            int[] index = Enumerable.Range(0, N).ToArray<int>();
            Array.Sort<int>(index, (a, b) => numbers[b].CompareTo(numbers[a]));
            return index.Take(n).ToArray();
        }


        /// <summary>
        /// Draws tracked data on a Uint16 infraredImage
        /// </summary>
        /// <param name="infraredImage"></param>
        private Mat DrawTrackedData(Mat infraredImage)
        {

            int thickness = Properties.UserSettings.Default.DataIndicatorThickness;
            int colorcode = Properties.Settings.Default.DataIndicatorColor;
            int padding = Properties.Settings.Default.DataIndicatorPadding;


            CvInvoke.Normalize(infraredImage, infraredImage, 0, 255, NormType.MinMax, DepthType.Cv8U);



            Mat colImg = new Mat(infraredImage.Width, infraredImage.Height, DepthType.Cv8U, 3);



            CvInvoke.CvtColor(infraredImage, colImg, ColorConversion.Gray2Bgr, 3);




            if (screen.PrevPoints != null)
            {


                for (int i = 0; i < screen.PrevPoints.Length; i++)
                {
                    int width = screen.PointInfo[i].Width;
                    int height = screen.PointInfo[i].Height;
                    int x = (int)screen.PrevPoints[i][0];
                    int y = (int)screen.PrevPoints[i][1];

                    Rectangle rect = new Rectangle(x - (width / 2) - padding, y - (height / 2) - padding, width + padding * 2, height + padding * 2);
                    /*
                    CvInvoke.Rectangle(infraredImage, rect, new Gray(colorcode).MCvScalar, thickness); // 2 pixel box thick

                    CvInvoke.PutText(infraredImage,
                                    i.ToString(),
                                    new System.Drawing.Point((int)prevPoints[i][0], (int)prevPoints[i][1]),
                                    FontFace.HersheyComplex,
                                    1.0,
                                    new Gray(colorcode).MCvScalar);
    */
                    if (screen.PointInfo[i].Visible)
                    {
                        CvInvoke.Rectangle(colImg, rect, new Bgr(0, 255, 0).MCvScalar, thickness); // 2 pixel box thick
                    }
                    else
                    {
                        CvInvoke.Rectangle(colImg, rect, new Bgr(0, 0, 255).MCvScalar, thickness); // 2 pixel box thick
                    }



                    CvInvoke.PutText(colImg,
                    i.ToString(),
                    new System.Drawing.Point((int)screen.PrevPoints[i][0] - width, (int)screen.PrevPoints[i][1] + height),
                    FontFace.HersheyComplex,
                    .6,
                    new Bgr(255, 255, 0).MCvScalar);

                }


            }

            int height1 = 10;
            int width1 = 10;
            Rectangle rect1 = new Rectangle((int)missingx[0] - (width1 / 2) - padding, (int)missingx[1] - (height1 / 2) - padding, width1 + padding * 2, height1 + padding * 2);
            CvInvoke.Rectangle(colImg, rect1, new Bgr(255, 0, 255).MCvScalar, thickness); // 2 pixel box thick

            return colImg;

        }

        /// <summary>
        /// Draws tracked data on a Byte infraredImage
        /// </summary>
        /// <param name="ThresholdedInfraredImage"></param>
        private void DrawTrackedData(Image<Gray, Byte> ThresholdedInfraredImage)
        {
            if (screen.PrevPoints != null)
            {


                int thickness = Properties.UserSettings.Default.DataIndicatorThickness;
                int colorcode = Properties.Settings.Default.DataIndicatorColor8bit;
                int padding = Properties.Settings.Default.DataIndicatorPadding;

                for (int i = 0; i < screen.PrevPoints.Length; i++)
                {
                    int width = screen.PointInfo[i].Width;
                    int height = screen.PointInfo[i].Height;
                    Rectangle rect = new Rectangle((int)screen.PrevPoints[i][0] - (width / 2) - padding, (int)screen.PrevPoints[i][1] - (height / 2) - padding, width + padding * 2, height + padding * 2);
                    CvInvoke.Rectangle(ThresholdedInfraredImage, rect, new Gray(colorcode).MCvScalar, thickness); // 2 pixel box thick



                }
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
