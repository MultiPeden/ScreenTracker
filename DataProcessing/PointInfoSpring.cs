using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;


namespace InfraredKinectData.DataProcessing
{
    class PointInfoSpring : PointInfo
    {


        //   spring model
        private bool movable; // can the particle move or not ? used to pin parts of the cloth

        private float mass; // the mass of the particle (is always 1 in this example)
        private Vector3 pos; // the current position of the particle in 3D space
        private Vector3 old_pos; // the position of the particle in the previous time step, used as part of the verlet numerical integration scheme
        private Vector3 acceleration; // a vector representing the current acceleration of the particle
        private Vector3 accumulated_normal; // an accumulated normal (i.e. non normalized), used for OpenGL soft shading


        private List<Constraint> constraints;
        // end spring



        int id;
        PointInfoSpring pN, pE, pS, pW, p2N, p2E, p2S, p2W;
        PointInfoSpring pNE, pSE, pSW, pNW;


        PointInfoSpring p2SE, p2NE;

        //  scaling hariable  double[] sN, sE, sS, sW, s2N, s2E, s2S, s2W;



        //for displacement calculations 
        double[] orignalPos;



        public PointInfoSpring(int height, int width, int id, double[] position) : base(height, width)
        {

            // spring
            this.pos = new Vector3((float)position[0], (float)position[1], (float)position[2]);
            this.old_pos = pos;
            this.mass = 1;
            this.movable = true;
            ///


            this.id = id;
            this.Visible = true;
            this.orignalPos = position;

            this.constraints = new List<Constraint>();
      

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



        public void addForce(Vector3 f)
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

        public void OffsetPos( Vector3 v)
        {
            if (this.movable)
            {
                pos += v;
            }
        }
        public void MakeUnmovable() { movable = false; }





        public void SetConstraints()
        {
            // cardinal points
      //      SetConstraint(pN);
            SetConstraint(pE);
            SetConstraint(pS);
     //       SetConstraint(pW);

            // second cardinal
       //     SetConstraint(p2N);
            SetConstraint(p2E);
            SetConstraint(p2S);
       //     SetConstraint(p2W);


            // inter-cardinal
            SetConstraint(pNE);
            SetConstraint(pSE);
            //    SetConstraint(pSW);
            //   SetConstraint(pNW);


            SetConstraint(p2SE);
            SetConstraint(p2NE);

        }

        public List<Constraint> Constraints { get => constraints; }

        private void SetConstraint(PointInfoSpring p2)
        {
            if(p2 != null)
            {
                Constraints.Add(new Constraint(this, p2));
            }
        }

        public void SatisfyConstraints()
        {
            foreach (Constraint constraint in Constraints)
            {
                constraint.SatisfyConstraint();
            }
        }


        //////////////////////



        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public double[] EstimatePostition(double[][] points)
        {
            double[] estPoint;
            double accX = 0;
            double accY = 0;
            int count = 0;

            estPoint = Extrapolate(pN, p2N, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }

            estPoint = Extrapolate(pE, p2E, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }

            estPoint = Extrapolate(pS, p2S, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }

            estPoint = Extrapolate(pW, p2W, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }

            if (count != 0)
            {
                //  estPoint[0] = accX / count;
                //  estPoint[1] = accY / count;

                estPoint = new double[2]
                {
                    accX / count,
                    accY / count
                };

                return estPoint;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinal"></param>
        /// <param name="cardinal2"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public double[] Extrapolate(PointInfoSpring cardinal, PointInfoSpring cardinal2, double[][] points)
        {



            if (cardinal != null && cardinal2 != null && cardinal.Visible && cardinal2.Visible)
            {


                int cardinalId = cardinal.id;
                int cardinalId2 = cardinal2.id;


                double[] p1 = points[cardinalId];
                double[] p2 = points[cardinalId2];

                if (p1 == null || p2 == null)
                {
                    return null;
                }
                else
                {

                    double[] est = new double[2] {
                    p1[0] * 2 - p2[0],
                    p1[1] * 2 - p2[1]
                };
                    return est;
                }
            }
            else
            {
                return null;
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="id"></param>
        /// <param name="numOfCols"></param>
        /// <param name="numOfRows"></param>
        public void AssignCardinalPoints(PointInfoSpring[] points, int id, int numOfCols, int numOfRows)
        {
            int pos;
            int n = (numOfCols * numOfRows) - 1;
            int idModCols = id % numOfCols;


            //         PointInfoRelation pNE, pSE, pSW, pNW;

            // north, north east and 2nd north
            pos = id - numOfCols;
            if (pos >= 0)
            {
                // north
                this.pN = points[pos];

                // north east
                int posNE = pos + 1;
                if (posNE <= n && posNE % numOfCols >= idModCols)
                {
                    this.pNE = points[pos];
                }

                // north west
                int posNW = pos - 1;
                if (posNW >= 0 && posNW % numOfCols <= idModCols)
                {
                    this.pNW = points[pos];
                }

                // second north
                pos -= numOfCols;
                if (pos >= 0)
                {
                    this.p2N = points[pos];


                    // Second NE
                    pos += 2;
                    if(pos <= n && pos % numOfCols >= idModCols)
                    {
                        this.p2NE = points[pos];
                    }


                }
            }
            // East and 2nd east
            pos = id + 1;
            if (pos <= n && pos % numOfCols >= idModCols)
            {
                this.pE = points[pos];
                pos += 1;
                if (pos <= n && pos % numOfCols >= idModCols)
                {
                    this.p2E = points[pos];
                }
            }
            // South and 2nd south
            pos = id + numOfCols;
            if (pos <= n)
            {
                this.pS = points[pos];


                // south east
                int posSE = pos + 1;
                if (posSE <= n && posSE % numOfCols >= idModCols)
                {
                    this.pSE = points[pos];

                }

                // north west
                int posSw = pos - 1;
                if (posSw >= 0 && posSw % numOfCols <= idModCols)
                {
                    this.pSW = points[pos];
                }


                pos += numOfCols;
                if (pos <= n)
                {
                    this.p2S = points[pos];

                    // Second NE
                    pos += 2;
                    if (pos <= n && pos % numOfCols >= idModCols)
                    {
                        this.p2SE = points[pos];
                    }


                }
            }
            // West and 2nd west
            pos = id - 1;
            if (pos >= 0 && pos % numOfCols <= idModCols)
            {
                this.pW = points[pos];
                pos -= 1;
                if (pos >= 0 && pos % numOfCols <= idModCols)
                {
                    this.p2W = points[pos];
                }
            }
        }

        /// <summary>
        /// retuns the displacement of the point.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private double[] Displacement(double[] position)
        {
            return new double[]
            {
               DisplacementFunction( position[0] - this.orignalPos[0]) ,
               DisplacementFunction( position[1] - this.orignalPos[1])
            };
        }


        private double DisplacementFunction(double displacement)
        {
            var sign = Math.Sign(displacement);
            displacement = Math.Abs(displacement);

            return displacement;

            //   return sign * (2 / (0.1 + Math.Exp(-displacement)));


        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public double[] EstimatePostitionDisplacement(double[][] points, int mode)
        {
            double[] estPoint;
            double accX = 0;
            double accY = 0;
            int count = 0;


            estPoint = ExtrapolateDisplacement(pN, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;

            }



            estPoint = ExtrapolateDisplacement(pE, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }



            estPoint = ExtrapolateDisplacement(pW, points);
            if (estPoint != null)
            {
                accX += estPoint[0];
                accY += estPoint[1];
                count++;
            }


            if (mode == 1)
            {


                estPoint = ExtrapolateDisplacement(pNE, points);
                if (estPoint != null)
                {
                    accX += estPoint[0];
                    accY += estPoint[1];
                    count++;
                }


                estPoint = ExtrapolateDisplacement(pSE, points);
                if (estPoint != null)
                {
                    accX += estPoint[0];
                    accY += estPoint[1];
                    count++;
                }

                estPoint = ExtrapolateDisplacement(pSW, points);
                if (estPoint != null)
                {
                    accX += estPoint[0];
                    accY += estPoint[1];
                    count++;
                }


                estPoint = ExtrapolateDisplacement(pNW, points);
                if (estPoint != null)
                {
                    accX += estPoint[0];
                    accY += estPoint[1];
                    count++;
                }

            }


            if (count != 0)
            {
                //  estPoint[0] = accX / count;
                //  estPoint[1] = accY / count;

                estPoint = new double[2]
                {
                   this.orignalPos[0] + (accX / count),
                   this.orignalPos[1] + (accY / count)
                };

                return estPoint;
            }
            else
            {
                return null;
            }

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardinal"></param>
        /// <param name="cardinal2"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        private double[] ExtrapolateDisplacement(PointInfoSpring cardinal, double[][] points)
        {



            if (cardinal != null && cardinal.Visible)
            {


                int cardinalId = cardinal.id;



                double[] p = points[cardinalId];


                if (p == null)
                {
                    return null;
                }
                else
                {

                    return cardinal.Displacement(p);
                }
            }
            else
            {
                return null;
            }

        }


    }
}
