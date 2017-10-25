using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Web;
using System.Runtime.Serialization;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    class IRPoint 
    {
        private int id;
        private Point coordinates;

        public int Id { get => id; set => id = value; }
        public Point Coordinates { get => coordinates; set => coordinates = value; }

        public IRPoint(int id, int x, int y)
        {
            this.id = id;
            this.coordinates = new Point(x, y);
        }


        public string toJson()
        {

            return String.Format("{{'id':{0},'x':{1},'y':{2}}}", this.id, this.coordinates.X, this.coordinates.Y );
        }


    }
}
