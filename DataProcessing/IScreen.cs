using Emgu.CV;



namespace InfraredKinectData.DataProcessing
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
