using ScreenTracker.DataProcessing.Screens.Points;


namespace ScreenTracker.DataProcessing.Screens
{
    interface IScreen
    {



        PointInfo[] PointInfo
        {
            get;
            set;
        }

        double[][] PrevPoints
        {
            get;
            set;
        }

        void Initialize(double[][] newPoints);


        void UpdateScreen(double[][] newPoints);


        bool InFrame(double[] point);


    }
}
