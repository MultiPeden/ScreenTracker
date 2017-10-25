using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    class IRPoints
    {

        IRPoint[] irPoints;


        public IRPoints(int size)
        {
            this.irPoints = new IRPoint[size];
        }

        public void insert(IRPoint iRPoint, int index)
        {

            irPoints[index] = iRPoint;


        }


        public String toJson()
        {
            String jSon = "{'IRPoints':[";
            IRPoint currentPoint;

            for (int i = 0; i < irPoints.Length; i++)
            {
                currentPoint = irPoints[i];
                if (currentPoint != null)
                {
                    jSon += currentPoint.toJson();
                    if (i < irPoints.Length - 1)
                    {
                        jSon += ",";
                    }
                    else
                    {
                        jSon += "]}";
                    }
                }
            }

            return jSon;
        }
    }
}
