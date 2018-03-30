

using System;

namespace ScreenTracker.GUI
{
    class FPSCounter
    {
        private int FramesSinceLast = 0;
        private MainWindow mainWindow;

        private DateTime start;
        private TimeSpan elapsed;

        public FPSCounter(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            start = DateTime.Now;

        }


        /// <summary>
        /// 
        /// </summary>
        public void Update()
        {

            elapsed = DateTime.Now - start;


            if (elapsed.Seconds > 0)
            {
                mainWindow.FPSText = "fps: " + FramesSinceLast;
                FramesSinceLast = 1;
                start = DateTime.Now;

            }
            else
            {
                FramesSinceLast++;
            }

        }






    }
}
