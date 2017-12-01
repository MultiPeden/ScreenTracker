using System;
using Emgu.CV.Structure;


namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    class IRUtils
    {




        /// <summary>
        /// generates String for point-info in Json format 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static String PointstoJson(double[][] points, ushort[] zCoordinates, int height, int width)
        {
            int i = 0;
            String jSon = "{\"Items\":[";

            if (zCoordinates == null)
            {
                foreach (double[] point in points)
                {
                    // invert y axis
                    jSon += IRUtils.IRPointsJson(i, width - (int)point[0], height - (int)point[1]);
                    if (i < points.Length - 1)
                        jSon += ",";
                    i++;
                }
            }
            else
            {
  
                foreach (double[] point in points)
                {
                    // invert y axis
                    jSon += IRUtils.IRPointsJson(i, width - (int)point[0], height - (int)point[1], (int)zCoordinates[i]);
                    if (i < points.Length - 1)
                        jSon += ",";
                    i++;
                }

            }
            jSon += "]}";
   

            return jSon;
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


    }



}
