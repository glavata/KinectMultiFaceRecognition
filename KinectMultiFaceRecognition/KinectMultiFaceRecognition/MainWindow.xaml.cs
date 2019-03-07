using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectMultiFaceRecognition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FaceManager faceManager;
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        private DrawingEngine drawingEngine;
        IList<Body> _bodies;
        private RecognitionEngine recognitionEngine;

        private WriteableBitmap _bitmap = null;
        private byte[] _pixels = null;
        private int _width = 0;
        private int _height = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            CreateDataForHistogram();
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _width = _sensor.ColorFrameSource.FrameDescription.Width;
                _height = _sensor.ColorFrameSource.FrameDescription.Height;
                _pixels = new byte[_width * _height * 4];
                _bitmap = new WriteableBitmap(_width, _height, 96.0, 96.0, PixelFormats.Bgra32, null);

                this.recognitionEngine = new RecognitionEngine();
                this.faceManager = new FaceManager(_sensor, recognitionEngine);                

                this.drawingEngine = new DrawingEngine(_sensor, canvasDraw, recognitionEngine);

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body |
                                                            FrameSourceTypes.Color |
                                                            FrameSourceTypes.Depth |
                                                            FrameSourceTypes.Infrared);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                camera.Source = _bitmap;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        frame.CopyRawFrameDataToArray(_pixels);
                    }
                    else
                    {
                        frame.CopyConvertedFrameDataToArray(_pixels, ColorImageFormat.Bgra);
                    }
                    _bitmap.Lock();

                    Marshal.Copy(_pixels, 0, _bitmap.BackBuffer, _pixels.Length);
                    _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
                    _bitmap.Unlock();
                }
            }
            
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                bool dataReceived = false;
                if (frame != null)
                {                  
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];                  
                    frame.GetAndRefreshBodyData(_bodies);
                    dataReceived = true;
                }

                if(dataReceived)
                {
                    drawingEngine.DrawBodies(_bodies);                   
                }
            }

            UpdateManagers();
        }

        private void UpdateManagers()
        {
            int counter = 0;
            for (int i = 0; i < this.faceManager.bodyCount; i++)
            {
                if (this.faceManager.faceTrackers[i].Reader != null)
                {
                    HighDefinitionFaceFrame frame = this.faceManager.faceTrackers[i].Reader.AcquireLatestFrame();

                    if (frame != null && frame.FaceModel != null && frame.IsFaceTracked)
                    {
                        frame.GetAndRefreshFaceAlignmentResult(this.faceManager.faceTrackers[i].Alignment);
                        var vertices = this.faceManager.faceTrackers[i].Model.CalculateVerticesForAlignment(this.faceManager.faceTrackers[i].Alignment);

                        recognitionEngine.UpdateRecognizer(vertices, this.faceManager.faceTrackers[i], _sensor.CoordinateMapper, _pixels);
                        drawingEngine.DrawLatestFaceResults(vertices, this.faceManager.faceTrackers[i], i , counter);

                        counter++;

                    }
                }
            }
        }

        private void CreateDataForHistogram()
        {
            string[] lines = File.ReadAllLines("angles.txt");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("convDeg.txt"))
            {

                foreach (string line in lines)
                {
                    string[] splitLine = line.Trim().Split(' ');
                    float x = float.Parse(splitLine[0]);
                    float y = float.Parse(splitLine[1]);
                    float z = float.Parse(splitLine[2]);
                    float w = float.Parse(splitLine[3]);


                    float[] ypr = QuartenionToYPR(x, y, z, w);

                    int yDeg = (int)(ypr[0] * 180 / (float)Math.PI);
                    int pDeg = (int)(ypr[1] * 180 / (float)Math.PI);
                    int rDeg = (int)(ypr[2] * 180 / (float)Math.PI);

                    file.WriteLine(String.Format("{0} {1} {2}",yDeg,pDeg,rDeg));
                }

            }
        }

        private float[] QuartenionToYPR(float x, float y, float z, float w)
        {
            // roll (x-axis rotation)
            double sinr_cosp = +2.0 * (w * x + y * z);
            double cosr_cosp = +1.0 - 2.0 * (x * x + y * y);
            float roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = +2.0 * (w * y - z * x);
            float pitch = 0;
            if (Math.Abs(sinp) >= 1)
            {
                pitch = (float)(Math.PI / 2);
                if (sinp < 0)
                {
                    if (pitch > 0)
                    {
                        pitch = -pitch;
                    }
                }
                else
                {
                    if (pitch < 0)
                    {
                        pitch = -pitch;
                    }
                }
            }
            else
            {
                pitch = (float)Math.Asin(sinp);
            }

            
            // yaw (z-axis rotation)
            double siny_cosp = +2.0 * (w * z + x * y);
            double cosy_cosp = +1.0 - 2.0 * (y * y + z * z);
            float yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return new float[3] { yaw, pitch, roll };
        }


    }
}
