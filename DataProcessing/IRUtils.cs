using System;
using Emgu.CV.Structure;
using Microsoft.Kinect;

namespace ScreenTracker.DataProcessing
{
    class IRUtils
    {




        /// <summary>
        /// generates String for point-info in Json format 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static String PointstoJson(double[][] points, ushort[] zCoordinates)
        {
            if (points != null)
            {
                int i = 0;
                String jSon = "{\"Items\":[";



                foreach (double[] point in points)
                {
                    // invert y axis
                    //  jSon += IRUtils.IRPointsJson(i, (width - (int)point[0]) - (width/2) , (height - (int)point[1]) - (height/2) , (int)zCoordinates[i]);
                    // no invert
                    jSon += IRUtils.IRPointsJson(i, (int)point[0] * -1, (int)point[1] * -1, (int)zCoordinates[i]);
                    if (i < points.Length - 1)
                        jSon += ",";
                    i++;
                }


                jSon += "]}";


                return jSon;
            }
            else
            {
                return null;
            }
        }



        public static String IRPointsJson(int id, int x, int y)
        {
            // return String.Format("{{\"IRPoint\":{{\"id\":{0},\"x\":{1},\"y\":{2}}}}}", id, x, y);
            return String.Format("{{\"id\":{0},\"x\":{1},\"y\":{2}}}", id, x, y);
            // return String.Format("{{\"id\":\"{0}\",\"x\":\"{1}\",\"y\":\"{2}\"}}", id, x, y);
        }

        public static String IRPointsJson(int id, int x, int y, int z)
        {
            // return String.Format("{{\"IRPoint\":{{\"id\":{0},\"x\":{1},\"y\":{2}}}}}", id, x, y);
            return String.Format("{{\"id\":{0},\"x\":{1},\"y\":{2},\"z\":{3}}}", id, x, y, z);
            // return String.Format("{{\"id\":\"{0}\",\"x\":\"{1}\",\"y\":\"{2}\"}}", id, x, y);
        }

        public static int LowDist(MCvPoint2D64f p, MCvPoint2D64f[] prevPoints)
        {
            int index = 0;
            double lowDist = 10000;
            int i = 0;
            double dist = 0;

            while (i < prevPoints.Length)
            {
                dist = Dist(p, prevPoints[i]);
                if (dist < lowDist)
                {
                    lowDist = dist;
                    index = i;
                }
                i++;
            }
            return index;
        }






        private static double Dist(MCvPoint2D64f a, MCvPoint2D64f b)
        {

            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }




        public static double[][] GetCostMatrix(double[][] prevPoints, double[][] newpoints)
        {

            double[][] costMatrix = new double[prevPoints.Length][];

            for (int i = 0; i < prevPoints.Length; i++)
            {
                double[] costRow = new double[newpoints.Length];
                for (int j = 0; j < newpoints.Length; j++)
                {
                    costRow[j] = UnsqrtDist(prevPoints[i], newpoints[j]);
                }
                costMatrix[i] = costRow;
            }

            return costMatrix;
        }


        public static double[][] GetCostMatrix2(double[][] prevPoints, double[][] newpoints)
        {

            double[][] costMatrix = new double[newpoints.Length][];

            for (int i = 0; i < newpoints.Length; i++)
            {
                double[] costRow = new double[prevPoints.Length];
                for (int j = 0; j < prevPoints.Length; j++)
                {
                    costRow[j] = UnsqrtDist(newpoints[i], prevPoints[j]);
                }
                costMatrix[i] = costRow;
            }

            return costMatrix;
        }





        public static int[,] GetCostMatrixArray(double[][] prevPoints, double[][] newpoints)
        {

            int[,] costMatrix = new int[prevPoints.Length, newpoints.Length];

            for (int i = 0; i < prevPoints.Length; i++)
            {

                for (int j = 0; j < newpoints.Length; j++)
                {
                    costMatrix[i, j] = (int)Math.Round(UnsqrtDist(prevPoints[i], newpoints[j]));
                }
            }

            return costMatrix;
        }








        private static double UnsqrtDist(double[] a, double[] b)
        {

            return Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2);
        }


        public static double[][] RearrangeArray(double[][] points, int[] indices)
        {

            double[][] ArrangedPoints = new double[indices.Length][];

            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] != -1)
                    ArrangedPoints[i] = points[indices[i]];
            }

            return ArrangedPoints;
        }



        public static double[][] RearrangeArray2(double[][] points, int[] indices, int n)
        {




            double[][] ArrangedPoints = new double[n][];

            try
            {

                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] != -1)
                    {
                        ArrangedPoints[i] = points[indices[i]];
                    }
                    else
                    {

                    }
                }

            }
            catch (Exception)
            {


            }

            return ArrangedPoints;
        }



        public static double[][] RearrangeArray3(double[][] points, int[] indices, int n)
        {




            double[][] ArrangedPoints = new double[n][];

            try
            {

                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] != -1)
                        ArrangedPoints[indices[i]] = points[i];
                }

            }
            catch (Exception)
            {


            }

            return ArrangedPoints;
        }





        public static double[][] RearrangeArray4(double[][] points, int[] indices, int n)
        {




            double[][] ArrangedPoints = new double[n][];

            int index = 0;
            double[] point;
            int mappedindex;

            try
            {

                for (int i = 0; i < indices.Length; i++)
                {

                    index = i;
                    if (indices[i] != -1)
                    {
                        point = points[i];
                        mappedindex = indices[i];

                        ArrangedPoints[mappedindex] = point;
                    }
                }

            }
            catch (Exception)
            {


            }

            return ArrangedPoints;
        }

    }



}
