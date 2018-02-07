using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenTracker
{
    class PointsConnected : IComparer<double[]>
    {
        public int Compare(double[] point1, double[] point2)
        {
            double x1 = point1[0];
            double y1 = point1[1];
            double x2 = point2[0];
            double y2 = point2[1];

            if (x1 < x2)
            {
                if(y1 < y2)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }

            }


            if (x1 > x2)
            {

            }

            return 1;

        }

  
    }
}

