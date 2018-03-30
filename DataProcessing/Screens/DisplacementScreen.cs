using Emgu.CV;
using ScreenTracker.DataProcessing.Screens.Points;
using System;

namespace ScreenTracker.DataProcessing.Screens
{
    class DisplacementScreen : BaseScreen, IScreen
    {

        /// <summary>
        /// Info for each detected point i the frame.
        /// </summary>
        private PointInfoDisplacement[] pointInfo;


        /// <summary>
        /// Array for holding points found in the previous frame
        /// </summary>
        public double[][] prevPoints;

        public double[][] PrevPoints { get => prevPoints; set => prevPoints = value; }
        public PointInfo[] PointInfo { get => pointInfo; set => pointInfo = (PointInfoDisplacement[])value; }



        int num_particles_width = Properties.UserSettings.Default.GridColums;
        int num_particles_height = Properties.UserSettings.Default.GridRows;


        //constructor
        public DisplacementScreen(int height, int width) : base(height, width) { }




        public void Initialize(double[][] orderedCentroidPoints, Mat stats)
        {



            PointInfo = new PointInfoDisplacement[num_particles_height * num_particles_width];



            int i = 0;
            // initialize points
            foreach (double[] point in orderedCentroidPoints)
            {
                int j = i + 2;
                int width = stats.GetData(j, 2)[0];
                int height = stats.GetData(j, 3)[0];
                int area = stats.GetData(j, 4)[0];
                // set info for each point, used later to get z-coordinate

                //todo
                //screen.PointInfo[i] = new PointInfoSpring(width, height, i, new double[] {point[0], point[1],0 });
                this.PointInfo[i] = new PointInfoDisplacement(width, height, i, point);
                i++;
                Console.WriteLine("X: " + point[0] + " Y: " + point[1]);
            }



            // assign kardinal points to pointInfo
            for (int k = 0; k < pointInfo.Length; k++)
            {
                pointInfo[k].AssignCardinalPoints(pointInfo, k, num_particles_width, num_particles_height);
            }


            this.prevPoints = orderedCentroidPoints;

        }


        public void UpdateScreen(double[][] newPoints)
        {

            PredictMissingPoints(newPoints);


        }

        private void PredictMissingPoints(double[][] newPoints)
        {


            for (int k = 0; k < newPoints.Length; k++)
            {
                if (newPoints[k] == null)
                {

                    //double[] estPoint = pointInfo[k].EstimatePostition(newPointsSparse);

                    double[] estPoint = pointInfo[k].EstimatePostitionDisplacement(newPoints, 1);


                    // if we can get an estimate using extrapolation, update with the estimated point
                    if (estPoint != null && InFrame(estPoint))
                    {
                        newPoints[k] = estPoint;
                    }
                    else
                    {
                        newPoints[k] = prevPoints[k];

                    }



                    pointInfo[k].Visible = false;
                }

            }

            this.prevPoints = newPoints;
        }


    }
}
