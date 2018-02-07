using Emgu.CV;
using System;
using System.Collections.Generic;

using System.Numerics;


namespace ScreenTracker.DataProcessing
{
    class SpringScreen : IScreen
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
        public PointInfo[] PointInfo { get => pointInfo; set => pointInfo = (PointInfoSpring[]) value; }



        Dictionary<String, Constraint> pairDict = new Dictionary<String, Constraint>();

        int num_particles_width = Properties.UserSettings.Default.GridColums;
        int num_particles_height = Properties.UserSettings.Default.GridRows;




        public void Initialize(double[][] orderedCentroidPoints, Mat stats)
        {


            PointInfo = new PointInfoSpring[num_particles_height * num_particles_width];



            int i = 0;
            // initialize points
            foreach (double[] point in orderedCentroidPoints)
            {
                int j = i + 1;
                int width = stats.GetData(j, 2)[0];
                int height = stats.GetData(j, 3)[0];
                int area = stats.GetData(j, 4)[0];
                // set info for each point, used later to get z-coordinate

                //todo
                //screen.PointInfo[i] = new PointInfoSpring(width, height, i, new double[] {point[0], point[1],0 });
                this.PointInfo[i] = new PointInfoSpring(width, height, i, point);
                i++;
                Console.WriteLine("X: " + point[0] + " Y: " + point[1]);
            }




            PrevPoints = orderedCentroidPoints;
            AssignConstraints();
        }


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
                    foreach (int id in pointInfo[i].CardinalIDs)
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





    }
}
