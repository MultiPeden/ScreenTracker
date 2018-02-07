using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;


namespace ScreenTracker.DataProcessing
{
    class PointInfoSpring : PointInfo
    {


        //   spring model
        private bool movable; // can the particle move or not ? used to pin parts of the cloth

        private float mass; // the mass of the particle (is always 1 in this example)
        private Vector3 pos; // the current position of the particle in 3D space
        private Vector3 old_pos; // the position of the particle in the previous time step, used as part of the verlet numerical integration scheme
        private Vector3 acceleration; // a vector representing the current acceleration of the particle
      //  private Vector3 accumulated_normal; // an accumulated normal (i.e. non normalized), used for OpenGL soft shading


        private List<int> cardinalIDs;
        // end spring



        public int id;

        

        //for displacement calculations 
        double[] orignalPos;



        public PointInfoSpring(int height, int width, int id, double[] position) : base(height, width)
        {

            // spring
            this.pos = new Vector3((float)position[0], (float)position[1], 0);
            this.old_pos = pos;
            this.mass = 1;
            this.movable = false;
            ///


            this.id = id;
            this.Visible = true;
            this.orignalPos = position;

            this.cardinalIDs = new List<int>();


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
                float stepsize = Properties.UserSettings.Default.Spring_StepSize;
                Vector3 temp = pos;


                ///   pos = pos + (pos - old_pos) * (1.0 - damping) + acceleration * stepsize;

                pos = Vector3.Multiply(pos + (pos - old_pos), (float)(1.0 - damping)) + acceleration * stepsize;



                old_pos = temp;
                acceleration = new Vector3((float)0, (float)0, (float)0); // acceleration is reset since it HAS been translated into a change in position (and implicitely into velocity)	
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

        public void MakeUnmovable() { movable = false; }

        public void MakeMovable() { movable = true; }




        public List<int> CardinalIDs { get => cardinalIDs; }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="id"></param>
        /// <param name="numOfCols"></param>
        /// <param name="numOfRows"></param>
        public List<int> GetCardinals(PointInfoSpring[] points, int id, int numOfCols, int numOfRows)
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
