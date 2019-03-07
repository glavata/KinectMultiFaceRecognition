using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.ML.MlEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static KinectMultiFaceRecognition.FaceTracker;
using static KinectMultiFaceRecognition.RecognitionEngine;

namespace KinectMultiFaceRecognition
{
    public class FaceRecognizer
    {
        private bool initialDataProcessed;
        private readonly double topPercentage = 1;
        private bool[] viewsGathered;
        private int[] framesTaken;

        private List<Bitmap> collectedFrames;

        private int totalFrames;


        public FaceRecognizer()
        {
            this.collectedFrames = new List<Bitmap>(25);

            this.framesTaken = new int[5] { 0, 0, 0, 0, 0 };

            this.viewsGathered = new bool[5];
            this.initialDataProcessed = false;
            this.Trained = false;
        }

        public FaceRecognizer(Matrix<float> wVec, Matrix<float> eigenVec, Matrix<float> meanVec, int imgWidth, int imgHeight, string name, SVM svm)
        {
            this.CompVectors = wVec;
            this.ImageWidth = imgWidth;
            this.ImageHeight = imgHeight;
            this.Name = name;
            this.initialDataProcessed = true;
            this.EigenVectors = eigenVec;
            this.MeanVector = meanVec;

            if(svm != null)
            {
                this.Trained = true;
                this.Svm = svm;
            }
            else
            {
                this.Trained = false;
            }
        }

        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }

        public string Name { get; set; }

        public Matrix<float> CompVectors { get; private set; }

        public SVM Svm { get; private set; }

        public bool Trained { get; private set; }
    
        public Matrix<float> EigenVectors { get; private set; }

        public Matrix<float> MeanVector { get; private set; }

        private void ProcessInitialImages()
        {
            if(initialDataProcessed)
            {
                throw new ArgumentException("Initial data already processed");
            }

            if (collectedFrames == null || collectedFrames.Count == 0)
            {
                throw new ArgumentNullException("Empty image list");
            }

            initialDataProcessed = false;

            ImageWidth = collectedFrames[0].Width;
            ImageHeight = collectedFrames[0].Height;

            int imgCount = collectedFrames.Count;
            int rowLength = ImageWidth * ImageHeight;

            Mat mergedImages = new Mat(0, rowLength, DepthType.Cv32F, 1);

            for (int i = 0; i < imgCount; i++)
            {
                Mat curVecImg = new Image<Gray, float>(collectedFrames[i]).Mat.Reshape(0, 1);
                CvInvoke.VConcat(curVecImg, mergedImages, mergedImages);
            }

            Mat averageMat = new Mat();
            Mat outputEigenVec = new Mat();

            CvInvoke.PCACompute(mergedImages, averageMat, outputEigenVec, topPercentage);

            this.EigenVectors = new Matrix<float>(outputEigenVec.Rows, outputEigenVec.Cols);
            outputEigenVec.CopyTo(this.EigenVectors);

            this.MeanVector = new Matrix<float>(averageMat.Rows, averageMat.Cols);
            averageMat.CopyTo(this.MeanVector);

            Mat weighted = new Mat();

            CvInvoke.PCAProject(mergedImages, averageMat, outputEigenVec, weighted);
            this.CompVectors = new Matrix<float>(weighted.Rows, weighted.Cols);
            //CvInvoke.Normalize(weighted, CompVectors);
            weighted.CopyTo(CompVectors);

            initialDataProcessed = true;

            Name = DateTime.Now.ToString("yyyyMMddHHmmssffff");

            DatabaseManager.SaveRecognizer(this);         
        }

        public void Train(Matrix<float> wVecNo)
        {
            Matrix<int> trainClassesAll = new Matrix<int>(CompVectors.Rows + wVecNo.Rows, 1);
            trainClassesAll.GetRows(0, CompVectors.Rows, 1).SetValue(1);
            trainClassesAll.GetRows(CompVectors.Rows, CompVectors.Rows + wVecNo.Rows, 1).SetValue(2);

            Matrix<float> trainDataAll = new Matrix<float>(CompVectors.Rows + wVecNo.Rows, wVecNo.Cols);
            CvInvoke.VConcat(CompVectors, wVecNo, trainDataAll);
                
            SVM svmTmp = new SVM();

            TrainData data = new TrainData(trainDataAll, DataLayoutType.RowSample, trainClassesAll);
            
            svmTmp.TrainAuto(data);
            
            this.Svm = svmTmp;
            this.Trained = true;
            DatabaseManager.SaveSVM(this.Name, this.Svm.SaveToString());
        }

        public void PCACustom(Mat data, IInputOutputArray mean, IOutputArray eigenvectors)
        {

            Mat covar = new Mat();
            Mat Eval = new Mat();
            Mat Evec = new Mat();
            Mat meanOut = new Mat();

            CvInvoke.CalcCovarMatrix(data, covar, meanOut, CovarMethod.Rows | 
                                                           CovarMethod.Scrambled | 
                                                           CovarMethod.Scale,
                                                           DepthType.Cv32F);
            Matrix<float> covMtrx = new Matrix<float>(10000, 10000);
            covar.CopyTo(covMtrx);

            CvInvoke.Eigen(covar, Eval, Evec);
            mean = meanOut;

            Mat tmp_data = new Mat();
            CvInvoke.Repeat(meanOut, data.Rows / meanOut.Rows, data.Cols / meanOut.Cols, tmp_data);
            Mat tmp_mean = tmp_data;

            CvInvoke.Subtract(data, tmp_mean, tmp_mean);
            tmp_data = tmp_mean;

            Mat evects1 = new Mat(25, 10000, DepthType.Cv32F, 1);
            Mat tmpp = new Mat();
            CvInvoke.Gemm(Evec, tmp_data, 1, tmpp, 0, evects1, 0);

            eigenvectors = evects1;
            int i;
            for (i = 0; i < 25; i++)
            {
                Mat vec = evects1.Row(i);
                CvInvoke.Normalize(vec, vec);
            }

        }

        public float Predict(Bitmap bmp)
        {
            if(!initialDataProcessed)
            {
                throw new ArgumentNullException("NO INITIAL DATA");
            }

            Image<Gray, float> img = new Image<Gray, float>(bmp);
            Matrix<float> mtrx = new Matrix<float>(img.Height, img.Width);
            img.CopyTo(mtrx);
            Mat reshaped = mtrx.Reshape(1, 1).Mat;
            Matrix<float> mtrxReshaped = new Matrix<float>(reshaped.Rows, reshaped.Cols);
            reshaped.CopyTo(mtrxReshaped);

            Mat wVec = new Mat();
            CvInvoke.PCAProject(mtrxReshaped, this.MeanVector, this.EigenVectors, wVec);

            //Mat normalizedVec = new Mat();
            //CvInvoke.Normalize(wVec, normalizedVec);
            
            Mat res = new Mat();
            Matrix<float> normalizedVecMatrx = new Matrix<float>(wVec.Rows, wVec.Cols);
            wVec.CopyTo(normalizedVecMatrx);

            float resFloat = this.Svm.Predict(normalizedVecMatrx, res);
            Matrix<float> resMatrix = new Matrix<float>(res.Rows, res.Cols);
            res.CopyTo(resMatrix);


            return resMatrix[0,0];


        }

        public TrackerStatus AddFrame(Bitmap bmp, NeededView view)
        {
            collectedFrames.Add(bmp);
            ++totalFrames;

            if (++framesTaken[(int)view] == 5)
            {
                viewsGathered[(int)view] = true;
                if(totalFrames == 25)
                {
                    this.ProcessInitialImages();
                    return TrackerStatus.Trained;
                }
            }

            return TrackerStatus.InTraining;
        }

        public NeededView[] GetNeededViews()
        {
            return viewsGathered.Select((val, ind) => new { val, ind })
                                     .Where(a => a.val == false)
                                     .Select(x => (NeededView)x.ind).ToArray();
        }

    }
}
