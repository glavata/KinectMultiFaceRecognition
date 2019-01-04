using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
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
        Dictionary<ulong, FaceTracker> facialStates = new Dictionary<ulong, FaceTracker>(6);
        private FaceManager faceManager;
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        private DrawingEngine drawingEngine;
        IList<Body> _bodies;

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
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _width = _sensor.ColorFrameSource.FrameDescription.Width;
                _height = _sensor.ColorFrameSource.FrameDescription.Height;
                _pixels = new byte[_width * _height * 4];
                _bitmap = new WriteableBitmap(_width, _height, 96.0, 96.0, PixelFormats.Bgra32, null);

                this.facialStates = new Dictionary<ulong, FaceTracker>();

                this.faceManager = new FaceManager(_sensor);

                this.drawingEngine = new DrawingEngine(_sensor, canvasDraw);

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

            WriteableBitmap lastBitmap = null;

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
                    lastBitmap = _bitmap;
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

            drawingEngine.DrawLatestFaceResults(this.faceManager, lastBitmap);
        }

    }
}
