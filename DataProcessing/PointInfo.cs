using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraredKinectData.DataProcessing
{
    /// <summary>
    /// Holds information for tracking points
    /// both for visual representation and track filtering
    /// </summary>
    abstract class PointInfo
    {
        // Bounding box dimensions
        private int height;
        private int width;


        private bool visible;

  

        // zFilter for completing OneEuroFilter
        private OneEuroFilter zFilter;
        // Chosen Rate for Filtering
        private double rate = 60;

        public PointInfo(int height, int width)
        {
            this.height = height;
            this.width = width;
            this.zFilter = new OneEuroFilter(1, 0);
        }
        
        public int Height { get => height; set => height = value; }
        public int Width { get => width; set => width = value; }

        public double Filter(double zval)
        {
          return  this.zFilter.Filter(zval,rate);
        }


        public bool Visible { get => visible; set => visible = value; }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="n"></param>
        /// <param name="numOfCols"></param>
        /// <returns></returns>
        public int CanGoSouth(int id, int n, int numOfCols)
        {
            int index = id + numOfCols;

            if (index <= n)
            {
                return index;
            }

            return -1;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="n"></param>
        /// <param name="numOfCols"></param>
        /// <returns></returns>
        public int CanGoNorth(int id, int n, int numOfCols)
        {
            int index = (id - numOfCols);
            if (index >= 0)
            {
                return index;
            }
            return -1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="n"></param>
        /// <param name="numOfCols"></param>
        /// <param name="idModCols"></param>
        /// <returns></returns>
        public int CanGoEast(int id, int n, int numOfCols, int idModCols)
        {
            int index = id + 1;
            if (index <= n && index % numOfCols > idModCols)
            {
                return index;
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="n"></param>
        /// <param name="numOfCols"></param>
        /// <param name="idModCols"></param>
        /// <returns></returns>
        public int CanGoWest(int id, int n, int numOfCols, int idModCols)
        {
            int index = id - 1;
            if (index >= 0 && index % numOfCols < idModCols)
            {
                return index;
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="n"></param>
        /// <param name="numOfCols"></param>
        /// <param name="idModCols"></param>
        /// <returns></returns>
        public int CanGo2East(int id, int n, int numOfCols, int idModCols)
        {
            int index = id + 2;
            if (index <= n && index % numOfCols > idModCols)
            {
                return index;
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="n"></param>
        /// <param name="numOfCols"></param>
        /// <param name="idModCols"></param>
        /// <returns></returns>
        public int CanGo2West(int id, int n, int numOfCols, int idModCols)
        {
            int index = id - 2;
            if (index >= 0 && index % numOfCols < idModCols)
            {
                return index;
            }
            return -1;
        }

    }

}
