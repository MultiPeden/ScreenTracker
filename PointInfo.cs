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
        private OneEuroFilter zFilter;
        private double rate = 60;

        public PointInfo(int height, int width)
        {
            this.height = height;
            this.width = width;
            this.zFilter = new OneEuroFilter(1, 0);
        }
        
        public int Height { get => height; set => height = value; }
        public int Width { get => width; set => width = value; }

        public double Filter(double zval)
        {
          return  this.zFilter.Filter(zval,rate);
        }
    }
}
