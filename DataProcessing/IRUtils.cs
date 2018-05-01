using Emgu.CV.Structure;
using System;

namespace ScreenTracker.DataProcessing
{
    class IRUtils
    {




        /// <summary>
        /// generates String for point-info in Json format 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static String PointstoJson(double[][] points)
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
                    if (point != null)
                    {

                        jSon += IRUtils.IRPointsJson(i, point[0], point[1], point[2]);
                        //   break;
                    }
                    else
                    {
                        jSon += IRUtils.IRPointsJsonNull(i);

                    }
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




        /// <summary>
        /// generates String for point-info in Json format 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static String PointstoJson(double[][] points, double[][] cameraPoints)
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
                    if (point != null)
                    {
                        jSon += IRUtils.IRPointsJson(i, cameraPoints[i][0], cameraPoints[i][1], point[2]);
                        //   break;


                    }
                    else
                    {
                        jSon += IRUtils.IRPointsJsonNull(i);

                    }
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



        public static String IRPointsJson(int id, double x, double y)
        {
            // return String.Format("{{\"IRPoint\":{{\"id\":{0},\"x\":{1},\"y\":{2}}}}}", id, x, y);
            return String.Format("{{\"id\":{0},\"x\":{1},\"y\":{2}}}", id, x, y);
            // return String.Format("{{\"id\":\"{0}\",\"x\":\"{1}\",\"y\":\"{2}\"}}", id, x, y);
        }

        public static String IRPointsJson(int id, double x, double y, double z)
        {
            // return String.Format("{{\"IRPoint\":{{\"id\":{0},\"x\":{1},\"y\":{2}}}}}", id, x, y);
            return String.Format("{{\"id\":{0},\"visible\":1,\"x\":{1},\"y\":{2},\"z\":{3}}}", id, (float)x, (float)y, (float)(z * 1000));
            // return String.Format("{{\"id\":\"{0}\",\"x\":\"{1}\",\"y\":\"{2}\"}}", id, x, y);
        }

        public static String IRPointsJsonNull(int id)
        {

            return String.Format("{{\"id\":{0},\"visible\":0,\"x\":null,\"y\":null,\"z\":null}}", id);


        }





        private static double Dist(MCvPoint2D64f a, MCvPoint2D64f b)
        {

            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }











        public static double UnsqrtDist(double[] a, double[] b)
        {

            return Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2) + Math.Pow(b[2] - a[2], 2);
        }




        public static double[][] RearrangeArray(double[][] points, int[] indices, int n)
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



    }




}
