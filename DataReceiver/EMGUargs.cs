using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Windows.Media.Imaging;

namespace InfraredKinectData.DataReceiver
{
    [CLSCompliant(false)]

    /// <summary>
    /// EMGUargs holds the converted camera data as EMGU images
    /// used for further processing.
    /// Each EMGU image is made up of an image and a frame dimension
    /// </summary>
    public class EMGUargs : EventArgs
    {
       // Color image
        public Image<Bgr, UInt16> Colorimage { get; set; }
        public FrameDimension ColorFrameDimension { get; set; }

        // Infrared image
        public Image<Gray, UInt16> InfraredImage { get; set; }
        public FrameDimension InfraredFrameDimension { get; set; }

        // Depth image
        public Image<Gray, UInt16> DepthImage { get; set; }
        public FrameDimension DepthFrameDimension { get; set; }

    }
}
