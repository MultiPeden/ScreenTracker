using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;


namespace Microsoft.Samples.Kinect.InfraredBasics
{
    class IRUtils
    {




        public static String IRPointsJson(int id, int x, int y)
        {
            // return String.Format("{{\"IRPoint\":{{\"id\":{0},\"x\":{1},\"y\":{2}}}}}", id, x, y);
            return String.Format("{{\"id\":{0},\"x\":{1},\"y\":{2}}}", id, x, y);
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
