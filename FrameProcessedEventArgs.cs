using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    public class FrameProcessedEventArgs : EventArgs
    {
        public WriteableBitmap ThresholdBitmap { get; set; }
        public WriteableBitmap InfraredBitmap { get; set; }
        public WriteableBitmap ColorBitmap { get; set; }
        public WriteableBitmap DepthBitmap { get; set; }

    }
}
