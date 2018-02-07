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
        private double[][] prevPoints;

        public double[][] PrevPoints { get => prevPoints; set => prevPoints = value; }
        internal PointInfoSpring[] PointInfo { get => pointInfo; set => pointInfo = value; }



        Dictionary<String, Constraint> pairDict = new Dictionary<String, Constraint>();




        public void UpdateScreen(double[][] newPoints)
        {
            TimeStep(newPoints);
        }


        /* this is an important methods where the time is progressed one time step for the entire cloth.
This includes calling satisfyConstraint() for every constraint, and calling timeStep() for all particles
*/

        public void TimeStep(double[][] newPoints)
        {


            /*
             * 
             * 
             * 
             * 
             * update known points, and old pos if visible   - done
             * satisfy invisible points' constraints   ---- edit, only satisfy for a single pointt
             * timestep invisible points'
             * 
             * update old pos for all points
             * 
             * 
             */

            /*
                        for (int i = 0; i < Properties.UserSettings.Default.Spring_ConstraintIterations; i++)
                        {
                            foreach (PointInfoSpring point in PointInfo)
                            {
                                point.SatisfyConstraints();
                            }
                        }
                        */

            for (int i = 0; i < newPoints.Length; i++)
            {
                if (newPoints[i] == null)
                {
                    pointInfo[i].MakeMovable();
                }
                else
                {
                    pointInfo[i].MakeUnmovable();
                }
            }



            UpdateKnownPoints(newPoints);

            SatisfyConstraints(newPoints);



            for (int i = 0; i < newPoints.Length; i++)
            {
                if (newPoints[i] == null)
                {
                    pointInfo[i].TimeStep();
                    newPoints[i] = new double[] { pointInfo[i].GetPos().X, pointInfo[i].GetPos().Y };
                }
            }



            this.prevPoints = newPoints;


        }


        public void AssignConstraints()
        {
            int num_particles_width = Properties.UserSettings.Default.GridColums;
            int num_particles_height = Properties.UserSettings.Default.GridRows;

            for (int i = 0; i < prevPoints.Length; i++)
            {
                List<int> cardinals = pointInfo[i].GetCardinals(pointInfo, i, num_particles_width, num_particles_height);

                foreach (int cardinal in cardinals)
                {
                    //     constraints.Add(new Constraint(pointInfo[i], pointInfo[cardinal]));
                    //     PointPair2 pp = new PointPair2(i, cardinal);
                    Constraint constraint = new Constraint(pointInfo[i], pointInfo[cardinal], PointPair(i, cardinal));

                    pairDict.Add(PointPair(i, cardinal), constraint);
                }
            }

        }

        private string PointPair(int p1, int p2)
        {
            if (p1 < p2)
            {
                return "" + p1 + "," + p2;
            }
            return "" + p2 + "," + p1;

        }

        public void SatisfyConstraints(double[][] newPoints)
        {
            HashSet<Constraint> missingPointsConstraints = new HashSet<Constraint>();


            for (int i = 0; i < newPoints.Length; i++)
            {
                if (newPoints[i] == null)
                {
                    foreach (int id in PointInfo[i].CardinalIDs)
                    {
                        PointInfo[i].Visible = false;
                        Constraint constraint = pairDict[PointPair(i, id)];

                        missingPointsConstraints.Add(constraint);
                    }
                }


                foreach (Constraint constraint in missingPointsConstraints)
                {
                    constraint.SatisfyConstraint();
                }



            }
        }





        /// <summary>
        /// function for updating known points from the camera frame
        /// </summary>
        /// <param name="newPoints"></param>
        public void UpdateKnownPoints(double[][] newPoints)
        {


            for (int i = 0; i < newPoints.Length; i++)
            {

                double[] point = newPoints[i];

                if (point != null)
                {
                    Vector3 vec = new Vector3((float)point[0], (float)point[1], (float)0);
                    PointInfoSpring pinfo = pointInfo[i];

                    pinfo.SetPos(vec);
                    pinfo.SetOldPos(vec);
                }

            }




        }

        /* used to add gravity (or any other arbitrary vector) to all particles*/
        public void AddForce(Vector3 direction)
        {

            foreach (PointInfoSpring point in PointInfo)
            {
                point.AddForce(direction);
            }



        }


        /* A private method used by windForce() to calcualte the wind force for a single triangle 
defined by p1,p2,p3*/
        public void AddWindForcesForTriangle(PointInfoSpring p1, PointInfoSpring p2, PointInfoSpring p3, Vector3 direction)
        {

            Vector3 normal = calcTriangleNormal(p1, p2, p3);
            Vector3 d = Vector3.Normalize(normal);
            Vector3 force = normal * (Vector3.Dot(d, direction));
            p1.AddForce(force);
            p2.AddForce(force);
            p3.AddForce(force);
        }



        /* A private method used by drawShaded() and addWindForcesForTriangle() to retrieve the  
normal vector of the triangle defined by the position of the particles p1, p2, and p3.
The magnitude of the normal vector is equal to the area of the parallelogram defined by p1, p2 and p3
*/
        private Vector3 calcTriangleNormal(PointInfoSpring p1, PointInfoSpring p2, PointInfoSpring p3)
        {
            Vector3 pos1 = p1.GetPos();
            Vector3 pos2 = p2.GetPos();
            Vector3 pos3 = p3.GetPos();

            Vector3 v1 = pos2 - pos1;
            Vector3 v2 = pos3 - pos1;

            return Vector3.Cross(v1, v2);
        }




    }
}
