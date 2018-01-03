using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    public interface ICameraInterface
    {


        event EventHandler<EMGUargs> emguArgsProcessed;

        void GenerateColorImage(bool enable);




    }
}
