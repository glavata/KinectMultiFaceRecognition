using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KinectMultiFaceRecognition
{
    public static class BitmapExtensions
    {


        private enum RGB
        {
            B = 0,
            G = 1,
            R = 2
        }

        private static readonly List<int> BOUNDING_HIGH_DETAIL_FACE_POINTS = new List<int>
        {
            1245, 976, 977, 978, 1064, 1065, 1060, 1059, 1055, 1058, 1027, 988, 878, 879, 886, 889, 890,
            891, 295, 445, 250, 247, 245, 232, 335, 214, 462, 460, 537, 141, 548, 477, 478, 280, 36, 1254,
            1297, 37, 1073, 65, 429, 427, 57, (int)HighDetailFacePoints.LowerjawLeftend, 1309, 48, 41, 528,
            40, 533, 532, 530, 529, 531, 1039, 1038, 998, 1036, 1037, 1043, 1046, 1042, 1003, 1328,
            (int)HighDetailFacePoints.LowerjawRightend, 595, 581, 596, 1025, 1074, 1024, 1292
        };

        private static List<System.Drawing.Point> FaceBoundaryPoints(IReadOnlyList<CameraSpacePoint> vertices, CoordinateMapper mapper)
        {
            /*if (BOUNDING_HIGH_DETAIL_FACE_POINTS == null)
                BOUNDING_HIGH_DETAIL_FACE_POINTS = CalculateBoundingHighDefinitionFacePoints(vertices);*/

            return BOUNDING_HIGH_DETAIL_FACE_POINTS.Select(x => TranslatePoint(vertices[(int)x], mapper)).ToList();
        }


        /// <summary>
        /// Translates between kinect and drawing points
        /// </summary>
        private static System.Drawing.Point TranslatePoint(CameraSpacePoint point, CoordinateMapper mapper)
        {
            var colorPoint = mapper.MapCameraPointToColorSpace(point);
            return new System.Drawing.Point((int)colorPoint.X, (int)colorPoint.Y);
        }

        private static Bitmap PixelsToBitmap(byte[] buffer)
        {
            Bitmap bmap = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(new System.Drawing.Rectangle(0, 0, 1920, 1080), ImageLockMode.WriteOnly, bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;

            lock (buffer)
            {
                Marshal.Copy(buffer, 0, ptr, buffer.Length);
            }

            bmap.UnlockBits(bmapdata);
            return bmap;
        }

        public static Bitmap GetCroppedFace(IReadOnlyList<CameraSpacePoint> points, CoordinateMapper mapper, byte[] pixels)
        {

            List<System.Drawing.Point> colorSpaceFacePoints = FaceBoundaryPoints(points, mapper);
            // Create a path tracing the face and draw on the processed image
            var origPath = new GraphicsPath();

            foreach (var point in colorSpaceFacePoints)
            {
                origPath.AddLine(point, point);
            }

            origPath.CloseFigure();

            var minX = (int)origPath.PathPoints.Min(x => x.X);
            var maxX = (int)origPath.PathPoints.Max(x => x.X);
            var minY = (int)origPath.PathPoints.Min(x => x.Y);
            var maxY = (int)origPath.PathPoints.Max(x => x.Y);
            var width = maxX - minX;
            var height = maxY - minY;

            // Create a cropped path tracing the face...
            var croppedPath = new GraphicsPath();

            foreach (var point in colorSpaceFacePoints)
            {
                var croppedPoint = new System.Drawing.Point(point.X - minX, point.Y - minY);
                croppedPath.AddLine(croppedPoint, croppedPoint);
            }

            croppedPath.CloseFigure();

            // ...and create a cropped image to use for facial recognition
            var croppedBmp = new Bitmap(width, height);
            Bitmap colorSpaceBitmap = PixelsToBitmap(pixels);

            using (var croppedG = Graphics.FromImage(croppedBmp))
            {
                croppedG.FillRectangle(Brushes.Gray, 0, 0, width, height);
                croppedG.SetClip(croppedPath);
                croppedG.DrawImage(colorSpaceBitmap, minX * -1, minY * -1);
            }

            return croppedBmp;


        }


        public static Bitmap MakeGrayscale(this Bitmap original, int newWidth, int newHeight)
        {
            // Create a blank bitmap the desired size
            Bitmap newBitmap = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                // Create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                {
                    new float[] { .3f, .3f, .3f, 0, 0 },
                    new float[] { .59f, .59f, .59f, 0, 0 },
                    new float[] { .11f, .11f, .11f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });

                ImageAttributes attributes = new ImageAttributes();

                // Set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                // Fixes "ringing" around the borders...
                attributes.SetWrapMode(WrapMode.TileFlipXY);

                // Draw the original image on the new image using the grayscale color matrix
                g.CompositingMode = CompositingMode.SourceCopy;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(original, new Rectangle(0, 0, newWidth, newHeight), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            return newBitmap;
        }


        public static void HistogramEqualize(this Bitmap bitmap)
        {
            if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                throw new ArgumentException("Input bitmap must be 32bppargb!");

            int step;
            var rawData = bitmap.CopyBitmapToByteArray(out step);

            // Get the Lookup table for histogram equalization
            var histLut = HistogramEqualizationLut(rawData);

            for (int i = 0; i < rawData.Length; i += 4)
            {
                // Update pixels according to LUT
                rawData[i + (int)RGB.R] = (byte)histLut[(int)RGB.R, rawData[i + (int)RGB.R]];
                rawData[i + (int)RGB.G] = (byte)histLut[(int)RGB.G, rawData[i + (int)RGB.G]];
                rawData[i + (int)RGB.B] = (byte)histLut[(int)RGB.B, rawData[i + (int)RGB.B]];
            }

            // Copy bits back into the bitmap...
            var bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
            Marshal.Copy(rawData, 0, bits.Scan0, rawData.Length);
            bitmap.UnlockBits(bits);
        }

        private static int[,] ImageHistogram(byte[] rawData)
        {
            var result = new int[3, 256];

            for (int i = 0; i < rawData.Length; i += 4)
            {
                result[(int)RGB.R, rawData[i + (int)RGB.R]]++;
                result[(int)RGB.G, rawData[i + (int)RGB.G]]++;
                result[(int)RGB.B, rawData[i + (int)RGB.B]]++;
            }

            return result;
        }

        private static int[,] HistogramEqualizationLut(byte[] rawData)
        {
            // Get an image histogram - calculated values by R, G, B channels
            int[,] imageHist = ImageHistogram(rawData);

            // Create the lookup table
            int[,] imageLut = new int[3, 256];

            long sumr = 0;
            long sumg = 0;
            long sumb = 0;

            // Calculate the scale factor
            float scaleFactor = (float)(255.0 / (rawData.Length / 4));

            for (int i = 0; i < 256; i++)
            {
                sumr += imageHist[(int)RGB.R, i];
                int valr = (int)(sumr * scaleFactor);
                imageLut[(int)RGB.R, i] = valr > 255 ? 255 : valr;

                sumg += imageHist[(int)RGB.G, i];
                int valg = (int)(sumg * scaleFactor);
                imageLut[(int)RGB.G, i] = valg > 255 ? 255 : valg;

                sumb += imageHist[(int)RGB.B, i];
                int valb = (int)(sumb * scaleFactor);
                imageLut[(int)RGB.B, i] = valb > 255 ? 255 : valb;
            }

            return imageLut;
        }

        public static byte[] CopyGrayscaleBitmapToByteArray(this Bitmap bitmap, out int step)
        {
            var baseResult = bitmap.CopyBitmapToByteArray(out step);

            if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                step /= 4;
                byte[] result = new byte[step * bitmap.Height];

                for (int i = 0; i < result.Length; i++)
                    result[i] = baseResult[i * 4];

                return result;
            }

            return baseResult;
        }

        public static byte[] CopyBitmapToByteArray(this Bitmap bitmap, out int step)
        {
            var bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            step = bits.Stride;

            byte[] result = new byte[step * bitmap.Height];
            Marshal.Copy(bits.Scan0, result, 0, result.Length);
            bitmap.UnlockBits(bits);

            return result;
        }
    }
}
