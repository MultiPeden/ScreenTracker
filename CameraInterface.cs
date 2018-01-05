using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    public interface ICameraInterface
    {

        // eventhandler for sending frames as EMGU images when they are
        // received from the camera
        event EventHandler<EMGUargs> emguArgsProcessed;

        // enable/disable colorimage. The colorimage is only nessesary 
        // when the mainwindow is visible and the color option is selected 
        void GenerateColorImage(bool enable);


        double[][] ScreenToWorldCoordinates(double[][] newPoints, ushort[] zCoordinates);



    }
}
