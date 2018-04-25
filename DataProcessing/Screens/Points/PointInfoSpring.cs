using System.Collections.Generic;
using System.Numerics;


namespace ScreenTracker.DataProcessing.Screens.Points
{
    class PointInfoSpring : PointInfo
    {


        //   spring model
        private bool movable; // can the particle move or not ? used to pin parts of the cloth

        private float mass; // the mass of the particle (is always 1 in this example)
        private Vector3 pos; // the current position of the particle in 3D space
        private Vector3 old_pos; // the position of the particle in the previous time step, used as part of the verlet numerical integration scheme
        private Vector3 acceleration; // a vector representing the current acceleration of the particle
        private double stepsize;

        private List<int> cardinalIDs;
        // end spring



        public int id;



        //for displacement calculations 
        double[] orignalPos;



        public PointInfoSpring(int height, int width, int id, double[] position) : base(height, width)
        {

            // spring
            this.pos = new Vector3((float)position[0], (float)position[1], (float)position[2]);
            this.old_pos = pos;
            this.mass = 1;
            this.movable = false;
            ///
            this.stepsize = Properties.UserSettings.Default.Spring_StepSize;

            this.id = id;
            this.Visible = true;
            this.orignalPos = position;

            this.cardinalIDs = new List<int>();


        }


        public double[] GetVelocity(double[] newPos)
        {
            return new double[] {
            stepsize * (this.pos.X - newPos[0]),
            stepsize * (this.pos.Y - newPos[0]),
            stepsize * (this.pos.Z - newPos[0])
            };


        }

        public double[] GetOldVelocity()
        {
            return new double[] {
            stepsize * (this.pos.X - this.old_pos.X),
            stepsize * (this.pos.Y - this.old_pos.Y),
            stepsize * (this.pos.Z - this.old_pos.Z)
            };


        }





        public float CosineSim(double[] newPos)
        {

            Vector3 newp = new Vector3((float)newPos[0], (float)newPos[1], (float)newPos[2]);
            //      P -> Q
            //from oldpos -> newp
            Vector3 newDir = newp - this.pos;
            Vector3 olddir = this.pos - this.old_pos;


            float dotp = Vector3.Dot(olddir, newDir);
            float Lenghts = olddir.Length() * newDir.Length();

            if (Lenghts == 0)
            {
                return 0;
            }
            else
            {
                return dotp / Lenghts;
            }
        }


        //// spring model
        /* This is one of the important methods, where the time is progressed a single step size (TIME_STEPSIZE)
   The method is called by Cloth.time_step()
   Given the equation "force = mass * acceleration" the next position is found through verlet integration*/
        public void TimeStep()
        {
            if (movable)
            {
                float damping = Properties.UserSettings.Default.Spring_Damping;

                Vector3 temp = pos;


                ///   pos = pos + (pos - old_pos) * (1.0 - damping) + acceleration * stepsize;

                //    pos = Vector3.Multiply(pos + (pos - old_pos), (float)(1.0 - damping)) + acceleration * stepsize;

                // 	pos = pos + (pos-old_pos)*(1.0-DAMPING) 

                pos = pos + Vector3.Multiply((pos - old_pos), (float)(1.0 - damping)) + acceleration * (float)stepsize;



                old_pos = temp;
                ResetAcceleration(); // acceleration is reset since it HAS been translated into a change in position (and implicitely into velocity)	
            }
        }



        public void AddForce(Vector3 f)
        {
            acceleration += f / mass;
        }


        public Vector3 GetPos()
        {
            return this.pos;
        }

        public void ResetAcceleration()
        {
            acceleration = new Vector3(0, 0, 0);
        }

        public void OffsetPos(Vector3 v)
        {
            if (this.movable)
            {
                pos += v;
            }
        }


        public void SetPos(Vector3 vec)
        {
            this.pos = vec;
        }


        public void SetOldPos(Vector3 vec)
        {
            this.old_pos = vec;
        }

        public Vector3 GetOldPos()
        {
            return this.old_pos;
        }


        public void MakeUnmovable() { movable = false; }

        public void MakeMovable() { movable = true; }




        public List<int> CardinalIDs { get => cardinalIDs; }

        public bool RelativePosOK(double[] pos, double[][] newpoints, int numOfCols, int numOfRows)
        {
            int index;
            int n = (numOfCols * numOfRows) - 1;
            int idModCols = id % numOfCols;

            double x = pos[0];
            double y = pos[1];

            // north
            int north = CanGoNorth(id, n, numOfCols);
            if (north != -1)
            {

                if (newpoints[north] == null)
                    return true;
                if (newpoints[north][1] < y)
                    return false;
            }

            // south
            int south = CanGoSouth(id, n, numOfCols);
            if (south != -1)
            {
                if (newpoints[south] == null)
                    return true;
                if (newpoints[south][1] > y)
                    return false;
            }


            // east
            int east = CanGoEast(id, n, numOfCols, idModCols);
            if (east != -1)
            {
                if (newpoints[east] == null)
                    return true;
                if (newpoints[east][0] < x)
                    return false;
            }


            // west
            int west = CanGoWest(id, n, numOfCols, idModCols);
            if (west != -1)
            {
                if (newpoints[west] == null)
                    return true;
                if (newpoints[west][0] > x)
                    return false;
            }


            return true;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="id"></param>
        /// <param name="numOfCols"></param>
        /// <param name="numOfRows"></param>
        public List<int> GetCardinals(int id, int numOfCols, int numOfRows)
        {
            int index;
            int n = (numOfCols * numOfRows) - 1;
            int idModCols = id % numOfCols;


            List<int> SouthEast_cardinals = new List<int>();

            // north
            int north = CanGoNorth(id, n, numOfCols);
            if (north != -1)
            {
                cardinalIDs.Add(north);

                // north east
                index = CanGoEast(north, n, numOfCols, idModCols);
                if (index != -1)
                {
                    cardinalIDs.Add(index);
                }
                // north west
                index = CanGoWest(north, n, numOfCols, idModCols);
                if (index != -1)
                {
                    cardinalIDs.Add(index);
                }
                // second north
                int north2 = CanGoNorth(north, n, numOfCols);
                if (north2 != -1)
                {
                    cardinalIDs.Add(north2);

                    // second north east
                    index = CanGo2East(north2, n, numOfCols, idModCols);
                    if (index != -1)
                    {
                        cardinalIDs.Add(index);
                    }
                    // second north west
                    index = CanGo2West(north2, n, numOfCols, idModCols);
                    if (index != -1)
                    {
                        cardinalIDs.Add(index);
                    }

                }

            }


            // south
            int south = CanGoSouth(id, n, numOfCols);
            if (south != -1)
            {
                cardinalIDs.Add(south);
                SouthEast_cardinals.Add(south);

                // south east
                index = CanGoEast(south, n, numOfCols, idModCols);
                if (index != -1)
                {
                    cardinalIDs.Add(index);
                    SouthEast_cardinals.Add(index);
                }
                // south west
                index = CanGoWest(south, n, numOfCols, idModCols);
                if (index != -1)
                {
                    cardinalIDs.Add(index);
                    SouthEast_cardinals.Add(index);
                }
                // second south
                int south2 = CanGoSouth(south, n, numOfCols);
                if (south2 != -1)
                {
                    cardinalIDs.Add(south2);
                    SouthEast_cardinals.Add(south2);

                    // second south east
                    index = CanGo2East(south2, n, numOfCols, idModCols);
                    if (index != -1)
                    {
                        cardinalIDs.Add(index);
                        SouthEast_cardinals.Add(index);
                    }
                    // second south west
                    index = CanGo2West(south2, n, numOfCols, idModCols);
                    if (index != -1)
                    {
                        cardinalIDs.Add(index);
                        SouthEast_cardinals.Add(index);
                    }

                }
            }


            // east
            index = CanGoEast(id, n, numOfCols, idModCols);
            if (index != -1)
            {
                cardinalIDs.Add(index);
                SouthEast_cardinals.Add(index);

                // second east
                index = CanGoEast(index, n, numOfCols, idModCols);
                if (index != -1)
                {
                    cardinalIDs.Add(index);
                    SouthEast_cardinals.Add(index);
                }


            }

            // west
            index = CanGoWest(id, n, numOfCols, idModCols);
            if (index != -1)
            {
                cardinalIDs.Add(index);

                // second west
                index = CanGoWest(index, n, numOfCols, idModCols);
                if (index != -1)
                {
                    cardinalIDs.Add(index);
                }
            }
            return SouthEast_cardinals;
        }


    }
}
