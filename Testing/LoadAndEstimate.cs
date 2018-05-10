using Newtonsoft.Json;
using ScreenTracker.DataProcessing;
using System;

namespace ScreenTracker.Testing
{
    class LoadAndEstimate
    {
        JsonSerializerSettings settings;


        public LoadAndEstimate(string delete)
        {

            string type = "Displacement";
            //string type = "Extrapolation";



            using (System.IO.StreamReader fileInTin = new System.IO.StreamReader("C:\\Users\\MultiPeden\\Documents\\GitHub\\ScreenTrackerEvaluation\\results\\log\\center\\z-\\CameraSpacetrackingNullOut.txt", true))
            using (System.IO.StreamWriter fileOut = new System.IO.StreamWriter("C:\\Users\\MultiPeden\\Documents\\GitHub\\ScreenTrackerEvaluation\\results\\log\\center\\z-\\trackingNullOut.txt", false))
            {



                string str = fileInTin.ReadLine();
                double[][] first = JsonToDoubleArray2(str);


                ImageProcessing imageProcessing = new ImageProcessing(first, type);


                System.Threading.Thread.Sleep(2000);
                double[][] pointsOut = imageProcessing.cameraData.CameraToIR(first);

                double[][] pointsOutz = attachZcoord(pointsOut, first);

                string pointsOutJson = DoubleArrayToJson(pointsOutz);



                fileOut.WriteLine(pointsOutJson);


                while (!fileInTin.EndOfStream)
                {
                    str = fileInTin.ReadLine();
                    first = JsonToDoubleArray2(str);
                    pointsOut = imageProcessing.cameraData.CameraToIR(first);
                    pointsOutz = attachZcoord(pointsOut, first);
                    pointsOutJson = DoubleArrayToJson(pointsOutz);

                    fileOut.WriteLine(pointsOutJson);

                }

            }
        }

        private double[][] attachZcoord(double[][] screen, double[][] zs)
        {

            double[][] res = new double[screen.Length][];
            for (int i = 0; i < screen.Length; i++)
            {
                res[i] = new double[3]
                {
                                 screen[i][0],
                                              screen[i][1],
                                              zs[i][2]
                };
            }
            return res;
        }

        public LoadAndEstimate()
        {


            settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };



            double[][] first;

            string posDir = "center\\z-";

            using (System.IO.StreamReader fileInTin = new System.IO.StreamReader("C:\\Users\\MultiPeden\\Documents\\GitHub\\ScreenTrackerEvaluation\\results\\log\\" + posDir + "\\trackingNullOut.txt", true))
            {

                string str = fileInTin.ReadLine();
                first = JsonToDoubleArray2(str);
            }


            //string type = "Displacement";
            string type = "Extrapolation";


            ImageProcessing imageProcessing = new ImageProcessing(first, type);



            System.Threading.Thread.Sleep(2000);



            using (System.IO.StreamReader fileIn = new System.IO.StreamReader("C:\\Users\\MultiPeden\\Documents\\GitHub\\ScreenTrackerEvaluation\\results\\log\\" + posDir + "\\trackingNull.txt", true))
            using (System.IO.StreamWriter fileOut = new System.IO.StreamWriter("C:\\Users\\MultiPeden\\Documents\\GitHub\\ScreenTrackerEvaluation\\results\\log\\" + posDir + "\\tracking" + type + ".txt", false))
            {


                while (!fileIn.EndOfStream)
                {
                    string str = fileIn.ReadLine();



                    double[][] pointsIn = JsonToDoubleArray(str);

                    double[][] pointsOut = imageProcessing.TrackedDataEst(pointsIn);

                    string pointsOutJson = DoubleArrayToJson(pointsOut);

                    fileOut.WriteLine(pointsOutJson);



                }

            }


            Console.WriteLine("hejehej");
        }


        private string DoubleArrayToJson(double[][] points)
        {
            IRItems items = new IRItems
            {
                Items = new IRpoint[points.Length]
            };

            double[] point;
            for (int i = 0; i < points.Length; i++)
            {

                IRpoint irpoint = new IRpoint();
                point = points[i];
                irpoint.id = i;
                irpoint.visible = 1;
                irpoint.x = (float)point[0];
                irpoint.y = (float)point[1];
                irpoint.z = (float)point[2] * 1000;

                items.Items[i] = irpoint;
            }


            //    string oel = JsonConvert.DeSerializeObject<Items>(items, settings);

            return JsonConvert.SerializeObject(items);
        }


        private double[][] JsonToDoubleArray(string str)
        {
            IRItems items = JsonConvert.DeserializeObject<IRItems>(str, settings);
            double[][] points = new double[items.Items.Length][];
            double[] point;
            IRpoint iRPoint;

            for (int i = 0; i < items.Items.Length; i++)


            {

                iRPoint = items.Items[i];

                if (iRPoint.visible == 1)
                {
                    point = new double[3]
        {
                    iRPoint.x, iRPoint.y, iRPoint.z
        };
                    points[i] = point;
                }
                else
                {
                    points[i] = null;
                }


            }

            return points;
        }





        private double[][] JsonToDoubleArray2(string str)
        {
            IRItems items = JsonConvert.DeserializeObject<IRItems>(str, settings);
            double[][] points = new double[items.Items.Length][];
            double[] point;
            IRpoint iRPoint;

            for (int i = 0; i < items.Items.Length; i++)


            {

                iRPoint = items.Items[i];



                points[i] = new double[3]
    {
                    iRPoint.x, iRPoint.y, iRPoint.z
    };


            }

            return points;
        }
    }
}
