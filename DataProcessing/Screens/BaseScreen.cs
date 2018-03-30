namespace ScreenTracker.DataProcessing.Screens
{
    abstract class BaseScreen
    {

        int height, width;
        public BaseScreen(int height, int width)
        {
            this.height = height;
            this.width = width;
        }



        public bool InFrame(double[] point)
        {

            double x = point[0];
            double y = point[1];

            if (x >= 0 && x <= this.width)
            {
                if (y >= 0 && y <= this.height)
                    return true;
            }
            return false;
        }


    }
}
