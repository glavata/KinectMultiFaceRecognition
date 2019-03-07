using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace KinectMultiFaceRecognition
{
    public class FaceTracker
    {


        public enum TrackerStatus
        {
            Idle = 0,
            InRecognition = 1,
            Recognized = 2,
            NotRecognized = 3,
            InTraining = 4,
            Trained = 5
        }

        private ulong trackingId;


        public FaceTracker(KinectSensor sensor, ulong trackingId)
        {
            this.Source = new HighDefinitionFaceFrameSource(sensor);
            this.Reader = this.Source.OpenReader();
            this.Alignment = new FaceAlignment();
            this.TrackingId = trackingId;
            this.Model = new FaceModel();
            this.FaceBox = new FaceBoundingBox();
            
        }

        public HighDefinitionFaceFrameSource Source { get; set; }

        public HighDefinitionFaceFrameReader Reader { get; set; }

        public FaceAlignment Alignment { get; set; }

        public FaceModelBuilder ModelBuilder { get; set; }

        public FaceModel Model { get; set; }

        public FaceBoundingBox FaceBox { get; set; }

        public ulong TrackingId
        {
            get
            {
                return this.trackingId;
            }
            set
            {
                this.trackingId = value;
                this.Source.TrackingId = value;
            }
        }

        public bool IsTrackingIdValid
        {
            get { return this.Source.IsTrackingIdValid; }
        }

        public void StartRecognition()
        {
            this.Status = TrackerStatus.InRecognition;
            this.Recognizer = new FaceRecognizer();
        }

        public FaceRecognizer Recognizer { get; private set; }

        public void StartTraining()
        {
            if(this.Status == TrackerStatus.NotRecognized)
            {
                this.Status = TrackerStatus.InTraining;
            }
        }

        public void FinishTraining()
        {
            if (this.Status == TrackerStatus.InTraining)
            {
                this.Status = TrackerStatus.Trained;
            }
        }

        public void FinishRecognition(bool successful)
        {
            if(this.Status == TrackerStatus.InRecognition)
            {
                this.Status = successful ? TrackerStatus.Recognized : TrackerStatus.NotRecognized;
            }
        }


        public TrackerStatus Status { get; private set; }

    }
}
