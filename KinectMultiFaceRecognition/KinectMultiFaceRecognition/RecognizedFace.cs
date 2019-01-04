using Microsoft.Kinect.Face;
using System.Collections.Generic;

namespace KinectMultiFaceRecognition
{
    public class RecognizedFace
    {
        public string Name { get; set; }

        public IReadOnlyDictionary<FaceShapeDeformations, float> Deformations { get; set; }
    }
}
