﻿namespace ScreenTracker.DataReceiver
{
    /// <summary>
    /// class for holding information about frame dimensions
    /// (Width and Height)
    /// </summary>
    public class FrameDimension
    {
        private int width;
        private int height;

        public FrameDimension(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }
    }
}
