using CsvHelper;
using System.Diagnostics;
using System.IO;

namespace ScreenTracker.DataProcessing
{
    class TrackerTimer
    {
        private Stopwatch pointMatchingTimer = new Stopwatch();
        private Stopwatch trackingTimer = new Stopwatch();
        private Stopwatch costMatrixTimer = new Stopwatch();
        private Stopwatch SpringEstimationTimer = new Stopwatch();
        private Stopwatch ExtrapolationEstimationTimer = new Stopwatch();
        private Stopwatch DisplacementEstimationTimer = new Stopwatch();
        private TextWriter writer;
        private CsvWriter csvWriter;
        private TimerRes timerRes;
        private bool timerON;



        public TrackerTimer()
        {


            this.timerON = true;

            writer = new StreamWriter(@"C:\test\TrackerTimers.csv", append: true);
            long FileLength = new FileInfo(@"C:\test\TrackerTimers.csv").Length;

            csvWriter = new CsvWriter(writer);
            timerRes = new TimerRes();
            if (FileLength == 0)
            {
                csvWriter.WriteHeader(timerRes.GetType());
                csvWriter.NextRecord();
            }
        }


        ~TrackerTimer()
        {
            csvWriter.Flush();
            writer.Dispose();
        }


        public void StartPointMatchingtimer()
        {
            pointMatchingTimer.Restart();
            //    csvWriter.WriteHeader()
        }





        public void StartCostMatrixTimer()
        {
            costMatrixTimer.Restart();
        }

        public void StartTrackingtimer()
        {
            trackingTimer.Restart();
        }

        public void StartSpringEstimationtimer()
        {
            SpringEstimationTimer.Restart();
        }

        public void StartExtrapolatioEstimationtimer()
        {
            ExtrapolationEstimationTimer.Restart();
        }

        public void StartDisplacementEstimationtimer()
        {
            DisplacementEstimationTimer.Restart();
        }


        public void StopPointMatchingTimer(int numbOfPoints, int maxPoints)
        {
            pointMatchingTimer.Stop();
            timerRes.PointMatchingTiming = pointMatchingTimer.Elapsed.TotalMilliseconds;
            timerRes.TrackedPoints = numbOfPoints;
            timerRes.TotalPoints = maxPoints;
            timerRes.MissingPoints = maxPoints - numbOfPoints;
        }

        public void StopTrackingTimer()
        {
            trackingTimer.Stop();
            timerRes.TrackingTiming = trackingTimer.Elapsed.TotalMilliseconds;
        }

        public void StopCostMatrixTimer()
        {
            costMatrixTimer.Stop();
            timerRes.CostMatrixTiming = costMatrixTimer.Elapsed.TotalMilliseconds;
        }



        public void StopSpringEstimationTimer()
        {
            SpringEstimationTimer.Stop();
            timerRes.EstimationSpringTiming = SpringEstimationTimer.Elapsed.TotalMilliseconds;


        }

        public void StopExtrapolationEstimationTimer()
        {
            ExtrapolationEstimationTimer.Stop();
            timerRes.EstimationExtrapolationTiming = ExtrapolationEstimationTimer.Elapsed.TotalMilliseconds;


        }


        public void StopDisplacementEstimationTimer()
        {
            DisplacementEstimationTimer.Stop();
            timerRes.EstimationDisplacementTiming = DisplacementEstimationTimer.Elapsed.TotalMilliseconds;


        }



        public void WriteTimersToFile()
        {
            if (this.timerON)
            {
                csvWriter.WriteRecord(timerRes);
                csvWriter.NextRecord();

            }


        }


        public void FlushTimer()
        {

            this.timerON = false;
            csvWriter.Flush();
            writer.Flush();
            csvWriter.Dispose();
            writer.Close();
            writer.Dispose();
        }

    }
}
