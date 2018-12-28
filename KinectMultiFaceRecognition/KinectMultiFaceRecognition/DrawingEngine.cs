using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KinectMultiFaceRecognition
{
    public static class DrawingEngine
    {
        public static Color[] colorPoints = new Color[]{ Colors.YellowGreen,
                                                         Colors.DarkCyan,
                                                         Colors.HotPink,
                                                         Colors.Khaki,
                                                         Colors.Orchid,
                                                         Colors.Sienna};

        public static Color[] colorBones = new Color[]{ Colors.Purple,
                                                        Colors.Gold,
                                                        Colors.Lavender,
                                                        Colors.OrangeRed,
                                                        Colors.Brown,
                                                        Colors.GreenYellow};

        public static void DrawSkeleton(this Canvas canvas, KinectSensor sensor, Body body, int colorIndex)
        {
            if (body == null) return;

            foreach (Joint joint in body.Joints.Values)
            {
                canvas.DrawPoint(sensor, joint, colorPoints[colorIndex]);
            }

            canvas.DrawLine(sensor, body.Joints[JointType.Head], body.Joints[JointType.Neck], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.WristRight], body.Joints[JointType.HandRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft], colorBones[colorIndex]);
            canvas.DrawLine(sensor, body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight], colorBones[colorIndex]);
        }

        private static ColorSpacePoint ToColorSpacePoint(this Joint joint, KinectSensor sensor)
        {
            CameraSpacePoint jointPosition = joint.Position;
            ColorSpacePoint jointPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(jointPosition);
            return jointPoint;
        }

        public static void DrawPoint(this Canvas canvas, KinectSensor sensor, Joint joint, Color color)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;

            ColorSpacePoint jointPoint = joint.ToColorSpacePoint(sensor);

            if (jointPoint.X < 0 || jointPoint.X > canvas.ActualWidth ||
                jointPoint.Y < 0 || jointPoint.Y > canvas.ActualHeight)
            {
                return;
            }

            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(color)
            };

            Canvas.SetLeft(ellipse, jointPoint.X);
            Canvas.SetTop(ellipse, jointPoint.Y);

            canvas.Children.Add(ellipse);
        }

        public static void DrawLine(this Canvas canvas, KinectSensor sensor, Joint first, Joint second, Color color)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            ColorSpacePoint jointPointFirst = first.ToColorSpacePoint(sensor);
            ColorSpacePoint jointPointSecond = second.ToColorSpacePoint(sensor);

            if(jointPointFirst.X < 0 || jointPointFirst.X > canvas.ActualWidth ||
               jointPointFirst.Y < 0 || jointPointFirst.Y > canvas.ActualHeight)
            {
                return;
            }

            if (jointPointSecond.X < 0 || jointPointSecond.X > canvas.ActualWidth ||
                jointPointSecond.Y < 0 || jointPointSecond.Y > canvas.ActualHeight)
            {
                return;
            }


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

        /*
        public static void DrawFace(this Canvas canvas, FaceTracker state, KinectSensor sensor, int colorIndex)
        {
            var vertices = state.Model.CalculateVerticesForAlignment(state.Alignment);
            if (vertices.Count > 0)
            {
                for (int index = 0; index < vertices.Count; index++)
                {
                    Ellipse ellipse = new Ellipse
                    {
                        Width = 2.0,
                        Height = 2.0,
                        Fill = new SolidColorBrush(colorPoints[colorIndex])
                    };

                    CameraSpacePoint vertice = vertices[index];

                    //DepthSpacePoint point = sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertice);
                    DepthSpacePoint point = vertice.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

                    if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) return;

                    Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                    canvas.Children.Add(ellipse);
                }
            }

        }
        */

        public static void DrawInfo(this Canvas canvas, IList<Body> _bodies, Dictionary<ulong, FaceTracker> facialStates)
        {
            int top = 10;
            int colorsIndex = 0;

            foreach (var body in _bodies.Where(b => b.IsTracked))
            {
                TextBlock textBlockBodyInfo = new TextBlock();
                textBlockBodyInfo.Background = new SolidColorBrush(Colors.White);
                textBlockBodyInfo.Text = body.TrackingId.ToString();

                textBlockBodyInfo.Foreground = new SolidColorBrush(colorBones[colorsIndex]);
                textBlockBodyInfo.Width = 150;

                Canvas.SetLeft(textBlockBodyInfo, 80);
                Canvas.SetTop(textBlockBodyInfo, top);

                canvas.Children.Add(textBlockBodyInfo);


                if (facialStates != null)
                {
                    if (facialStates.ContainsKey(body.TrackingId))
                    {
                        TextBlock textBlockFaceInfo = new TextBlock();
                        textBlockFaceInfo.Background = new SolidColorBrush(Colors.White);
                        FaceTracker target = facialStates[body.TrackingId];

                        FaceModel model = target.Model;

                        if (model != null)
                        {





                            textBlockFaceInfo.Text = String.Format("{0:0.0000} {1:0.0000} {2:0.0000} {3:0.0000} {4:0.0000} {5:0.0000} {6:0.0000}",
                                                                  model.FaceShapeDeformations[FaceShapeDeformations.Forehead00],
                                                                  model.FaceShapeDeformations[FaceShapeDeformations.Nose00],
                                                                  model.FaceShapeDeformations[FaceShapeDeformations.Eyes00],
                                                                  model.FaceShapeDeformations[FaceShapeDeformations.Cheeks00],
                                                                  model.FaceShapeDeformations[FaceShapeDeformations.MouthBag01],
                                                                  model.FaceShapeDeformations[FaceShapeDeformations.Chin01],
                                                                  model.FaceShapeDeformations[FaceShapeDeformations.Mouth02]);
                            textBlockFaceInfo.Foreground = new SolidColorBrush(colorPoints[colorsIndex]);

                            Canvas.SetLeft(textBlockFaceInfo, 220);
                            Canvas.SetTop(textBlockFaceInfo, top);

                            canvas.Children.Add(textBlockFaceInfo);
                        }
                    }
                }


                colorsIndex++;
                top += 30;
            }


        }

    }
}
