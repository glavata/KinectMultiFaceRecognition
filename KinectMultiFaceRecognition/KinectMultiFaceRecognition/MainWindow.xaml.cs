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
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
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

            int colorIndex = 0;
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvasDraw.Children.Clear();

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies.Where(b => b.IsTracked))
                    {
                        if (!this.facialStates.ContainsKey(body.TrackingId))
                        {
                            FaceTracker newStateEntry = new FaceTracker(_sensor, body.TrackingId,
                              this.OnTrackingIdLost);

                            this.facialStates[body.TrackingId] = newStateEntry;
                        }

                        canvasDraw.DrawSkeleton(_sensor, body, colorIndex++);
                    }
                }
            }

            colorIndex = 0;
            foreach (var stateEntry in this.facialStates)
            {
                using (var faceFrame = stateEntry.Value.Reader.AcquireLatestFrame())
                {
                    if ((faceFrame != null) && (faceFrame.FaceModel != null) && faceFrame.IsFaceTracked)
                    {
                        stateEntry.Value.Model = faceFrame.FaceModel;
                        faceFrame.GetAndRefreshFaceAlignmentResult(stateEntry.Value.Alignment);
                        canvasDraw.DrawFace(stateEntry.Value,_sensor, colorIndex++);
                    }
                }
            }

            if (_bodies != null)
                canvasDraw.DrawInfo(_bodies, facialStates);
        }

        void OnTrackingIdLost(object sender, TrackingIdLostEventArgs args)
        {
            if (this.facialStates.ContainsKey(args.TrackingId))
            {
                this.facialStates[args.TrackingId].Reader.Dispose();
                this.facialStates.Remove(args.TrackingId);
            }
        }

    }
}
