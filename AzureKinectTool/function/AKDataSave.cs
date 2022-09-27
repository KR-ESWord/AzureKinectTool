using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;

namespace AzureKinectTool.function
{
    public class AKDataSave
    {
        /*
         * Calibration
         */
        public void AKCalibration(string calibration_json, string cal_path)
        {
            File.WriteAllText(cal_path, calibration_json);
        }

        /*
         * JPEG Image
         */

        public void AKColorImage(string jpeg_path, WriteableBitmap jpeg_wbitmap)
        {
            try
            {
                using (FileStream stream = new FileStream(jpeg_path, FileMode.Create, FileAccess.ReadWrite))
                {
                    BitmapEncoder encoder = new JpegBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(jpeg_wbitmap));
                    encoder.Save(stream);
                }
            }
            catch
            {
                Thread.Sleep(5);
            }
        }

        /*
         * PNG Image
         */
        public void AKPNGImage(string png_path, WriteableBitmap png_wbitmap)
        {
            try
            {
                using (FileStream stream = new FileStream(png_path, FileMode.Create, FileAccess.ReadWrite))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(png_wbitmap));
                    encoder.Save(stream);
                }
            }
            catch
            {
                Thread.Sleep(5);
            }
        }

        /*
        * Joint
        */
        public void AKJointData(string annotation_json, string jd_path)
        {
            File.WriteAllText(jd_path, annotation_json);
        }

        /*
         * Video
         */
        public void AKVideo(double real_fps, string ci_path, string cv_path)
        {
            ProcessStartInfo cmd = new ProcessStartInfo();
            Process process = new Process();

            // Set CMD
            cmd.FileName = @"cmd";
            cmd.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.CreateNoWindow = true;
            cmd.UseShellExecute = false;
            cmd.RedirectStandardOutput = true;
            cmd.RedirectStandardInput = true;
            cmd.RedirectStandardError = true;

            process.EnableRaisingEvents = false;
            process.StartInfo = cmd;
            process.Start();

            // Run CMD Command
            string create_video_cmd = "ffmpeg -threads 4 -f image2 -r " + real_fps + " -i " + ci_path + " -c:v h264_nvenc " + cv_path;

            process.StandardInput.Write(create_video_cmd + Environment.NewLine);
            process.StandardInput.Close();
        }

        public void AKCaptureMeta()
        {

        }
    }
}
