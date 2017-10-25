using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
