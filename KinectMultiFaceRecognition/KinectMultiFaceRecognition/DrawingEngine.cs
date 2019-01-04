using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace KinectMultiFaceRecognition
{
    public class DrawingEngine
    {

        private KinectSensor sensor;
        private Canvas canvas;

        public Color[] colorPoints = new Color[]{ Colors.YellowGreen, Colors.DarkCyan, Colors.HotPink,
                                                  Colors.Khaki, Colors.Orchid, Colors.Sienna};

        public Color[] colorBones = new Color[]{ Colors.Purple, Colors.Gold, Colors.Lavender,
                                                 Colors.OrangeRed, Colors.Brown, Colors.GreenYellow};

        public Color[] faceColors = new Color[] { Colors.Red, Colors.Orange, Colors.Green,
                                                  Colors.LightBlue, Colors.Indigo, Colors.Violet};

        private readonly int[,] boneIndices = { {3, 2},   {2, 20},   {20, 4},    {20, 8},
                                                {20, 1},  {4, 5},    {8, 9 },    {5, 6 },
                                                {9, 10},  {6, 7},    {10, 11},   {11, 23},
                                                {7, 21},  {21, 22},  {23, 24 },  {1, 0 },
                                                {0, 12 }, {0, 16 },  {12, 13 },  {16, 17 },
                                                {13, 14}, {17, 18 }, {14, 15 },  {18, 19 }
        };

        public DrawingEngine(KinectSensor sensor, Canvas canvas)
        {
            this.sensor = sensor;
            this.canvas = canvas;
        }

        public void DrawLatestFaceResults(FaceManager manager, WriteableBitmap lastBitmap)
        {
            int counter = 0;

            for (int i = 0; i < manager.bodyCount; i++)
            {
                if (manager.faceTrackers[i].Reader != null)
                {
                    HighDefinitionFaceFrame frame = manager.faceTrackers[i].Reader.AcquireLatestFrame();
                    
                    if (frame != null && frame.FaceModel != null && frame.IsFaceTracked)
                    {                    
                        DrawFaceBoundingBox(frame, manager.faceTrackers[i], faceColors[i]);
                        DrawFaceBodyInfo(manager.faceTrackers[i], faceColors[i], counter);
                        
                        if(!manager.faceTrackers[i].ScreenshotTaken && manager.faceTrackers[i].CollectionCompleted)
                        {
                            ScreenshotFace(manager.faceTrackers[i], lastBitmap);                            
                        }

                        counter++;
                    }
                }
            }
        }
        private void DrawFaceBodyInfo(FaceTracker tracker, Color color, int order)
        {
            int top = order * 30 + 10;

            TextBlock textBlockBodyInfo = new TextBlock();
            textBlockBodyInfo.Background = new SolidColorBrush(Colors.White);
            textBlockBodyInfo.Text = tracker.TrackingId.ToString();

            textBlockBodyInfo.Foreground = new SolidColorBrush(colorPoints[order]);
            textBlockBodyInfo.Width = 150;

            Canvas.SetLeft(textBlockBodyInfo, 80);
            Canvas.SetTop(textBlockBodyInfo, top);

            canvas.Children.Add(textBlockBodyInfo);
        }

        private void DrawFaceBoundingBox(HighDefinitionFaceFrame frame, FaceTracker tracker, Color color)
        {

            frame.GetAndRefreshFaceAlignmentResult(tracker.Alignment);
            var vertices = tracker.Model.CalculateVerticesForAlignment(tracker.Alignment);

            if (vertices.Count > 0)
            {

                CameraSpacePoint verticeTop = vertices[(int)HighDetailFacePoints.ForeheadCenter];
                ColorSpacePoint pointTop = sensor.CoordinateMapper.MapCameraPointToColorSpace(verticeTop);

                CameraSpacePoint verticeLeft = vertices[(int)HighDetailFacePoints.Leftcheekbone];
                ColorSpacePoint pointLeft = sensor.CoordinateMapper.MapCameraPointToColorSpace(verticeLeft);

                CameraSpacePoint verticeRight = vertices[(int)HighDetailFacePoints.Rightcheekbone];
                ColorSpacePoint pointRight = sensor.CoordinateMapper.MapCameraPointToColorSpace(verticeRight);

                CameraSpacePoint verticeBottom = vertices[(int)HighDetailFacePoints.ChinCenter];
                ColorSpacePoint pointBottom = sensor.CoordinateMapper.MapCameraPointToColorSpace(verticeBottom);

                if (float.IsInfinity(pointTop.X) || float.IsInfinity(pointTop.Y)) return;
                if (float.IsInfinity(pointLeft.X) || float.IsInfinity(pointLeft.Y)) return;
                if (float.IsInfinity(pointRight.X) || float.IsInfinity(pointRight.Y)) return;
                if (float.IsInfinity(pointBottom.X) || float.IsInfinity(pointBottom.Y)) return;

                float posX = pointLeft.X;
                float posY = pointTop.Y;
                double width = Math.Abs(pointRight.X - pointLeft.X);
                double height = Math.Abs(pointTop.Y - pointBottom.Y);
                double lineSize = 5;

                Rectangle rect = CreateFaceBoxRectangle(color, lineSize, width, height);
                Canvas.SetLeft(rect, posX);
                Canvas.SetTop(rect, posY);

                canvas.Children.Add(rect);
                tracker.FaceBox.X = posX;
                tracker.FaceBox.Y = posY;
                tracker.FaceBox.Width = width;
                tracker.FaceBox.Height = height;

                if (!tracker.CollectionEventCalled)
                {
                    if (tracker.ModelBuilder.CollectionStatus.HasFlag(FaceModelBuilderCollectionStatus.FrontViewFramesNeeded))
                    {
                        Rectangle rectFront = CreateFaceBoxRectangle(color, 5, 30, 30);
                        Canvas.SetLeft(rectFront, posX + width / 2);
                        Canvas.SetTop(rectFront, posY + height / 2);

                        canvas.Children.Add(rectFront);
                    }

                    if (tracker.ModelBuilder.CollectionStatus.HasFlag(FaceModelBuilderCollectionStatus.LeftViewsNeeded))
                    {
                        Rectangle rectLeft = CreateFaceBoxRectangle(color, 5, 30, 30);
                        Canvas.SetLeft(rectLeft, posX - width / 2);
                        Canvas.SetTop(rectLeft, posY + height / 2);

                        canvas.Children.Add(rectLeft);
                    }

                    if (tracker.ModelBuilder.CollectionStatus.HasFlag(FaceModelBuilderCollectionStatus.RightViewsNeeded))
                    {
                        Rectangle rectRight = CreateFaceBoxRectangle(color, 5, 30, 30);
                        Canvas.SetLeft(rectRight, posX + width + width / 2);
                        Canvas.SetTop(rectRight, posY + height / 2);

                        canvas.Children.Add(rectRight);
                    }

                    if (tracker.ModelBuilder.CollectionStatus.HasFlag(FaceModelBuilderCollectionStatus.TiltedUpViewsNeeded))
                    {
                        Rectangle rectUp = CreateFaceBoxRectangle(color, 5, 30, 30);
                        Canvas.SetLeft(rectUp, posX + width / 2);
                        Canvas.SetTop(rectUp, posY - height / 2);

                        canvas.Children.Add(rectUp);
                    }
                }
            }
        }

        private Rectangle CreateFaceBoxRectangle(Color color, double strokeThickness, double width, double height, bool filled = false)
        {
            Rectangle rect = new Rectangle();
            rect.Width = width;
            rect.Height = height;
            rect.Stroke = new SolidColorBrush(color);
            rect.StrokeThickness = strokeThickness;
            rect.StrokeLineJoin = PenLineJoin.Round;
            if(!filled)
            {
                rect.Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
            else
            {
                rect.Fill = new SolidColorBrush(color);
            }
            return rect;
        }

        private void DrawFaceFeatures(HighDefinitionFaceFrame frame, FaceAlignment alignment, Color color)
        {          
            frame.GetAndRefreshFaceAlignmentResult(alignment);
            var vertices = frame.FaceModel.CalculateVerticesForAlignment(alignment);
            
            if (vertices.Count > 0)
            {
                for (int index = 0; index < vertices.Count; index++)
                {
                    Ellipse ellipse = new Ellipse
                    {
                        Width = 2.0,
                        Height = 2.0,
                        Fill = new SolidColorBrush(color)
                    };

                    CameraSpacePoint vertice = vertices[index];
                    ColorSpacePoint point = sensor.CoordinateMapper.MapCameraPointToColorSpace(vertice);

                    if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) return;

                    Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                    canvas.Children.Add(ellipse);
                }
            }          
        }

        public void DrawBodies(IList<Body> bodies)
        {
            int colorIndex = 0;
            canvas.Children.Clear();

            foreach (var body in bodies)
            {
                if(body.IsTracked)
                {
                    DrawSkeleton(body, colorIndex);
                }            
            }
        }

        private void DrawSkeleton(Body body, int colorIndex)
        {
            foreach (Joint joint in body.Joints.Values)
            {
                DrawPoint(joint, colorPoints[colorIndex]);
            }

            for(int i = 0; i < boneIndices.GetLength(0); i++)
            {
                DrawLine(body.Joints[(JointType)boneIndices[i,0]], 
                         body.Joints[(JointType)boneIndices[i,1]], colorBones[colorIndex]);
            }
        }

        private ColorSpacePoint ToColorSpacePoint(Joint joint)
        {
            CameraSpacePoint jointPosition = joint.Position;
            ColorSpacePoint jointPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(jointPosition);
            return jointPoint;
        }

        private void DrawPoint(Joint joint, Color color)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;

            ColorSpacePoint jointPoint = ToColorSpacePoint(joint);

            if (float.IsInfinity(jointPoint.X) || float.IsInfinity(jointPoint.Y)) return;
            
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(color)
            };

            Canvas.SetLeft(ellipse, jointPoint.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, jointPoint.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        public void DrawLine(Joint first, Joint second, Color color)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            ColorSpacePoint jointPointFirst = ToColorSpacePoint(first);
            ColorSpacePoint jointPointSecond = ToColorSpacePoint(second);

            if (float.IsInfinity(jointPointFirst.X) || float.IsInfinity(jointPointFirst.Y)) return;
            if (float.IsInfinity(jointPointSecond.X) || float.IsInfinity(jointPointSecond.Y)) return;

            Line line = new Line
            {
                X1 = jointPointFirst.X,
                Y1 = jointPointFirst.Y,
                X2 = jointPointSecond.X,
                Y2 = jointPointSecond.Y,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(color)
            };

            canvas.Children.Add(line);
        }


        public void ScreenshotFace(FaceTracker tracker, WriteableBitmap bitmap)
        {
            CroppedBitmap cb = new CroppedBitmap(bitmap, 
                new Int32Rect((int)tracker.FaceBox.X, (int)tracker.FaceBox.Y,
                (int)tracker.FaceBox.Width,
                (int)tracker.FaceBox.Height));

            DatabaseManager.InsertRecord(cb, tracker.Model.FaceShapeDeformations);

            tracker.ScreenshotTaken = true;

        }
    }
}
