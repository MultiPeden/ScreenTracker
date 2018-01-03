using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Windows.Media.Imaging;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    public class EMGUargs : EventArgs
    {


        public Image<Bgr, UInt16> Colorimage { get; set; }
        public FrameDimension ColorFrameDimension { get; set; }

        public Image<Gray, UInt16> InfraredImage { get; set; }
        public FrameDimension InfraredFrameDimension { get; set; }


        public Image<Gray, UInt16> DepthImage { get; set; }
        public FrameDimension DepthFrameDimension { get; set; }

    }
}
