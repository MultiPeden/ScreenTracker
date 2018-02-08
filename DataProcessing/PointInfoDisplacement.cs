using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenTracker.DataProcessing
{
    class PointInfoDisplacement : PointInfo
    {
        int id;
        PointInfoDisplacement pN, pE, pS, pW, p2N, p2E, p2S, p2W;
        PointInfoDisplacement pNE, pSE, pSW, pNW;


        //  scaling hariable  double[] sN, sE, sS, sW, s2N, s2E, s2S, s2W;


        //for displacement calculations 
        double[] orignalPos;

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
        public double[] Extrapolate(PointInfoDisplacement cardinal, PointInfoDisplacement cardinal2, double[][] points)
        {



            if (cardinal != null && cardinal2 != null && cardinal.Visible && cardinal2.Visible)
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
               DisplacementFunction( position[1] - this.orignalPos[1])
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
            double accX = 0;
            double accY = 0;
            int count = 0;


            estPoint = ExtrapolateDisplacement(pN, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;

            }



            estPoint = ExtrapolateDisplacement(pE, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }



            estPoint = ExtrapolateDisplacement(pW, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }


            if(mode == 1)
            {


                estPoint = ExtrapolateDisplacement(pNE, points);
                if (estPoint != null)
                {
                    accX += estPoint[0];
                    accY += estPoint[1];
                    count++;
                }


                estPoint = ExtrapolateDisplacement(pSE, points);
                if (estPoint != null)
                {
                    accX += estPoint[0];
                    accY += estPoint[1];
                    count++;
                }

                estPoint = ExtrapolateDisplacement(pSW, points);
                if (estPoint != null)
                {
                    accX += estPoint[0];
                    accY += estPoint[1];
                    count++;
                }


                estPoint = ExtrapolateDisplacement(pNW, points);
                if (estPoint != null)
                {
                    accX += estPoint[0];
                    accY += estPoint[1];
                    count++;
                }

            }


            if (count != 0)
            {
                //  estPoint[0] = accX / count;
                //  estPoint[1] = accY / count;

                estPoint = new double[2]
                {
                   this.orignalPos[0] + (accX / count),
                   this.orignalPos[1] + (accY / count)
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
