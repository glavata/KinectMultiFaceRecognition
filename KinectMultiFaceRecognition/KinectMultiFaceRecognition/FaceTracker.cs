using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Threading.Tasks;

namespace KinectMultiFaceRecognition
{
    public class FaceTracker
    {
        private readonly int threshold = 20;

        private ulong trackingId;

        public FaceTracker(KinectSensor sensor, ulong trackingId)
        {
            this.Source = new HighDefinitionFaceFrameSource(sensor);
            this.Reader = this.Source.OpenReader();
            this.Alignment = new FaceAlignment();
            this.TrackingId = trackingId;

            this.CollectionCompleted = false;         
            this.CollectionEventCalled = false;

            this.ScreenshotTaken = false;
            this.Model = new FaceModel();
            this.FaceBox = new FaceBoundingBox();

            this.Name = null;
        }

        public HighDefinitionFaceFrameSource Source { get; set; }

        public HighDefinitionFaceFrameReader Reader { get; set; }

        public FaceAlignment Alignment { get; set; }

        public FaceModelBuilder ModelBuilder { get; set; }

        public FaceModel Model { get; set; }

        public FaceBoundingBox FaceBox { get; set;}

        public string Name { get; set; }

        public bool CollectionCompleted { get; private set; }

        public bool CollectionEventCalled { get; private set; }

        public bool ScreenshotTaken { get; set; }
        
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
            var modelData = e.ModelData;
            Task.Factory.StartNew(() => CreateFaceModel(modelData));
            this.ModelBuilder.CollectionCompleted -= this.HdFaceBuilder_CollectionCompleted;

            this.CollectionEventCalled = true;
        }

        private void CreateFaceModel(FaceModelData data)
        {
            this.Model = data.ProduceFaceModel();         
            this.ModelBuilder.Dispose();
            this.ModelBuilder = null;

            this.CollectionCompleted = true;

            if(this.Name == null)
            {
                foreach (var face in DatabaseManager.AllFaces)
                {
                    double diff = 0;
                    foreach(FaceShapeDeformations fs in Enum.GetValues(typeof(FaceShapeDeformations)))
                    {
                        if(fs != FaceShapeDeformations.Count)
                        {
                            diff += Math.Abs(face.Deformations[fs] - this.Model.FaceShapeDeformations[fs]);
                        }
                        
                    }

                    if(diff < threshold)
                    {
                        this.Name = face.Name;
                        break;
                    }
                }
            }
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
