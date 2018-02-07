using Emgu.CV;



namespace ScreenTracker.DataProcessing
{
    interface IScreen
    {


        PointInfo[] PointInfo { get; 
                                set; }

        double[][] PrevPoints
        {
            get;
            set;
        }
    
        void Initialize(double[][] newPoints, Mat stats);


        void UpdateScreen(double[][] newPoints);

    }
}
