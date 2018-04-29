namespace ScreenTracker.DataProcessing
{
    class TimerRes
    {



        private double pointMatchingTiming;
        private double trackingTiming;
        private double estimationSpringTiming;
        private double estimationExtrapolationTiming;
        private double estimationDisplacementTiming;

        private double costMatrixTiming;
        private int trackedPoints;
        private int totalPoints;
        private int missingPoints;




        public double TrackingTiming { get => trackingTiming; set => trackingTiming = value; }
        public double PointMatchingTiming { get => pointMatchingTiming; set => pointMatchingTiming = value; }
        public int TrackedPoints { get => trackedPoints; set => trackedPoints = value; }
        public int TotalPoints { get => totalPoints; set => totalPoints = value; }
        public int MissingPoints { get => missingPoints; set => missingPoints = value; }
        public double CostMatrixTiming { get => costMatrixTiming; set => costMatrixTiming = value; }
        public double EstimationDisplacementTiming { get => estimationDisplacementTiming; set => estimationDisplacementTiming = value; }
        public double EstimationExtrapolationTiming { get => estimationExtrapolationTiming; set => estimationExtrapolationTiming = value; }
        public double EstimationSpringTiming { get => estimationSpringTiming; set => estimationSpringTiming = value; }
    }
}
