using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{

    class PointInfo
    {

        private int height;
        private int width;

        public PointInfo(int height, int width)
        {
            this.height = height;
            this.width = width;
        }
        
        public int Height { get => height; set => height = value; }
        public int Width { get => width; set => width = value; }
    }
}
