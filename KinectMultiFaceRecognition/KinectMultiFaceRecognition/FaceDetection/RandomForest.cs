using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMultiFaceRecognition.FaceDetection
{
    class LeafNode
    {
        public float Probability { get; set; } //Probability of belonging to a head

        public Mat Mean { get; set; } //Mean vector

        public float Trace { get; set; } //Trace of cov matr
    }

    class DecisionTree
    {



        public void LoadTree(string dir)
        {
            
        }
    }
}
