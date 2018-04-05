namespace ScreenTracker.DataProcessing.Screens
{
    abstract class BaseScreen
    {

        private int height, width;

        private int num_particles_width, num_particles_height;


        /// <summary>
        /// Array for holding points found in the previous frame
        /// </summary>
        public double[][] prevPoints;

        public double[][] PrevPoints { get => prevPoints; set => prevPoints = value; }
        public int Num_particles_width { get => num_particles_width; set => num_particles_width = value; }
        public int Num_particles_height { get => num_particles_height; set => num_particles_height = value; }

        public BaseScreen(int height, int width)
        {
            this.Num_particles_width = Properties.UserSettings.Default.GridColums;
            this.Num_particles_height = Properties.UserSettings.Default.GridRows;
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
