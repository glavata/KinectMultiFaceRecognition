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

namespace KinectMultiFaceRecognition
{
    public static class DatabaseManager
    {

        private static SQLiteConnection sqliteConn;

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

        public static void InsertRecord(CroppedBitmap picture, IReadOnlyDictionary<FaceShapeDeformations, float> deformations)
        {
            JsonSerializer serializer = new JsonSerializer();

            string name = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            RecognizedFace face = new RecognizedFace() { Name = name, Deformations = deformations };
            string output = JsonConvert.SerializeObject(face);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(name + ".face", true))
            {
                file.WriteLine(output);
            }
            SaveCroppedBitmap(picture, name + ".jpg");

            // SQLiteBlob myBlob = 

        }

        public static void SaveCroppedBitmap(CroppedBitmap image, string path)
        {
            FileStream mStream = new FileStream(path, FileMode.Create);
            JpegBitmapEncoder jEncoder = new JpegBitmapEncoder();
            jEncoder.Frames.Add(BitmapFrame.Create(image));
            jEncoder.Save(mStream);
        }


    }
}
