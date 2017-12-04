using System;
using System.Xml;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    /// <summary>
    /// class for RoomAlive calibration data 
   /// </summary>
    class CalibrationData
    {

        private double[][] colorCameraMatrix;
        private double[][] depthCameraMatrix;
        private double[][] projectorCameraMatrix;
        private double[][] projectorPoseMatrix;
        private double[][] camPoseMatrix;
        private double[] colorLensDistortionVector;
        private double[] depthLensDistortioVector;
        private double[][] depthToColorTransformMatrix;
        private double[] lensDistortionVector;
        private int projectorHeight;
        private int projectorwidth;

        /// <summary>
        /// loads a RoomAlive calibration xml file from the specified "path"
        /// </summary>
        /// <param name="path"></param>
        public CalibrationData(string path)
        {

            var doc = new XmlDocument();
            // try to load file
            try
            {
                doc.Load(path);
            }
            catch (Exception e)
            {

                Console.WriteLine(e);
            }
            


            // get handles to xml nodes
            XmlNode EnsambleXML = doc["ProjectorCameraEnsemble"];
            XmlNode CameraEnsambleXML = EnsambleXML["cameras"].FirstChild;
            XmlNode CameraCalibrationXML = CameraEnsambleXML["calibration"];
            XmlNode ProjectorEnsambleXML = EnsambleXML["projectors"].FirstChild;

            // get camera matrices and vectors
            XmlElement colorCameraMatrixXML = CameraCalibrationXML["colorCameraMatrix"];
            this.colorCameraMatrix = XmlToMatrix(colorCameraMatrixXML);

            XmlElement colorLensDistortionXML = CameraCalibrationXML["colorLensDistortion"];
            this.colorLensDistortionVector = XmlToVector(colorLensDistortionXML);

            XmlElement depthCameraMatrixXML = CameraCalibrationXML["depthCameraMatrix"];
            this.depthCameraMatrix = XmlToMatrix(depthCameraMatrixXML);

            XmlElement depthLensDistortionXML = CameraCalibrationXML["depthLensDistortion"];
            this.depthLensDistortioVector = XmlToVector(depthLensDistortionXML);

            XmlElement depthToColorTransformXML = CameraCalibrationXML["depthToColorTransform"];
            this.depthToColorTransformMatrix = XmlToMatrix(depthToColorTransformXML);

            XmlElement camPoseXML = CameraEnsambleXML["pose"];
            this.camPoseMatrix = XmlToMatrix(camPoseXML);


            // get projector matrices and vectors
            XmlElement cameraMatrixXML = ProjectorEnsambleXML["cameraMatrix"];
            this.projectorCameraMatrix = XmlToMatrix(cameraMatrixXML);

            XmlElement lensDistortionXML = ProjectorEnsambleXML["lensDistortion"];
            this.lensDistortionVector = XmlToVector(lensDistortionXML);

            XmlElement poseXML = ProjectorEnsambleXML["pose"];
            this.projectorPoseMatrix = XmlToMatrix(poseXML);

            // projector resolution
            this.projectorHeight = Convert.ToInt32(ProjectorEnsambleXML["height"].InnerText);
            this.projectorwidth = Convert.ToInt32(ProjectorEnsambleXML["width"].InnerText);

        }

        /// <summary>
        /// Getters for the local varables
        /// </summary>
        public double[][] DepthCameraMatrix { get => depthCameraMatrix; }
        public double[][] ColorCameraMatrix { get => colorCameraMatrix; }
        public double[][] ProjectorCameraMatrix { get => projectorCameraMatrix; }
        public int Projectorwidth { get => projectorwidth; }
        public int ProjectorHeight { get => projectorHeight; }
        public double[] LensDistortionVector { get => lensDistortionVector; }
        public double[][] DepthToColorTransformMatrix { get => depthToColorTransformMatrix; }
        public double[] DepthLensDistortioVector { get => depthLensDistortioVector; }
        public double[] ColorLensDistortionVector { get => colorLensDistortionVector; }
        public double[][] CamPoseMatrix { get => camPoseMatrix; }
        public double[][] ProjectorPoseMatrix { get => projectorPoseMatrix; }

        /// <summary>
        ///  converts from  XmlElement matrix  to a jagged double array
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        private double[][] XmlToMatrix(XmlElement matrix)
        {
            XmlNodeList cols = matrix["ValuesByColumn"].ChildNodes;
            double[][] outMatrix = new double[cols.Count][];
            // foreach col
            for (int i = 0; i < cols.Count; i++)
            {
                XmlNodeList row = cols.Item(i).ChildNodes;
                double[] outrow = new double[row.Count];
                // foreach row         
                for (int j = 0; j < row.Count; j++)
                {
                    outrow[j] = Convert.ToDouble(row.Item(j).InnerText);
               }
                outMatrix[i] = outrow;
            }
            // transpose 
            return outMatrix.TransposeRowsAndColumns();
        }

        /// <summary>
        /// Converts from XmlElement vector to an array
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        private double[] XmlToVector(XmlElement vector)
        {
            XmlNodeList cols = vector["ValuesByColumn"].FirstChild.ChildNodes;
            double[] outVector = new double[cols.Count];

            for (int i = 0; i < cols.Count; i++)
            {
                Console.WriteLine(cols.Item(i).InnerText);
                outVector[i] = Convert.ToDouble(cols.Item(i).InnerText);
            }
            return outVector;
        }


    }
}
