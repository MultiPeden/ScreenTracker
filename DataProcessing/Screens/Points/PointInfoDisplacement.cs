using System;

namespace ScreenTracker.DataProcessing.Screens.Points
{
    class PointInfoDisplacement : PointInfo
    {
        int id;
        PointInfoDisplacement pN, pE, pS, pW, p2N, p2E, p2S, p2W;
        PointInfoDisplacement pNE, pSE, pSW, pNW;


        //  scaling hariable  double[] sN, sE, sS, sW, s2N, s2E, s2S, s2W;


        //for displacement calculations 
        public double[] orignalPos;

        public PointInfoDisplacement(int height, int width, int id, double[] position) : base(height, width)
        {
            this.id = id;
            Visible = true;
            this.orignalPos = position;

        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="id"></param>
        /// <param name="numOfCols"></param>
        /// <param name="numOfRows"></param>
        public void AssignCardinalPoints(PointInfoDisplacement[] points, int id, int numOfCols, int numOfRows)
        {
            int n = (numOfCols * numOfRows) - 1;
            int idModCols = id % numOfCols;

            int index;

            // north
            int north = CanGoNorth(id, n, numOfCols);
            if (north != -1)
            {
                this.pN = points[north];

                // north east
                index = CanGoEast(north, n, numOfCols, idModCols);
                if (index != -1)
                {
                    this.pNE = points[index];
                }
                // north west
                index = CanGoWest(north, n, numOfCols, idModCols);
                if (index != -1)
                {
                    this.pNW = points[index];
                }
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

                // south east
                index = CanGoEast(south, n, numOfCols, idModCols);
                if (index != -1)
                {
                    this.pSE = points[index];
                }
                // south west
                index = CanGoWest(south, n, numOfCols, idModCols);
                if (index != -1)
                {
                    this.pSW = points[index];
                }
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
        /// retuns the displacement of the point.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private double[] Displacement(double[] position)
        {
            return new double[]
            {
               DisplacementFunction( position[0] - this.orignalPos[0]) ,
               DisplacementFunction( position[1] - this.orignalPos[1]) ,
               DisplacementFunction( position[2] - this.orignalPos[2])
            };
        }


        private double DisplacementFunction(double displacement)
        {
            var sign = Math.Sign(displacement);
            displacement = Math.Abs(displacement);

            return sign * displacement;

            //   return sign * (2 / (0.1 + Math.Exp(-displacement)));


        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public double[] EstimatePostitionDisplacement(double[][] points, int mode)
        {
            double[] estPoint;
            double[] acc = new double[3] { 0, 0, 0 };
            int count = 0;


            estPoint = ExtrapolateDisplacement(pN, points);
            count += AccumulateVector(acc, estPoint);

            estPoint = ExtrapolateDisplacement(pE, points);
            count += AccumulateVector(acc, estPoint);

            estPoint = ExtrapolateDisplacement(pW, points);
            count += AccumulateVector(acc, estPoint);

            estPoint = ExtrapolateDisplacement(pS, points);
            count += AccumulateVector(acc, estPoint);




            if (mode == 1)
            {


                estPoint = ExtrapolateDisplacement(pNE, points);
                count += AccumulateVector(acc, estPoint);

                estPoint = ExtrapolateDisplacement(pSE, points);
                count += AccumulateVector(acc, estPoint);


                estPoint = ExtrapolateDisplacement(pSW, points);
                count += AccumulateVector(acc, estPoint);

                estPoint = ExtrapolateDisplacement(pNW, points);
                count += AccumulateVector(acc, estPoint);

            }


            if (count != 0)
            {
                //  estPoint[0] = accX / count;
                //  estPoint[1] = accY / count;

                return new double[3]{
                   this.orignalPos[0] + ScaleDistx(acc[0] / count),
                   this.orignalPos[1] + ScaleDisty(acc[1] / count),
                   this.orignalPos[2] + ScaleDisty(acc[2] / count)};


            }
            else
            {
                return null;
            }

        }

        private double ScalarFun(double x)
        {
            //  return (-0.0558 * Math.Pow(x, 3)) + (43.223 * Math.Pow(x, 2)) - (11148 * x) + 958141;
            // y = -0.0558x3 + 43.223x2 - 11148x + 958141

            // return 5.5934 * x - 1186.7;

            return x;


        }

        private double ScaleDistx(double x)
        {
            // v1 y = -0.0596x3 - 0.0097x2 + 6.7821x + 0.5564
            // v2 y = -0.0322x3 + 0.0674x2 + 4.2751x + 1.0466
            // y = -0.0322x3 + 0.0674x2 + 4.2751x + 1.0466


            return x;
            //   return (-0.0322 * Math.Pow(x, 3)) + (0.0674 * Math.Pow(x, 2)) + (4.2751 * x) + 1.0466;
        }



        private double ScaleDisty(double y)
        {
            //  y = -0.0318x3 - 0.6185x2 + 7.4246x - 2.0988
            return y;
            //  return (-0.0318 * Math.Pow(y, 3)) - (0.6185 * Math.Pow(y, 2)) + (7.4246 * y) - 2.0988;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinal"></param>
        /// <param name="cardinal2"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        private double[] ExtrapolateDisplacement(PointInfoDisplacement cardinal, double[][] points)
        {



            if (cardinal != null && cardinal.Visible)
            {


                int cardinalId = cardinal.id;



                double[] p = points[cardinalId];


                if (p == null)
                {
                    return null;
                }
                else
                {

                    return cardinal.Displacement(p);
                }
            }
            else
            {
                return null;
            }

        }


    }
}
