using Emgu.CV;
using Microsoft.Kinect.Face;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;


namespace KinectMultiFaceRecognition
{
    public class RecognitionEngine
    {
        public enum NeededView
        {
            None = -1,
            Left = 0,
            Front = 1,
            Right = 2,
            Up = 3,
            Down = 4
        };

        private readonly double[,,] ViewMargins =
        {
            { {-0.05, 0.05 }, { 0.075, 0.15} }, //LEFT
            { {-0.05, 0.05 }, { -0.05, 0.05} }, //FRONT
            { {-0.05, 0.05 }, { -0.15, -0.075} }, //RIGHT
            { {0.075, 0.15 }, { -0.05, 0.05} }, //UP
            { {-0.15, -0.075 }, { -0.05, 0.05} }, //DOWN
        };

        private List<FaceRecognizer> recognizers = new List<FaceRecognizer>();

        public RecognitionEngine()
        {
            recognizers = DatabaseManager.LoadRecognizers();
            this.RetrainRecognizers();
        }


        public void UpdateRecognizer(IReadOnlyList<CameraSpacePoint> vertices, FaceTracker tracker, CoordinateMapper mapper, byte[] pixels)
        {
            if(tracker.Status == FaceTracker.TrackerStatus.NotRecognized)
            {
                tracker.StartTraining();
            }

            if (tracker.Status == FaceTracker.TrackerStatus.InRecognition)
            {
                NeededView view = CheckFrameDirection(tracker.Alignment.FaceOrientation, NeededView.Front);

                if (view == NeededView.Front)
                {
                    Bitmap croppedBmp = BitmapExtensions.GetCroppedFace(vertices, mapper, pixels);
                    var grayBmp = croppedBmp.MakeGrayscale(100, 100);
                    string name = ClassifyFrame(grayBmp);
                    tracker.Recognizer.Name = name;
                    tracker.FinishRecognition(name == null ? false : true);
                }

            }
            else if (tracker.Status == FaceTracker.TrackerStatus.InTraining)
            {
                NeededView[] nViews = tracker.Recognizer.GetNeededViews();
         
                NeededView view = CheckFrameDirection(tracker.Alignment.FaceOrientation, nViews);
                if (view != NeededView.None)
                {
                    Bitmap croppedBmp = BitmapExtensions.GetCroppedFace(vertices, mapper, pixels);
                    var grayBmp = croppedBmp.MakeGrayscale(100, 100);

                    if(tracker.Recognizer.AddFrame(grayBmp, view) == FaceTracker.TrackerStatus.Trained)
                    {
                        recognizers.Add(tracker.Recognizer);
                        tracker.FinishTraining();
                        RetrainRecognizers();
                    }
                }
            }
        }



        private NeededView CheckFrameDirection( Vector4 orientation, params NeededView[] views)
        {

            double orX = Math.Round(orientation.X, 2);
            double orY = Math.Round(orientation.Y, 2);
            double orZ = Math.Round(orientation.Z, 2);

            if (orZ <= 0.015 && orZ >= -0.015)
            {
                for (int i = 0; i < views.Length; i++)
                {
                    int num = (int)views[i];
                    if (orX >= ViewMargins[num, 0, 0] && orX <= ViewMargins[num, 0, 1] &&
                        orY >= ViewMargins[num, 1, 0] && orY <= ViewMargins[num, 1, 1])
                    {
                        return views[i];
                    }
                }
            }

            return NeededView.None;
        }


        private string ClassifyFrame(Bitmap bmp)
        {
            IEnumerable<FaceRecognizer> tRec = this.recognizers.Where(a => a.Trained);
            int recCount = tRec.Count();
            Dictionary<string, int> votes = new Dictionary<string, int>(recCount);

            foreach (var recg in tRec)
            {
                int correct = recg.Predict(bmp) == 1 ? 1 : 0;

                if (!votes.ContainsKey(recg.Name))
                {
                    votes[recg.Name] = correct;
                }
                else
                {
                    votes[recg.Name] += correct;
                }
            }

            string result = votes.OrderByDescending(a => a.Value).FirstOrDefault().Key;

            return result;
        }

        private void RetrainRecognizers()
        {
            if(this.recognizers.Count > 1)
            {
                foreach (var rec in this.recognizers)
                {
                    Matrix<float>[] compVecNo = this.recognizers.Where(a => a != rec).Select(b => b.CompVectors).ToArray();

                    Mat mergedVec = new Mat(0, 25, Emgu.CV.CvEnum.DepthType.Cv32F, 1);

                    for (int i = 0; i < compVecNo.Length; i++)
                    {
                        CvInvoke.VConcat(compVecNo[i], mergedVec, mergedVec);
                    }

                    Matrix<float> compVecNoOut = new Matrix<float>(mergedVec.Rows, mergedVec.Cols);
                    mergedVec.CopyTo(compVecNoOut);

                    rec.Train(compVecNoOut);

                }
            }
        }

    }
}
