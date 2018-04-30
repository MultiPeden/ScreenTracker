using Accord.Collections;
using Accord.Statistics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ScreenTracker.Communication;
using ScreenTracker.DataProcessing.Screens;
using ScreenTracker.DataProcessing.Screens.Points;
using ScreenTracker.DataReceiver;
using ScreenTracker.GUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


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
        private IScreen screenExtrapolation;
        private IScreen screenDisplacement;




        Experiment experiment;

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

        private Rectangle mask;

        Mat kernel;



        Hungarian hung;



        int cols;
        int rows;
        int maxPoints;

        bool timerOn = false;

        TrackerTimer trackerTimer;


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

            //this.screen = new DisplacementScreen();
            int[] irdims = cameraData.IRFrameDImensions();
            int height = irdims[0];
            int width = irdims[1];



            this.screenExtrapolation = new ExtrapolationScreen(height, width);
            this.screen = new SpringScreen(height, width);
            this.screenDisplacement = new DisplacementScreen(height, width);






            int padding = 20;

            mask = new Rectangle(padding, padding, width - (padding * 2), height - (padding * 2));

            int kernelSize = Properties.UserSettings.Default.kernelSize;
            this.kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new System.Drawing.Size(kernelSize, kernelSize), new System.Drawing.Point(-1, -1));




            //maskMat = new Mat(424,512, DepthType.Cv8U,1);




            cols = Properties.UserSettings.Default.GridColums;
            rows = Properties.UserSettings.Default.GridRows;
            maxPoints = rows * cols;
            hung = new Hungarian(maxPoints, maxPoints);

            //        CvInvoke.Rectangle(maskMat, mask, new Gray(1).MCvScalar , -1); // 2 pixel box thick


            experiment = new Experiment();


            trackerTimer = new TrackerTimer();
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
            this.ProcessInfraredFrame(e.InfraredImage, e.InfraredFrameDimension, e.DepthImage);

            if (timerOn)
            {
                trackerTimer.WriteTimersToFile();

            }
            if (experiment.SaveFrame)
            {


                experiment.RecordFrame(e.InfraredImage.Clone(), e.Colorimage.Clone());
                timerOn = true;

            }




            SendPoints(screen.PrevPoints);


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
            if (e.Colorimage != null)
            {
                e.Colorimage.Dispose();
            }
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
                this.infraredBitmap = new WriteableBitmap(img.Width, img.Height, 96.0, 96.0, PixelFormats.Bgr24, null);
            }
            // Convert to writablebitmao
            AddToBitmap(this.infraredBitmap, img, (img.Width * img.Height * 3));
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



                this.colorThesholdedBitmap = new WriteableBitmap(img.Width, img.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            }

            //    colorThesholdedBitmap.se

            // Convert to writablebitmao
            AddToBitmap(this.colorThesholdedBitmap, img, (img.Width * img.Height));
            // Send to Mainwindow
            mainWindow.SetLeftImage(this.colorThesholdedBitmap);

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
        private unsafe void AssignZCoordinatesSurroundingBox(double[][] newPoints, Mat stats, Mat depthImage)
        {

            // init array to the size of the array with tracked points
            //ushort[] zCoordinates = new ushort[this.screen.PrevPoints.Length];



            // list for each point's depth sample points
            List<double> zCoords;

            // find the cardinal and inter-candinal points and their values
            // to the list zCoords.
            int x, y;
            int j;
            int BoxWidth, BoxHeight, width, height;
            double[] point;

            for (int i = 0; i < newPoints.Length; i++)
            {
                zCoords = new List<double>();
                point = newPoints[i];
                if (point != null)
                {

                    x = (int)Math.Round(point[0]);
                    y = (int)Math.Round(point[1]);


                    j = i + 2;

                    BoxWidth = stats.GetData(j, 2)[0];
                    BoxHeight = stats.GetData(j, 3)[0];


                    // We want to go half the with and hight(+ padding) 
                    // in each direction to get the estimate
                    width = (BoxWidth / 2) + 1;
                    height = (BoxHeight / 2) + 1;

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
                        double zval = Measures.Mean(zCoords.ToArray());

                        // apply one-euro-filter 
                        //   zCoordinates[i] = (ushort)p.Filter(zval);

                        point[2] = zval;

                    }
                    else
                    {
                        // zCoordinates[i] = 0;
                        point[2] = 1500;
                    }
                    // todo
                    // zCoordinates[i] = 130;
                }
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
        private unsafe void AddDephtPixel(int x, int y, ref Mat depthImage, ref List<double> zCoords)
        {
            if (x >= 0 && x < depthImage.Width && y >= 0 && y < depthImage.Height)
            {


                // Get pointer to first pixel
                //    ushort* pixelP = (ushort*)depthImage.DataPointer.ToPointer();


                //   ushort der = pixelP[x + depthImage.Width + y];

                //   ushort caller = pixelP[x+y];

                // Mat objects created using the create method are stored
                // in one continous memory block.
                //    const Pixel* endPixel = pixel + image1.cols * image1.rows;


                /// todo check correct z vals
                //   string zCoordy = depthImage.GetData(x, y)[1].ToString();//.Data[y, x, 0];

                try
                {
                    // X and Y has med interchanged because the GetData get the trasposed pixel values
                    short zCoord = BitConverter.ToInt16(depthImage.GetData(y, x), 0);

                    zCoords.Add(zCoord);
                }
                catch (Exception)
                {

                    Console.Out.WriteLine("z coord failed x : " + x + " y: " + y);
                }




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
        private void AddDephtPixelcpy(int x, int y, ref Mat depthImage, ref List<double> zCoords)
        {
            if (x >= 0 && x < depthImage.Width && y >= 0 && y < depthImage.Height)
            {

                /// todo check correct z vals
                ushort zCoordy = (ushort)depthImage.GetData(x, y)[0];//.Data[y, x, 0];
                ushort zCoord = 1500;


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
        private void TrackedData(Mat thresholdImg, Mat depthFrame)
        {




            //int minArea = Properties.UserSettings.Default.DataIndicatorMinimumArea;

            // Get Connected component in the frame

            using (Mat labels = new Mat(),
             stats = new Mat(),
             centroids = new Mat())
            {


                int n;
                n = CvInvoke.ConnectedComponentsWithStats(thresholdImg, labels, stats, centroids, LineType.EightConnected, DepthType.Cv16U);


                // Copy centroid points to point array

                int numbOfPoints = n - 2;


                if (this.maxPoints < numbOfPoints)
                {
                    mainWindow.StatusText = "Detected too many makers in the frame  N =" + numbOfPoints + " should be lower than " + maxPoints;
                    return;
                }



                MCvPoint2D64f[] centroidPointsEmgu;
                centroidPointsEmgu = new MCvPoint2D64f[n];

                centroids.CopyTo(centroidPointsEmgu);





                // Convert centoid points to jagged array
                double[][] centroidPoints = GetCentroidPoints(centroidPointsEmgu, n);

                // add z-coordinates to the tracked points
                AssignZCoordinatesSurroundingBox(centroidPoints, stats, depthFrame);


                cameraData.ScreenToWorldCoordinates2(centroidPoints);




                // index for array
                int index;



                int i = 0;
                // if we have no previous points we add the conneted components as the tracked points
                if (screen.PrevPoints == null)
                {


                    if (this.maxPoints != numbOfPoints)
                    {
                        mainWindow.StatusText = "Unable to detect a r: " + rows + " c: " + cols + " grid in the image";

                        return;
                    }






                    double[][] orderedCentroidPoints = new double[rows * cols][];



                    Array.Sort(centroidPoints, (left, right) => right[1].CompareTo(left[1]));

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




                    screen.Initialize(orderedCentroidPoints, stats);
                    screenExtrapolation.Initialize(orderedCentroidPoints, stats);
                    screenDisplacement.Initialize(orderedCentroidPoints, stats);



                }
                else
                { // If we have previous points, we search for their nearest neighbours in the new frame.

                    // copy previous points to new point to avoid loosing any points
                    // newPoints = prevPoints;






                    if (firstDetected)
                    {

                        mainWindow.StatusText = "Detected a r: " + rows + " c: " + cols + " grid in the image";

                        firstDetected = false;
                    }




                    trackerTimer.StopTrackingTimer();
                    trackerTimer.StartPointMatchingtimer();

                    // Use Hungarian algorithm to find points from the old frame, in the new frame
                    int[] minInd = GetPointsIndices(centroidPoints);


                    double[][] rearranged = IRUtils.RearrangeArray2(centroidPoints, minInd, screen.PrevPoints.Length);





                    //Update points
                    foreach (double[] point in rearranged)
                    {

                        if (point != null)
                        {
                            index = minInd[i] + 2;

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

                                PointInfo pInfoextra = screenExtrapolation.PointInfo[i];
                                pInfoextra.Width = width;
                                pInfoextra.Height = height;
                                pInfoextra.Visible = true;


                                PointInfo pInfodisp = screenDisplacement.PointInfo[i];
                                pInfodisp.Width = width;
                                pInfodisp.Height = height;
                                pInfodisp.Visible = true;





                            }
                        }

                        i++;
                    }



                    double[][] rearrangedcopy = (double[][])rearranged.Clone();
                    double[][] rearrangedcopy1 = (double[][])rearranged.Clone();
                    double[][] rearrangedcopy2 = (double[][])rearranged.Clone();

                    trackerTimer.StopPointMatchingTimer(numbOfPoints, maxPoints);

                    trackerTimer.StartSpringEstimationtimer();
                    screen.UpdateScreen(rearranged);
                    trackerTimer.StopSpringEstimationTimer();

                    trackerTimer.StartExtrapolatioEstimationtimer();
                    screenExtrapolation.UpdateScreen(rearrangedcopy1);
                    trackerTimer.StopExtrapolationEstimationTimer();

                    trackerTimer.StartDisplacementEstimationtimer();
                    screenDisplacement.UpdateScreen(rearrangedcopy2);
                    trackerTimer.StopDisplacementEstimationTimer();




                    if (experiment.ShouldRecord())
                    {
                        double[][] cameraSpaceCoordinatesNulls = cameraData.CameraToIR(rearrangedcopy);

                        double[][] cameraSpaceCoordinatesSpring = cameraData.CameraToIR(screen.PrevPoints);
                        double[][] cameraSpaceCoordinatesExtrap = cameraData.CameraToIR(screenExtrapolation.PrevPoints);
                        double[][] cameraSpaceCoordinatesDispos = cameraData.CameraToIR(screenDisplacement.PrevPoints);

                        double[][] colorCamCoordiates = cameraData.CameraToColor(rearrangedcopy);



                        experiment.RecordTracking(rearrangedcopy, cameraSpaceCoordinatesNulls,
                                                  screen.PrevPoints, cameraSpaceCoordinatesSpring,
                                                  screenExtrapolation.PrevPoints, cameraSpaceCoordinatesExtrap,
                                                  screenDisplacement.PrevPoints, cameraSpaceCoordinatesDispos,
                                                  colorCamCoordiates);
                    }

                    //     PointInfo sprinInfo = screen.PointInfo[12];
                    // if (sprinInfo.Visible)
                    //   {
                    //screen.UpdateScreen(newPoints);
                    /*
                    double[] estimatedPos = sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1);
                    double[] orgPost = sprinInfo.orignalPos;

                    double estimateDiffx = estimatedPos[0] - orgPost[0];
                    double aactualDiffx = screen.PrevPoints[12][0] - orgPost[0];
                    */
                    //   double estimateDiffy = estimatedPos[1] - orgPost[1];
                    //   double aactualDiffy = screen.PrevPoints[12][1] - orgPost[1];


                    //    Console.Write(estimateDiffx + ",");

                    //   Console.WriteLine(aactualDiffx + ",");

                    //    Console.Write(estimateDiffy + ",");

                    //  Console.WriteLine(aactualDiffy);

                    /*
                                        Console.Write(sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1)[0] + ",");
                                        Console.Write(sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1)[1] + ",");
                                        Console.Write(screen.PrevPoints[12][0] + ",");
                                        Console.WriteLine(screen.PrevPoints[12][0]);
                                        */
                    /*
                                            missingx = new double[]
                                            {
                                            sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1)[0],
                                            sprinInfo.EstimatePostitionDisplacement(screen.PrevPoints, 1)[1]
                                            };
                                            */
                    //  }

                    //do estimation using the displacement model
                    ///                DisplacementEstimation(double[][] newPointsSparse, double[][] newPoints)

                }
            }

        }

        private double[][] FindNearest(Mat stats, double[][] centroidPoints)
        {

            // build KD-tree for nearest neighbour search
            KDTree<int> tree = KDTree.FromData<int>(screen.PrevPoints, Enumerable.Range(0, screen.PrevPoints.Length).ToArray());
            double[][] newPoints = new double[screen.PrevPoints.Length][];
            int[] mapping = new int[screen.PrevPoints.Length];

            int[] taken = new int[screen.PrevPoints.Length];
            int index;
            //  notAssigned = new List<double[]>();

            for (int k = 0; k < mapping.Length; k++)
            {
                mapping[k] = -1;
            }


            int i = 0;
            //Update points
            foreach (double[] point in centroidPoints)
            {
                int j = i + 2;
                int width = stats.GetData(j, 2)[0];
                int height = stats.GetData(j, 3)[0];
                int area = stats.GetData(j, 4)[0];

                // if the area is more than minArea, discard 
                if (true) // (area > minArea)
                {

                    // find nearest neighbour
                    KDTreeNode<int> nearest = tree.Nearest(point);
                    // get its index
                    index = nearest.Value;
                    // update info for the point
                    PointInfoSpring pInfo = (PointInfoSpring)screen.PointInfo[index];
                    pInfo.Width = width;
                    pInfo.Height = height;
                    pInfo.Visible = true;


                    double[] dublicate = newPoints[index];

                    if (newPoints[index] == null)
                    {
                        newPoints[index] = point;
                        mapping[index] = i;

                    }
                    else
                    {


                        double[] n1 = point;
                        int n2Index = mapping[index];
                        double[] n2 = centroidPoints[n2Index];
                        double[] old = screen.PrevPoints[index];



                        double dist1 = IRUtils.UnsqrtDist(n1, old);
                        double dist2 = IRUtils.UnsqrtDist(n2, old);



                        if (dist1 < dist2)
                        {
                            newPoints[index] = n1;
                            //   notAssigned.Add(n2);
                            mapping[index] = i;
                        }
                        else
                        {
                            newPoints[index] = n2;
                            //    notAssigned.Add(n1);
                            mapping[index] = n2Index;
                        }



                    }


                }

                i++;
            }


            while (mapping.Any(j => j != -1))
            {

                FindRest(stats, centroidPoints, newPoints, mapping);
            }

            return newPoints;

        }

        private void FindRest(Mat stats, double[][] centroidPoints, double[][] newPoints, int[] mapping)
        {
            List<int> missingMatchedPrev = new List<int>();
            for (int i = 0; i < newPoints.Length; i++)
            {
                if (newPoints[i] == null)
                {
                    missingMatchedPrev.Add(i);
                }
            }


            double[][] missingMathedPoints = new double[missingMatchedPrev.Count][];
            for (int i = 0; i < missingMathedPoints.Length; i++)
            {
                missingMathedPoints[i] = screen.PrevPoints[missingMatchedPrev[i]];
            }





            KDTree<int> tree = KDTree.FromData<int>(missingMathedPoints, Enumerable.Range(0, missingMathedPoints.Length).ToArray());


            int index;

            for (int i = 0; i < mapping.Length; i++)
            {
                if (mapping[i] == -1)
                {

                    double[] point = centroidPoints[i];


                    int j = i + 2;
                    int width = stats.GetData(j, 2)[0];
                    int height = stats.GetData(j, 3)[0];
                    int area = stats.GetData(j, 4)[0];


                    // find nearest neighbour
                    KDTreeNode<int> nearest = tree.Nearest(point);
                    // get its index from the prevois points list
                    index = missingMatchedPrev[nearest.Value];
                    // update info for the point
                    PointInfoSpring pInfo = (PointInfoSpring)screen.PointInfo[index];
                    pInfo.Width = width;
                    pInfo.Height = height;
                    pInfo.Visible = true;




                    double[] dublicate = newPoints[index];

                    if (newPoints[index] == null)
                    {
                        newPoints[index] = point;
                        mapping[index] = i;

                    }
                    else
                    {


                        double[] n1 = point;
                        int n2Index = mapping[index];
                        double[] n2 = centroidPoints[n2Index];
                        double[] old = screen.PrevPoints[index];



                        double dist1 = IRUtils.UnsqrtDist(n1, old);
                        double dist2 = IRUtils.UnsqrtDist(n2, old);



                        if (dist1 < dist2)
                        {
                            newPoints[index] = n1;
                            //   notAssigned.Add(n2);
                            mapping[index] = i;
                        }
                        else
                        {
                            newPoints[index] = n2;
                            //    notAssigned.Add(n1);
                            mapping[index] = n2Index;
                        }

                    }
                }

            }
        }





        /// <summary>
        /// Uses the Hungarian algorithm to recognize points from the old frame, in the new frame.
        /// </summary>
        /// <param name="centroidPoints"></param>
        /// <returns></returns>
        private int[] GetPointsIndices(double[][] centroidPoints)
        {




            hung.Solve(screen.PrevPoints, centroidPoints, trackerTimer);


            //    int[,] M = hung.M;
            return hung.GetMinimizedIndicies();

        }



        /// <summary>
        ///  Converts from EMGUs MCvPoint2D64f array to a jagged double array
        /// </summary>
        /// <param name="points"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private double[][] GetCentroidPoints(MCvPoint2D64f[] points, int n)
        {

            double[][] jaggedPoints = new double[points.Length - 2][];


            System.Drawing.Point offsetP = mask.Location;
            MCvPoint2D64f point;


            for (int i = 0; i < jaggedPoints.Length; i++)
            {
                point = points[i + 2];
                jaggedPoints[i] = new double[3] { point.X + offsetP.X, point.Y + offsetP.Y, 0 };
            }

            return jaggedPoints;

        }





        /// <summary>
        /// Converts a list of points to Json and sends it via UDP socket
        /// </summary>
        /// <param name="newPoints"></param>
        private void SendPoints(double[][] points)
        {

            if (points != null)
                udpSender.WriteToSocket(points);


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
            bitmap.AddDirtyRect(new Int32Rect(0, 0, data.Width, data.Height));
            bitmap.Unlock();
            //  data.Dispose();
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
        private void ProcessInfraredFrame(Mat infraredFrameOrg, FrameDimension infraredFrameDimension, Mat depthFrame)
        {



            // init threshold image variable
            //            Image<Gray, Byte> thresholdImg = new Image<Gray, Byte>(infraredFrameDimension.Width, infraredFrameDimension.Height);


            trackerTimer.StartTrackingtimer();

            using (Mat thresholdImg = new Mat(),
                 infraredFrameROI = new Mat(infraredFrameOrg, mask))

            {

                CvInvoke.Normalize(infraredFrameROI, thresholdImg, 0, 255, NormType.MinMax, DepthType.Cv8U);


                CvInvoke.AdaptiveThreshold(thresholdImg, thresholdImg, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 13, -20);


                // perform opening 



                CvInvoke.MorphologyEx(thresholdImg, thresholdImg, MorphOp.Dilate, kernel, new System.Drawing.Point(-1, -1), 2, BorderType.Constant, new MCvScalar(1.0));
                CvInvoke.MorphologyEx(thresholdImg, thresholdImg, MorphOp.Erode, kernel, new System.Drawing.Point(-1, -1), 1, BorderType.Constant, new MCvScalar(1.0));


                // find controids of reflective surfaces and mark them on the image 
                TrackedData(thresholdImg, depthFrame);



                // only generate writeable bitmap if the mainwindow is shown
                if (this.showWindow)
                {

                    // copy the processed image back into a writeable bitmap and dispose the EMGU image
                    if (thresholdedClicked)
                    {
                        using (DrawTrackedData(thresholdImg))
                        {


                            SetThresholdedInfraredImage(thresholdImg, infraredFrameDimension);


                        }

                    }
                    else
                    {

                        using (Mat colImg = DrawTrackedData(infraredFrameOrg))
                        {
                            //  CvInvoke.Normalize(colImg, colImg, 0, 255, NormType.MinMax, DepthType.Cv8U);
                            SetInfraredImage(colImg, infraredFrameDimension);




                        }
                    }
                }

                // cleanup
            }
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


                double[][] cameraSpaceCoordinates = cameraData.CameraToIR(screen.PrevPoints);
                ////double[][] cameraSpaceCoordinates = screen.PrevPoints;



                if (cameraSpaceCoordinates != null)
                {

                    int width, height;
                    int x, y;
                    double[] point;

                    for (int i = 0; i < cameraSpaceCoordinates.Length; i++)
                    {
                        point = cameraSpaceCoordinates[i];
                        width = screen.PointInfo[i].Width;
                        height = screen.PointInfo[i].Height;
                        x = (int)point[0];
                        y = (int)point[1];

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
                            CvInvoke.Rectangle(colImg, rect, new Bgr(100, 100, 0).MCvScalar, -1); // 2 pixel box thick
                        }
                        else
                        {
                            CvInvoke.Rectangle(colImg, rect, new Bgr(0, 0, 255).MCvScalar, thickness); // 2 pixel box thick
                        }



                        CvInvoke.PutText(colImg,
                        i.ToString(),
                                            new System.Drawing.Point((int)point[0] - (width) + 2, (int)point[1] + (height / 2)),
                                            FontFace.HersheyPlain,
                                            .4,
                                            new Bgr(200, 200, 255).MCvScalar);

                    }


                }

                /*
                int height1 = 10;
                int width1 = 10;
                Rectangle rect1 = new Rectangle((int)missingx[0] - (width1 / 2) - padding, (int)missingx[1] - (height1 / 2) - padding, width1 + padding * 2, height1 + padding * 2);
                CvInvoke.Rectangle(colImg, rect1, new Bgr(255, 0, 255).MCvScalar, thickness); // 2 pixel box thick
                */
            }
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








        public void KillTimer()
        {
            trackerTimer.FlushTimer();
        }






    }






}
