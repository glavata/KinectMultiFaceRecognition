using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System.Linq;
using System.Data.SQLite;
using System;

namespace KinectMultiFaceRecognition
{
    public class FaceManager
    {
        private KinectSensor kinectSensor = null;

        private BodyFrameReader bodyFrameReader = null;

        private Body[] bodies = null;

        public FaceManager(KinectSensor sensor)
        {
            this.kinectSensor = sensor;

            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            this.bodyCount = this.kinectSensor.BodyFrameSource.BodyCount;

            this.bodies = new Body[this.bodyCount];

            this.faceTrackers = new FaceTracker[this.bodyCount];

            for (int i = 0; i < this.bodyCount; i++)
            {
                this.faceTrackers[i] = new FaceTracker(this.kinectSensor, 0);
                //this.faceTrackers[i].Source.TrackingIdLost += HdFaceSource_TrackingIdLost;
            }
        }

        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    for (int i = 0; i < this.bodyCount; i++)
                    {                       
                        if (!this.faceTrackers[i].Source.IsTrackingIdValid)
                        {                                                  
                            if (this.bodies[i].IsTracked)
                            {
                                this.faceTrackers[i].TrackingId = this.bodies[i].TrackingId;
                                this.faceTrackers[i].StartCollecting();
                            }
                        }
                    }
                }
            }
        }

        public FaceTracker[] faceTrackers { get; set; }

        public int bodyCount { get; set; }

        private void HdFaceSource_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            var faceTracker = this.faceTrackers.FirstOrDefault(a => a.TrackingId == e.TrackingId);

            if (faceTracker != null)
            {
                faceTracker.ModelBuilder.Dispose();
                faceTracker.Source.TrackingId = 0;
            }
        }
    }
}
