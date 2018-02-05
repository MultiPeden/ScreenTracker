using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraredKinectData.DataProcessing
{
    class Screen
    {

        /// <summary>
        /// Info for each detected point i the frame.
        /// </summary>
        private PointInfoRelation[] pointInfo;


        /// <summary>
        /// Array for holding points found in the previous frame
        /// </summary>
        double[][] prevPoints;

        public double[][] PrevPoints { get => prevPoints; set => prevPoints = value; }
        internal PointInfoRelation[] PointInfo { get => pointInfo; set => pointInfo = value; }
    }
}
