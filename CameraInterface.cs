using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    [CLSCompliant(false)]
    public interface ICameraInterface
    {
     

        // eventhandler for sending frames as EMGU images when they are
        // received from the camera
        event EventHandler<EMGUargs> emguArgsProcessed;

        /// <summary>
        /// EventHandler for passing on the camera availibility status on
        /// </summary>
        event EventHandler<bool> ChangeStatusText;

        // enable/disable colorimage. The colorimage is only nessesary 
        // when the mainwindow is visible and the color option is selected 
        void GenerateColorImage(bool enable);


        double[][] ScreenToWorldCoordinates(double[][] newPoints, ushort[] zCoordinates);



    }
}
