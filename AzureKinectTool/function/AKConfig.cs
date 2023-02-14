using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Azure Kinect SDK
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;

namespace AzureKinectTool.function
{
    public class AKConfig
    {
        // Azure Kinect Sensor Configuration
        public DeviceConfiguration SensorConfig(int dm_idx, int cf_idx, int cr_idx, int fr_idx)
        {
            DeviceConfiguration sensor_config = new DeviceConfiguration();

            // Depth Mode
            switch (dm_idx)
            {
                case 0:
                    sensor_config.DepthMode = DepthMode.NFOV_2x2Binned;
                    break;

                case 1:
                    sensor_config.DepthMode = DepthMode.NFOV_Unbinned;
                    break;

                case 2:
                    sensor_config.DepthMode = DepthMode.WFOV_2x2Binned;
                    break;

                case 3:
                    sensor_config.DepthMode = DepthMode.WFOV_Unbinned;
                    break;
            }

            // Color Format
            switch (cf_idx)
            {
                case 0:
                    sensor_config.ColorFormat = ImageFormat.ColorBGRA32;
                    break;

                case 1:
                    sensor_config.ColorFormat = ImageFormat.ColorMJPG;
                    break;

                case 2:
                    sensor_config.ColorFormat = ImageFormat.ColorNV12;
                    break;

                case 3:
                    sensor_config.ColorFormat = ImageFormat.ColorYUY2;
                    break;
            }

            // Color Resolution
            switch (cr_idx)
            {
                case 0:
                    sensor_config.ColorResolution = ColorResolution.R720p;
                    break;

                case 1:
                    sensor_config.ColorResolution = ColorResolution.R1080p;
                    break;

                case 2:
                    sensor_config.ColorResolution = ColorResolution.R1440p;
                    break;

                case 3:
                    sensor_config.ColorResolution = ColorResolution.R1536p;
                    break;

                case 4:
                    sensor_config.ColorResolution = ColorResolution.R2160p;
                    break;

                case 5:
                    sensor_config.ColorResolution = ColorResolution.R3072p;
                    break;
            }

            // Frame Rate
            switch (fr_idx)
            {
                case 0:
                    sensor_config.CameraFPS = FPS.FPS5;
                    break;

                case 1:
                    sensor_config.CameraFPS = FPS.FPS15;
                    break;

                case 2:
                    sensor_config.CameraFPS = FPS.FPS30;
                    break;
            }

            sensor_config.SynchronizedImagesOnly = true;

            return sensor_config;
        }

        // Azure Kinect Tracker Configuration
        public TrackerConfiguration TrackerConfig(int tm_idx, int om_idx, int so_idx, int gi_idx)
        {
            TrackerConfiguration tracker_config = new TrackerConfiguration();

            // Tracker Process Mode
            switch (tm_idx)
            {
                case 0:
                    tracker_config.ProcessingMode = TrackerProcessingMode.Cpu;
                    break;
                case 1:
                    tracker_config.ProcessingMode = TrackerProcessingMode.Cuda;
                    break;
                case 2:
                    tracker_config.ProcessingMode = TrackerProcessingMode.DirectML;
                    break;
                case 3:
                    tracker_config.ProcessingMode = TrackerProcessingMode.Gpu;
                    break;
                case 4:
                    tracker_config.ProcessingMode = TrackerProcessingMode.TensorRT;
                    break;
            }

            // Tracker ONNX Model
            switch (om_idx)
            {
                case 0:
                    tracker_config.ModelPath = "dnn_model_2_0_op11.onnx";
                    break;

                case 1:
                    tracker_config.ModelPath = "dnn_model_2_0_lite_op11.onnx";
                    break;
            }

            // Sensor Orientation
            switch (so_idx)
            {
                case 0:
                    tracker_config.SensorOrientation = SensorOrientation.Default;
                    break;

                case 1:
                    tracker_config.SensorOrientation = SensorOrientation.Clockwise90;
                    break;

                case 2:
                    tracker_config.SensorOrientation = SensorOrientation.Flip180;
                    break;

                case 3:
                    tracker_config.SensorOrientation = SensorOrientation.CounterClockwise90;
                    break;
            }

            // GPU Index
            tracker_config.GpuDeviceId = gi_idx;

            return tracker_config;
        }
    }
}
