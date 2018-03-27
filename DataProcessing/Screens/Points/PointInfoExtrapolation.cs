namespace ScreenTracker.DataProcessing.Screens.Points
{
    class PointInfoExtrapolation : PointInfo
    {
        int id;
        PointInfoExtrapolation pN, pE, pS, pW, p2N, p2E, p2S, p2W;
        private bool visible;

  //      public bool Visible { get => visible; set => visible = value; }

        public PointInfoExtrapolation(int height, int width, int id) : base(height, width)
        {
            this.id = id;
            this.visible = true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="id"></param>
        /// <param name="numOfCols"></param>
        /// <param name="numOfRows"></param>
        public void AssignCardinalPoints(PointInfoExtrapolation[] points, int id, int numOfCols, int numOfRows)
        {
            int n = (numOfCols * numOfRows) - 1;
            int idModCols = id % numOfCols;

            int index;

            // north
            int north = CanGoNorth(id, n, numOfCols);
            if (north != -1)
            {
                this.pN = points[north];

                // second north
                int north2 = CanGoNorth(north, n, numOfCols);
                if (north2 != -1)
                {
                    this.p2N = points[north2];
                }

            }


            // south
            int south = CanGoSouth(id, n, numOfCols);
            if (south != -1)
            {
                this.pS = points[south];


                // second south
                int south2 = CanGoSouth(south, n, numOfCols);
                if (south2 != -1)
                {
                    this.p2S = points[south2];


                }
            }

            // east
            index = CanGoEast(id, n, numOfCols, idModCols);
            if (index != -1)
            {
                this.pE = points[index];
                // second east
                index = CanGoEast(index, n, numOfCols, idModCols);
                if (index != -1)
                {
                    this.p2E = points[index];
                }
            }

            // west
            index = CanGoWest(id, n, numOfCols, idModCols);
            if (index != -1)
            {
                this.pW = points[index];

                // second west
                index = CanGoWest(index, n, numOfCols, idModCols);
                if (index != -1)
                {
                    this.p2W = points[index];
                }
            }
        }







        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public double[] EstimatePostition(double[][] points)
        {
            double[] estPoint;
            double accX = 0;
            double accY = 0;
            int count = 0;

            estPoint = Extrapolate(pN, p2N, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }

            estPoint = Extrapolate(pE, p2E, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }

            estPoint = Extrapolate(pS, p2S, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }

            estPoint = Extrapolate(pW, p2W, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }

            if (count != 0)
            {
                //  estPoint[0] = accX / count;
                //  estPoint[1] = accY / count;

                estPoint = new double[2]
                {
                    accX / count,
                    accY / count
                };

                return estPoint;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinal"></param>
        /// <param name="cardinal2"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public double[] Extrapolate(PointInfoExtrapolation cardinal, PointInfoExtrapolation cardinal2, double[][] points)
        {



            if (cardinal != null && cardinal2 != null && cardinal.visible && cardinal2.visible)
            {


                int cardinalId = cardinal.id;
                int cardinalId2 = cardinal2.id;


                double[] p1 = points[cardinalId];
                double[] p2 = points[cardinalId2];

                if (p1 == null || p2 == null)
                {
                    return null;
                }
                else
                {

                    double[] est = new double[2] {
                    p1[0] * 2 - p2[0],
                    p1[1] * 2 - p2[1]
                };
                    return est;
                }
            }
            else
            {
                return null;
            }

        }




    }
}