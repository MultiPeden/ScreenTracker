﻿
using ScreenTracker.DataProcessing.Screens.Points;
using System.Numerics;

namespace ScreenTracker.DataProcessing.Screens
{
    class Spring
    {
        private float rest_distance; // the length between particle p1 and p2 in rest configuration
        public PointInfoSpring p1, p2; // the two particles that are connected through this constraint
        public string name;
        private float springConstant;

        public Spring(PointInfoSpring p1, PointInfoSpring p2, string name)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.name = name;
            this.springConstant = Properties.UserSettings.Default.Spring_constant;

            Vector3 vec = p1.GetPos() - p2.GetPos();
            this.rest_distance = vec.Length();

        }


        /* This is one of the important methods, where a single constraint between two particles p1 and p2 is solved
the method is called by Cloth.time_step() many times per frame*/
        public void SatisfyConstraint()
        {

            Vector3 p1_to_p2 = p2.GetPos() - p1.GetPos(); // vector from p1 to p2
            float current_distance = p1_to_p2.Length(); // current distance between p1 and p2
            Vector3 correctionVector = p1_to_p2 * (1 - rest_distance / current_distance); // The offset vector that could moves p1 into a distance of rest_distance to p2
            Vector3 correctionVectorHalf = Vector3.Multiply(correctionVector, springConstant * (float)0.5); // Lets make it half that length, so that we can move BOTH p1 and p2.


            p1.AddForce(correctionVectorHalf); // correctionVectorHalf is pointing from p1 to p2, so the length should move p1 half the length needed to satisfy the constraint.
            p2.AddForce(-correctionVectorHalf); // we must move p2 the negative direction of correctionVectorHalf since it points from p2 to p1, and not p1 to p2.	
        }



        public void SatisfyConstraintNorth()
        {

            Vector3 p1_to_p2 = p1.GetPos() - p2.GetPos(); // vector from p1 to p2
            float current_distance = p1_to_p2.Length(); // current distance between p1 and p2
            Vector3 correctionVector = p1_to_p2 * (1 - rest_distance / current_distance); // The offset vector that could moves p1 into a distance of rest_distance to p2
            Vector3 correctionVectorHalf = Vector3.Multiply(correctionVector, springConstant * (float)0.5); // Lets make it half that length, so that we can move BOTH p1 and p2.
            p1.AddForce(-correctionVectorHalf); // correctionVectorHalf is pointing from p1 to p2, so the length should move p1 half the length needed to satisfy the constraint.
            p2.AddForce(correctionVectorHalf); // we must move p2 the negative direction of correctionVectorHalf since it points from p2 to p1, and not p1 to p2.	
        }





    }
}
