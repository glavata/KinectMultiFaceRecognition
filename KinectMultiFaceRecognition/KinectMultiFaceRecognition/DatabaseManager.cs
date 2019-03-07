using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Microsoft.Kinect.Face;
using System.IO;
using System.Drawing;
using Microsoft.Win32;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Media;
using Emgu.CV;
using Emgu.Util;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Xml;
using Emgu.CV.ML;
using System.IO.Compression;

namespace KinectMultiFaceRecognition
{
    public static class DatabaseManager
    {

        private static readonly XmlSerializerNamespaces xmlNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
        private static readonly XmlWriterSettings xmlWsettings = new XmlWriterSettings() { OmitXmlDeclaration = true };
        private static readonly string xmlDatabase = "face_database.xml";

        public static List<RecognizedFace> AllFaces = new List<RecognizedFace>();

        private static SQLiteConnection sqliteConn = new SQLiteConnection();

        public static SQLiteConnection CreateConnection()
        {
            SQLiteConnection sqlite_conn;

            sqlite_conn = new SQLiteConnection("Data Source=database.db; Version = 3; New = True; Compress = True; ");

            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {

            }
            return sqlite_conn;
        }


        public static void CreateTable(SQLiteConnection conn)
        {

            SQLiteCommand sqlite_cmd;
            string createTableFaces = @"CREATE TABLE IF NOT EXISTS faces (id INTEGER PRIMARY KEY AUTOINCREMENT
            name VARCHAR(50), snapshot BLOB, json_feature_vector VARCHAR(500))";

            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = createTableFaces;
            sqlite_cmd.ExecuteNonQuery();

        }

        /*
        public static void InsertRecord(CroppedBitmap picture, IReadOnlyDictionary<FaceShapeDeformations, float> deformations)
        {
            JsonSerializer serializer = new JsonSerializer();

            string name = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            RecognizedFace face = new RecognizedFace() { Name = name, Deformations = deformations };
            string output = JsonConvert.SerializeObject(face);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filenameFaces, true))
            {
                file.WriteLine(output);
            }
            SaveCroppedBitmap(picture, name + ".jpg");

            // SQLiteBlob myBlob = 

        }
        */

        /*

        public static void SaveSingleBitmap(Bitmap bitmap)
        {
            string name = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            bitmap.Save(name + "AVG.jpeg",
                           System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        public static void SaveBitmap(List<Bitmap> bitmaps)
        {
            string name = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            var bitmap = new Bitmap(500, 500);

            var canvas = Graphics.FromImage(bitmap);
            int x = 0;
            int y = 0;
            canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
            int saveCounter = 1;
            for (int b = 0; b < bitmaps.Count(); b++)
            {
                canvas.DrawImage(bitmaps[b], x, y);

                x += 100;
                if(saveCounter % 5 == 0)
                {
                    y += 100;
                    x = 0;
                }
                saveCounter++;
            }

            canvas.Save();
               
            try
            {
                bitmap.Save(name + ".jpeg",
                            System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            catch (Exception ex) { }

        }

        public static void SaveImgSource(ImageSource bmp)
        {
            string name = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            var imageSource = bmp as BitmapImage;
            var stream = imageSource.StreamSource;
            Byte[] buffer = null;

            if (stream != null && stream.Length > 0)
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    buffer = br.ReadBytes((Int32)stream.Length);
                }
            }

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageSource));

            using (FileStream fileStream = new FileStream(name + "SCREENSHOT.png", FileMode.OpenOrCreate))
            {
                
                encoder.Save(fileStream);
                //imageBuffer = mstream.GetBuffer();
            }


        }
        */

        public static void SaveRecognizer(FaceRecognizer rec)
        {
            String filePath = "face_database.xml";
            XmlDocument xd = new XmlDocument();
            
            FileStream lfile = new FileStream(filePath, FileMode.Open);

            xd.Load(lfile);

            string matrixText = null;
            string eigenVec = null;
            string meanVec = null;
            
            XmlSerializer xsSubmit = new XmlSerializer(typeof(Matrix<float>));
            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww, xmlWsettings))
                {
                    xsSubmit.Serialize(writer, rec.CompVectors, xmlNs);
                    matrixText = sww.ToString();
                }
            }

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww, xmlWsettings))
                {
                    xsSubmit.Serialize(writer, rec.EigenVectors, xmlNs);
                    eigenVec = sww.ToString();
                }
            }

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww, xmlWsettings))
                {
                    xsSubmit.Serialize(writer, rec.MeanVector, xmlNs);
                    meanVec = sww.ToString();
                }
            }

            var recognizers = xd.GetElementsByTagName("Recognizers");
            XmlNode recognizersNode = null;
            if (recognizers.Count == 0)
            {
                recognizersNode = xd.CreateElement("Recognizers");
                xd.FirstChild.AppendChild(recognizersNode);
            }
            else
            {
                recognizersNode = recognizers[0];
            }

            XmlElement recEl = xd.CreateElement("Recognizer");
            XmlAttribute recAttr = xd.CreateAttribute("Name");
            recAttr.InnerText = rec.Name;

            XmlAttribute recAttrW = xd.CreateAttribute("Width");
            recAttrW.InnerText = rec.ImageWidth.ToString();

            XmlAttribute recAttrH = xd.CreateAttribute("Height");
            recAttrH.InnerText = rec.ImageHeight.ToString();

            XmlElement recVec = xd.CreateElement("CompVec");
            recVec.InnerXml = matrixText;

            XmlElement recEigenVec = xd.CreateElement("EigenVectors");
            recEigenVec.InnerXml = eigenVec;

            XmlElement recMeanVec = xd.CreateElement("MeanVector");
            recMeanVec.InnerXml = meanVec;


            recEl.Attributes.Append(recAttr);
            recEl.Attributes.Append(recAttrW);
            recEl.Attributes.Append(recAttrH);
            recEl.AppendChild(recVec);
            recEl.AppendChild(recEigenVec);
            recEl.AppendChild(recMeanVec);


            if (rec.Svm != null)
            {
                XmlSerializer serializerByte = new XmlSerializer(typeof(byte[]));
                string compressedXML;

                using (var swwByte = new StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(swwByte, xmlWsettings))
                    {
                        byte[] bytes = Zip(rec.Svm.SaveToString());
                        serializerByte.Serialize(writer, bytes, xmlNs);
                        compressedXML = swwByte.ToString();

                        XmlElement svmEl = xd.CreateElement("SVM");
                        svmEl.InnerXml = compressedXML;
                        recEl.AppendChild(svmEl);
                    }
                }
            }

            recognizersNode.AppendChild(recEl);
            
            lfile.Close();
            xd.Save(filePath);
        }
        
        public static List<FaceRecognizer> LoadRecognizers()
        {
            XmlDocument xd = new XmlDocument();
            FileStream lfile = new FileStream(xmlDatabase, FileMode.Open);
            xd.Load(lfile);

            XmlSerializer serializer = new XmlSerializer(typeof(Matrix<float>));

            var recognizers = xd.GetElementsByTagName("Recognizer");

            List<FaceRecognizer> recognizersLoaded = new List<FaceRecognizer>(recognizers.Count);

            foreach (XmlNode rec in recognizers)
            {
                string name = rec.Attributes["Name"].Value;
                SVM svm = null;

                if (rec["SVM"] != null)
                {
                    svm = new SVM();
                    XmlSerializer serializerByte = new XmlSerializer(typeof(byte[]));

                    using (var sr = new StringReader(rec["SVM"].InnerXml))
                    {
                        using (XmlReader reader = XmlReader.Create(sr))
                        {
                            byte[] bytes = (byte[])serializerByte.Deserialize(reader);
                            string SVMxml = Unzip(bytes);
                            svm.LoadFromString(SVMxml);
                        }
                    }
                }

                Matrix<float> compVec = null;
                Matrix<float> eigenVecs = null;
                Matrix<float> meanVec = null;

                if (rec["CompVec"] != null)
                {
                    using (var sr = new StringReader(rec["CompVec"].InnerXml))
                    {
                        using (XmlReader reader = XmlReader.Create(sr))
                        {
                            compVec = (Matrix<float>)serializer.Deserialize(reader);
                        }
                    }
                }

                if (rec["EigenVectors"] != null)
                {
                    using (var sr = new StringReader(rec["EigenVectors"].InnerXml))
                    {
                        using (XmlReader reader = XmlReader.Create(sr))
                        {
                            eigenVecs = (Matrix<float>)serializer.Deserialize(reader);
                        }
                    }
                }

                if (rec["MeanVector"] != null)
                {
                    using (var sr = new StringReader(rec["MeanVector"].InnerXml))
                    {
                        using (XmlReader reader = XmlReader.Create(sr))
                        {
                            meanVec = (Matrix<float>)serializer.Deserialize(reader);
                        }
                    }
                }


                if (compVec != null && eigenVecs != null && meanVec != null)
                {
                    FaceRecognizer newRec = new FaceRecognizer(compVec, eigenVecs, meanVec,
                                               Int32.Parse(rec.Attributes["Width"].Value),
                                               Int32.Parse(rec.Attributes["Height"].Value),
                                               name, svm);
                    recognizersLoaded.Add(newRec);
                }

            }

            lfile.Close();
            xd.Save(xmlDatabase);

            return recognizersLoaded;
        }

        public static void SaveSVM(string recName, string svmXML)
        {
            XmlDocument xd = new XmlDocument();
            FileStream lfile = new FileStream(xmlDatabase, FileMode.Open);
            xd.Load(lfile);

            var recognizers = xd.GetElementsByTagName("Recognizer");
            XmlNode targetRec = recognizers.Cast<XmlNode>().Where(a => a.Attributes["Name"].Value == recName).FirstOrDefault();

            if(targetRec != null)
            {
                XmlSerializer serializerByte = new XmlSerializer(typeof(byte[]));
                string compressedXML;

                using (var sww = new StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(sww, xmlWsettings))
                    {
                        byte[] bytes = Zip(svmXML);
                        serializerByte.Serialize(writer, bytes, xmlNs);
                        compressedXML = sww.ToString();
                    }
                }

                if (targetRec["SVM"] == null)
                {
                    XmlElement svmEl = xd.CreateElement("SVM");
                    svmEl.InnerXml = compressedXML;
                    targetRec.AppendChild(svmEl);
                }
                else
                {
                    targetRec["SVM"].InnerXml = compressedXML;
                }
            }

            lfile.Close();
            xd.Save(xmlDatabase);
        }

        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        private static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionLevel.Optimal))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        private static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

    }
}
