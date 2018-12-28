using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;

namespace KinectMultiFaceRecognition
{
    public class FaceTracker
    {
        private HighDefinitionFaceFrameSource source = null;

        public FaceTracker(KinectSensor sensor, ulong trackingId, EventHandler<TrackingIdLostEventArgs> handler)
        {
            this.source = new HighDefinitionFaceFrameSource(sensor);//, trackingId, FACE_FEATURES);
            this.source.TrackingId = trackingId;
            this.source.TrackingIdLost += handler;
            this.Reader = this.source.OpenReader();
            this.Alignment = new FaceAlignment();
            this.Model = new FaceModel();
        }

        public FaceModel Model { get; set; }

        public HighDefinitionFaceFrameReader Reader { get; set; }

        public FaceAlignment Alignment { get; set; }

        public FaceFrameResult FaceResult { get; set; }
    }
}
