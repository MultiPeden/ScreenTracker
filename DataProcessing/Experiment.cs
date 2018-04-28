using Emgu.CV;

namespace ScreenTracker.DataProcessing
{



    class Experiment
    {

        int frameCounter;
        int imageCounter;
        int fpsLimiter;
        System.IO.StreamWriter fileNull, fileSpring, fileExtra, fileDisp;
        bool record;

        public Experiment()
        {

            frameCounter = 0;
            imageCounter = 0;
            fpsLimiter = 15;
            fileNull = new System.IO.StreamWriter(@"C:\\test\\trackingNull.txt", false);
            fileSpring = new System.IO.StreamWriter(@"C:\\test\\trackingSpring.txt", false);
            fileExtra = new System.IO.StreamWriter(@"C:\\test\\trackingExtrapolation.txt", false);
            fileDisp = new System.IO.StreamWriter(@"C:\\test\\trackingDisplacement.txt", false);
        }


        ~Experiment()
        {
            fileNull.Dispose();
            fileSpring.Dispose();
            fileExtra.Dispose();
            fileDisp.Dispose();
        }




        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void RecordFrame(Mat img)
        {

            if (ShouldRecord())
            {
                img.Save("C:\\test\\test" + imageCounter + ".png");
                imageCounter++;
            }



            frameCounter++;

        }


        public bool ShouldRecord()
        {
            return (frameCounter % fpsLimiter) == 0;
        }


        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void RecordTracking(double[][] points, double[][] cameraPoints,
                                   double[][] pointsSpring, double[][] cameraPointsSpring,
                                   double[][] pointsExtra, double[][] cameraPointsExtra,
                                   double[][] pointsDisp, double[][] cameraPointsDisp)
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






        }




    }



}
