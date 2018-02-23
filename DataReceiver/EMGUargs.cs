using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;

using System.Windows.Media.Imaging;

namespace ScreenTracker.DataReceiver
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

        public Mat Colorimage { get; set; }
       // public Mat<Bgr, UInt16> Colorimage { get; set; }
        public FrameDimension ColorFrameDimension { get; set; }

        // Infrared image <Gray, UInt16> 
        public Mat InfraredImage { get; set; }
        public FrameDimension InfraredFrameDimension { get; set; }

        // Depth image Image<Gray, UInt16>
        public Mat DepthImage { get; set; }
        public FrameDimension DepthFrameDimension { get; set; }

    }
}
