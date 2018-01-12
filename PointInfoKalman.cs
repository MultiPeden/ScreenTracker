using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;



namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    class PointInfoKalman : PointInfo
    {
        int depth;
        private KalmanFilter kal;

        float px, py, cx, cy, ix, iy;
        private bool visible;



        private SyntheticData syntheticData;
        public PointInfoKalman(int height, int width, double x, double y ) : base(height, width)
        {
            // this.depth = depth;
            visible = true;


            syntheticData = new SyntheticData();




            int stateSize = 4;
            int measSize = 2;
            int contrSize = 0;

            kal = new KalmanFilter(stateSize, measSize, contrSize);



            Matrix<float> state = new Matrix<float>(new float[]
            {
            (float)x, (float)y , 0.0f, 0.0f
             }); // [x,y,v_x,v_y,w,h]

           //TODO kal.Correct(state.Mat); //             kal.CorrectedState = state;


            // Transition State Matrix A
            // Note: set dT at each processing step!
            // [ 1 0 dT 0  0 0 ]
            // [ 0 1 0  dT 0 0 ]
            // [ 0 0 1  0  0 0 ]
            // [ 0 0 0  1  0 0 ]
            // [ 0 0 0  0  1 0 ]
            // [ 0 0 0  0  0 1 ]


         //   kal.StatePre = state;

            state.Mat.CopyTo(kal.StatePre);

            //Console.WriteLine(kal.TransitionMatrix.Data.GetValue(0));
            Console.WriteLine(kal.TransitionMatrix.GetValue(0, 0));
            syntheticData.transitionMatrix.Mat.CopyTo(kal.TransitionMatrix);///   kal.TransitionMatrix = syntheticData.transitionMatrix;
            Console.WriteLine(kal.TransitionMatrix.GetValue(0,0));

            syntheticData.measurementNoise.Mat.CopyTo(kal.MeasurementNoiseCov); //   kal.MeasurementNoiseCovariance = syntheticData.measurementNoise;
            syntheticData.processNoise.Mat.CopyTo(kal.ProcessNoiseCov); // kal.ProcessNoiseCovariance = syntheticData.processNoise;
            syntheticData.errorCovariancePost.Mat.CopyTo(kal.ErrorCovPost); //kal.ErrorCovariancePost = syntheticData.errorCovariancePost;
            syntheticData.measurementMatrix.Mat.CopyTo(kal.MeasurementMatrix); // kal.MeasurementMatrix = syntheticData.measurementMatrix;
          










        }

        public bool Tracked { get => visible; set => visible = value; }
        public float Px { get => px; set => px = value; }
        public float Py { get => py; set => py = value; }

        public PointF[] filterPoints(PointF pt)
        {
            syntheticData.state[0, 0] = pt.X;
            syntheticData.state[1, 0] = pt.Y;         

            Mat prediction = kal.Predict();
            PointF predictPoint = new PointF(prediction.GetValue(0,0) , prediction.GetValue(1, 0));
            PointF measurePoint = new PointF(syntheticData.GetMeasurement()[0, 0],
            syntheticData.GetMeasurement()[1, 0]);

            Mat estimated = kal.Correct(syntheticData.GetMeasurement().Mat);
            PointF estimatedPoint = new PointF(estimated.GetValue(0, 0), estimated.GetValue(1, 0));
            syntheticData.GoToNextState();
            PointF[] results = new PointF[2];
            results[0] = predictPoint;
            results[1] = estimatedPoint;
            px = predictPoint.X;
            py = predictPoint.Y;
            cx = estimatedPoint.X;
            cy = estimatedPoint.Y;
            return results;
        }



        public PointF PredictUntracked()
        {


            Mat prediction = kal.Predict();
            PointF predictPoint = new PointF(prediction.GetValue(0, 0), prediction.GetValue(1, 0));
            PointF measurePoint = new PointF(syntheticData.GetMeasurement()[0, 0],
            syntheticData.GetMeasurement()[1, 0]);

            px = predictPoint.X;
            py = predictPoint.Y;

            //  Mat estimated = kal.Correct(syntheticData.GetMeasurement().Mat);
            //   PointF estimatedPoint = new PointF(estimated.GetValue(0, 0), estimated.GetValue(1, 0));
            //   syntheticData.GoToNextState();
            PointF[] results = new PointF[2];
            results[0] = predictPoint;


            return predictPoint;
        }

    }
}
