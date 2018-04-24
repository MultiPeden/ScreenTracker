using Emgu.CV;
using ScreenTracker.DataProcessing.Screens.Points;

namespace ScreenTracker.DataProcessing.Screens
{
    class ExtrapolationScreen : BaseScreen
    {

        /// <summary>
        /// Info for each detected point in the frame.
        /// </summary>
        private PointInfoExtrapolation[] pointInfo;



        public PointInfo[] PointInfo { get => pointInfo; set => pointInfo = (PointInfoExtrapolation[])value; }



        int num_particles_width = Properties.UserSettings.Default.GridColums;
        int num_particles_height = Properties.UserSettings.Default.GridRows;


        public ExtrapolationScreen(int height, int width) : base(height, width) { }

        public void Initialize(double[][] orderedCentroidPoints, Mat stats)
        {



            PointInfo = new PointInfoExtrapolation[num_particles_height * num_particles_width];



            int j = 2;
            int width, height, area;
            // initialize points
            for (int i = 0; i < orderedCentroidPoints.Length; i++)
            {
                width = stats.GetData(j, 2)[0];
                height = stats.GetData(j, 3)[0];
                area = stats.GetData(j, 4)[0];
                // set info for each point, used to paint tracked marker later

                this.PointInfo[i] = new PointInfoExtrapolation(width, height, i);

                j++;
            }



            // assign kardinal points to pointInfo
            for (int i = 0; i < pointInfo.Length; i++)
            {
                pointInfo[i].AssignCardinalPoints(pointInfo, i, num_particles_width, num_particles_height);
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

                    double[] estPoint = pointInfo[k].EstimatePostition(newPoints);


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
