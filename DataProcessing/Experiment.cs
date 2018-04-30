using Emgu.CV;
using System.Threading.Tasks;

namespace ScreenTracker.DataProcessing
{



    class Experiment
    {

        int frameCounter;
        int imageCounter;
        int fpsLimiter;
        System.IO.StreamWriter fileNull, fileSpring, fileExtra, fileDisp, fileColor;
        private bool record;
        private bool saveFrame;

        public bool Record { get => record; set => record = value; }
        public bool SaveFrame { get => saveFrame; set => saveFrame = value; }

        public Experiment()
        {
            Record = false;
            frameCounter = 0;
            imageCounter = -1;
            fpsLimiter = 15;
            fileNull = new System.IO.StreamWriter(@"C:\\test\\trackingNull.txt", false);
            fileSpring = new System.IO.StreamWriter(@"C:\\test\\trackingSpring.txt", false);
            fileExtra = new System.IO.StreamWriter(@"C:\\test\\trackingExtrapolation.txt", false);
            fileDisp = new System.IO.StreamWriter(@"C:\\test\\trackingDisplacement.txt", false);
            fileColor = new System.IO.StreamWriter(@"C:\\test\\trackingColor.txt", false);
            SaveFrame = false;
        }


        ~Experiment()
        {
            fileNull.Dispose();
            fileSpring.Dispose();
            fileExtra.Dispose();
            fileDisp.Dispose();
            fileColor.Dispose();
        }




        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void RecordFrame(Mat img, Mat color)
        {
            imageCounter++;

            Task.Factory.StartNew(() => { SaveFramesAsync(img, color, imageCounter); });


            //     this.record = false;
            SaveFrame = false;
            frameCounter = 0;



        }

        private void SaveFramesAsync(Mat img, Mat color, int count)
        {

            img.Save("C:\\test\\test" + count + ".png");
            color.Save("C:\\test\\test" + count + "color.png");
            img.Dispose();
            color.Dispose();
        }


        public bool ShouldRecord()
        {
            frameCounter++;
            bool shouldRecord = frameCounter == fpsLimiter;

            return shouldRecord;

        }


        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void RecordTracking(double[][] points, double[][] cameraPoints,
                                   double[][] pointsSpring, double[][] cameraPointsSpring,
                                   double[][] pointsExtra, double[][] cameraPointsExtra,
                                   double[][] pointsDisp, double[][] cameraPointsDisp,
                                   double[][] colorCamCoordiates)
        {





            string jSon = IRUtils.PointstoJson(points, cameraPoints);
            fileNull.WriteLine(jSon);
            fileNull.Flush();


            jSon = IRUtils.PointstoJson(pointsSpring);
            fileSpring.WriteLine(jSon);
            fileSpring.Flush();

            jSon = IRUtils.PointstoJson(pointsExtra);
            fileExtra.WriteLine(jSon);
            fileExtra.Flush();

            jSon = IRUtils.PointstoJson(pointsDisp);
            fileDisp.WriteLine(jSon);
            fileDisp.Flush();


            jSon = IRUtils.PointstoJson(colorCamCoordiates);
            fileColor.WriteLine(jSon);
            fileColor.Flush();
            SaveFrame = true;






        }




    }



}
