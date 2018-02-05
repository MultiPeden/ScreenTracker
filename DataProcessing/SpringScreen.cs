using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace InfraredKinectData.DataProcessing
{
    class SpringScreen
    {
        /// <summary>
        /// Info for each detected point i the frame.
        /// </summary>
        private PointInfoSpring[] pointInfo;


        /// <summary>
        /// Array for holding points found in the previous frame
        /// </summary>
        double[][] prevPoints;

        public double[][] PrevPoints { get => prevPoints; set => prevPoints = value; }
        internal PointInfoSpring[] PointInfo { get => pointInfo; set => pointInfo = value; }




        /* this is an important methods where the time is progressed one time step for the entire cloth.
This includes calling satisfyConstraint() for every constraint, and calling timeStep() for all particles
*/
        public void TimeStep()
        {



            for (int i = 0; i < Properties.UserSettings.Default.Spring_ConstraintIterations; i++)
            {
                foreach (PointInfoSpring point in PointInfo)
                {
                    point.SatisfyConstraints();
                }
            }

            foreach (PointInfoSpring point in PointInfo)
            {
                point.TimeStep();
            }






        }

        /* used to add gravity (or any other arbitrary vector) to all particles*/
        public void AddForce(Vector3 direction)
        {

            foreach (PointInfoSpring point in PointInfo)
            {
                point.addForce(direction);
            }



        }


    }
}
