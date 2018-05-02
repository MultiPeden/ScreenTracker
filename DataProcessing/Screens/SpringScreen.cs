using Emgu.CV;
using ScreenTracker.DataProcessing.Screens.Points;
using System;
using System.Collections.Generic;
using System.Numerics;


namespace ScreenTracker.DataProcessing.Screens
{
    class SpringScreen : BaseScreen, IScreen
    {
        /// <summary>
        /// Info for each detected point i the frame.
        /// </summary>
        private PointInfoSpring[] pointInfo;
        public PointInfo[] PointInfo { get => pointInfo; set => pointInfo = (PointInfoSpring[])value; }
        Dictionary<String, Spring> pairDict = new Dictionary<String, Spring>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        public SpringScreen(int height, int width) : base(height, width) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderedCentroidPoints"></param>
        /// <param name="stats"></param>
        public void Initialize(double[][] orderedCentroidPoints, Mat stats)
        {


            PointInfo = new PointInfoSpring[Num_particles_height * Num_particles_width];

            int j = 2;
            int width, height, area;
            // initialize points
            for (int i = 0; i < orderedCentroidPoints.Length; i++)
            {
                width = stats.GetData(j, 2)[0];
                height = stats.GetData(j, 3)[0];
                area = stats.GetData(j, 4)[0];
                // set info for each point, used to paint tracked marker later

                this.PointInfo[i] = new PointInfoSpring(width, height, i, orderedCentroidPoints[i]);

                j++;
            }
            PrevPoints = orderedCentroidPoints;
            AssignConstraints();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPoints"></param>
        public void UpdateScreen(double[][] newPoints)
        {
            TimeStep(newPoints);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPoints"></param>
        public void TimeStep(double[][] newPoints)
        {

            Vector3 point;

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
                    point = pointInfo[i].GetPos();
                    newPoints[i] = new double[] { point.X, point.Y, point.Z };
                }
            }

            this.prevPoints = newPoints;
        }

        /// <summary>
        /// 
        /// </summary>
        private void AddGravity()
        {
            Vector3 gravityVector = new Vector3(0, (float)9.8, 0);
            foreach (PointInfoSpring point in pointInfo)
            {
                point.AddForce(gravityVector);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void AssignConstraints()
        {
            for (int i = 0; i < prevPoints.Length; i++)
            {
                List<int> cardinals = pointInfo[i].GetCardinals(i, Num_particles_width, Num_particles_height);
                foreach (int cardinal in cardinals)
                {
                    Spring constraint = new Spring(pointInfo[i], pointInfo[cardinal], PointPair(i, cardinal));
                    pairDict.Add(PointPair(i, cardinal), constraint);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
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
            HashSet<Spring> missingPointsConstraints = new HashSet<Spring>();
            HashSet<Spring> missingPointsConstraintsNorth = new HashSet<Spring>();

            for (int i = 0; i < newPoints.Length; i++)
            {
                if (newPoints[i] == null)
                {
                    PointInfo[i].Visible = false;
                    foreach (int id in pointInfo[i].CardinalIDs)
                    {
                        Spring constraint = pairDict[PointPair(i, id)];

                        if (id < i)
                        {

                            missingPointsConstraintsNorth.Add(constraint);
                        }
                        else
                        {
                            missingPointsConstraints.Add(constraint);
                        }
                    }
                }
            }

            for (int i = 0; i < 1; i++)
            {
                foreach (Spring constraint in missingPointsConstraints)
                {
                    constraint.SatisfyConstraint();
                }

                foreach (Spring constraint in missingPointsConstraintsNorth)
                {
                    constraint.SatisfyConstraintNorth();
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
                    Vector3 vec = new Vector3((float)point[0], (float)point[1], (float)point[2]);
                    PointInfoSpring pinfo = pointInfo[i];
                    pinfo.SetOldPos(pinfo.GetPos());
                    pinfo.SetPos(vec);
                }
            }
        }
    }
}
