using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;

namespace KinectMultiFaceRecognition
{
    public class FaceTracker
    {
        private ulong trackingId;

        public FaceTracker(KinectSensor sensor, ulong trackingId)
        {
            this.Source = new HighDefinitionFaceFrameSource(sensor);
            this.Reader = this.Source.OpenReader();
            this.Alignment = new FaceAlignment();
            this.TrackingId = trackingId;
        }

        public HighDefinitionFaceFrameSource Source { get; set; }

        public HighDefinitionFaceFrameReader Reader { get; set; }

        public FaceAlignment Alignment { get; set; }

        public FaceModelBuilder ModelBuilder { get; set; }

        public FaceModel Model { get; set; }

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

        public void StartCollecting()
        {
            this.StopCollecting();

            this.ModelBuilder = this.Source.OpenModelBuilder(FaceModelBuilderAttributes.None);

            this.ModelBuilder.BeginFaceDataCollection();

            this.ModelBuilder.CollectionCompleted += this.HdFaceBuilder_CollectionCompleted;
        }

        private void HdFaceBuilder_CollectionCompleted(object sender, FaceModelBuilderCollectionCompletedEventArgs e)
        {
            //TODO: MESSAGE

            var modelData = e.ModelData;

            this.Model = modelData.ProduceFaceModel();

            this.ModelBuilder.Dispose();
            this.ModelBuilder = null;

            //The kinect is done preparing here.
        }

        private void StopCollecting()
        {
            if (this.ModelBuilder != null)
            {
                this.ModelBuilder.Dispose();
                this.ModelBuilder = null;
            }
        }
    }
}
