using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenTracker.DataReceiver
{
    [CLSCompliant(false)]
    public interface ICamera
    {
        /// <summary>
        /// eventhandler for sending frames as EMGU images when they are
        /// received from the camera
        /// </summary>
        event EventHandler<EMGUargs> EmguArgsProcessed;

        /// <summary>
        /// EventHandler for passing on the camera availibility status on
        /// </summary>
        event EventHandler<bool> ChangeStatusText;

        /// <summary>
        /// enable/disable colorimage. The colorimage is only nessesary 
        /// when the mainwindow is visible and the color option is selected 
        /// </summary>
        /// <param name="enable"></param>
        void GenerateColorImage(bool enable);

        /// <summary>
        /// ScreenToWorldCoordinates takes the reference points in the image coordinate system
        /// and converts them to the WorldCoordinate system using the cameras mapper containing DepthSpacePoint.
        /// return the new coordinates
        /// </summary>
        /// <param name="newPoints"></param>
        /// <param name="zCoordinates"></param>
        /// <returns></returns>
        double[][] ScreenToWorldCoordinates(double[][] newPoints, ushort[] zCoordinates);



    }
}
