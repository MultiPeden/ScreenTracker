using ScreenTracker.DataProcessing.Screens.Points;

namespace ScreenTracker.DataProcessing.Screens
{
    class DisplacementScreen : BaseScreen, IScreen
    {

        /// <summary>
        /// Info for each detected point i the frame.
        /// </summary>
        private PointInfoDisplacement[] pointInfo;

        public PointInfo[] PointInfo { get => pointInfo; set => pointInfo = (PointInfoDisplacement[])value; }


        //constructor
        public DisplacementScreen(int height, int width) : base(height, width) { }


        public void Initialize(double[][] orderedCentroidPoints)
        {
            PointInfo = new PointInfoDisplacement[Num_particles_height * Num_particles_width];
            int j = 2;
            // initialize points
            for (int i = 0; i < orderedCentroidPoints.Length; i++)
            {

                // set info for each point, used to paint tracked marker later
                this.PointInfo[i] = new PointInfoDisplacement(i, orderedCentroidPoints[i]);
                j++;
            }
            // assign kardinal points to pointInfo
            for (int k = 0; k < pointInfo.Length; k++)
            {
                pointInfo[k].AssignCardinalPoints(pointInfo, k, Num_particles_width, Num_particles_height);
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
                    double[] estPoint = pointInfo[k].EstimatePostition(newPoints, 1);
                    // if we can get an estimate using extrapolation, update with the estimated point
                    if (estPoint != null )
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
