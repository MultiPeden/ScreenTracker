using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    class PointInfoRelation : PointInfo
    {
        int id;
        PointInfoRelation pN, pE, pS, pW, p2N, p2E, p2S, p2W;
        PointInfoRelation pNE, pSE, pSW, pNW;


        //  scaling hariable  double[] sN, sE, sS, sW, s2N, s2E, s2S, s2W;

        private bool visible;

        public bool Visible { get => visible; set => visible = value; }


        //for displacement calculations 
        double[] orignalPos;

        public PointInfoRelation(int height, int width, int id, double[] position) : base(height, width)
        {
            this.id = id;
            this.visible = true;
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
        public double[] Extrapolate(PointInfoRelation cardinal, PointInfoRelation cardinal2, double[][] points)
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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="id"></param>
        /// <param name="numOfCols"></param>
        /// <param name="numOfRows"></param>
        public void AssignCardinalPoints(PointInfoRelation[] points, int id, int numOfCols, int numOfRows)
        {
            int pos;
            int n = (numOfCols * numOfRows) - 1;
            int idModCols = id % numOfCols;


            //         PointInfoRelation pNE, pSE, pSW, pNW;

            // north, north east and 2nd north
            pos = id - numOfCols;
            if (pos >= 0)
            {
                // north
                this.pN = points[pos];

                // north east
                int posNE = pos + 1;
                if (posNE <= n && posNE % numOfCols >= idModCols)
                {
                    this.pNE = points[pos];
                }

                // north west
                int posNW = pos - 1;
                if (posNW >= 0 && posNW % numOfCols <= idModCols)
                {
                    this.pNW = points[pos];
                }

                // second north
                pos -= numOfCols;
                if (pos >= 0)
                {
                    this.p2N = points[pos];
                }
            }
            // East and 2nd east
            pos = id + 1;
            if (pos <= n && pos % numOfCols >= idModCols)
            {
                this.pE = points[pos];
                pos += 1;
                if (pos <= n && pos % numOfCols >= idModCols)
                {
                    this.p2E = points[pos];
                }
            }
            // South and 2nd south
            pos = id + numOfCols;
            if (pos <= n)
            {
                this.pS = points[pos];


                // south east
                int posSE = pos + 1;
                if (posSE <= n && posSE % numOfCols >= idModCols)
                {
                    this.pSE = points[pos];
                }

                // north west
                int posSw = pos - 1;
                if (posSw >= 0 && posSw % numOfCols <= idModCols)
                {
                    this.pSW = points[pos];
                }


                pos += numOfCols;
                if (pos <= n)
                {
                    this.p2S = points[pos];
                }
            }
            // West and 2nd west
            pos = id - 1;
            if (pos >= 0 && pos % numOfCols <= idModCols)
            {
                this.pW = points[pos];
                pos -= 1;
                if (pos >= 0 && pos % numOfCols <= idModCols)
                {
                    this.p2W = points[pos];
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
                position[0] - this.orignalPos[0] ,
                position[1] - this.orignalPos[1]
            };
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
        private double[] ExtrapolateDisplacement(PointInfoRelation cardinal, double[][] points)
        {



            if (cardinal != null && cardinal.visible)
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
