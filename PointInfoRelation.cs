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
        private bool visible;

        public bool Visible { get => visible; set => visible = value; }

        public PointInfoRelation(int height, int width, int id) : base(height, width)
        {
            this.id = id;
            this.visible= true;

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

            estPoint  = Extrapolate(pN, p2N, points);
            if(estPoint != null)
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
                estPoint[0] = accX / count;
                estPoint[1] = accY / count;
                return estPoint;
            }
            else {
                return points[this.id];
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

            

            if (cardinal != null && cardinal2 != null) //&& cardinal.visible && cardinal2.visible)
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

            // north and 2nd north
            pos = id - numOfCols;
            if (pos >= 0)
            {
                this.pN = points[pos];
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


    }
}
