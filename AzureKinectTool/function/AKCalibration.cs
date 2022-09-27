using System.Collections;
using System.Collections.Generic;
// JSON
using Newtonsoft.Json;
// Azure Kinect SDK
using Microsoft.Azure.Kinect.Sensor;

namespace AzureKinectTool.function
{
    public class AKCalibration
    {
        public string AKCalibrations(int id, string kinect_loc, string serial_num, Calibration calibration)
        {
            Dictionary<string, object> main_dict = new Dictionary<string, object>();

            // Extrinsics
            Dictionary<string, object> ext_dict = new Dictionary<string, object>();
            Dictionary<string, object> ext_params_dict = new Dictionary<string, object>();
            ArrayList ext_params_rotation = new ArrayList();
            ArrayList ext_params_translation = new ArrayList();

            int ext_rotation_cnt = calibration.ColorCameraCalibration.Extrinsics.Rotation.Length;
            for (int ext_r_idx = 0; ext_r_idx < ext_rotation_cnt; ext_r_idx++)
            {
                ext_params_rotation.Add(calibration.ColorCameraCalibration.Extrinsics.Rotation[ext_r_idx]);
            }
            int ext_translation_cnt = calibration.ColorCameraCalibration.Extrinsics.Translation.Length;
            for (int ext_t_idx = 0; ext_t_idx < ext_translation_cnt; ext_t_idx++)
            {
                ext_params_translation.Add(calibration.ColorCameraCalibration.Extrinsics.Translation[ext_t_idx]);
            }

            ext_params_dict.Add("rotation", ext_params_rotation);
            ext_params_dict.Add("translation", ext_params_translation);
            ext_params_dict.Add("metric_radius", "1.7"); // calibration.ColorCameraCalibration.MetricRadius.ToString()
            ext_params_dict.Add("resolution_width", calibration.ColorCameraCalibration.ResolutionWidth.ToString());
            ext_params_dict.Add("resolution_height", calibration.ColorCameraCalibration.ResolutionHeight.ToString());
            ext_dict.Add("parameters", ext_params_dict);

            // Instrinsics
            Dictionary<string, object> ins_dict = new Dictionary<string, object>();
            Dictionary<string, object> ins_params_dict = new Dictionary<string, object>();
            Dictionary<string, object> ins_decrip_dict = new Dictionary<string, object>();
            Dictionary<string, object> color_params_dict = new Dictionary<string, object>();
            Dictionary<string, object> depth_params_dict = new Dictionary<string, object>();

            ins_decrip_dict.Add("px", "principal point in image x");
            ins_decrip_dict.Add("py", "principal point in image y");
            ins_decrip_dict.Add("fx", "focal length x");
            ins_decrip_dict.Add("fy", "focal length y");
            ins_decrip_dict.Add("k1", "k1 radial distortion coefficient");
            ins_decrip_dict.Add("k2", "k2 radial distortion coefficient");
            ins_decrip_dict.Add("k3", "k3 radial distortion coefficient");
            ins_decrip_dict.Add("k4", "k4 radial distortion coefficient");
            ins_decrip_dict.Add("k5", "k5 radial distortion coefficient");
            ins_decrip_dict.Add("k6", "k6 radial distortion coefficient");
            ins_decrip_dict.Add("cx", "center of distortion in Z=1 plane, x (only used for Rational6KT");
            ins_decrip_dict.Add("cy", "center of distortion in Z=1 plane, y (only used for Rational6KT)");
            ins_decrip_dict.Add("p2", "tangential distortion coefficient 2");
            ins_decrip_dict.Add("p1", "tangential distortion coefficient 1");
            ins_decrip_dict.Add("zero", "zero");

            color_params_dict.Add("px", calibration.ColorCameraCalibration.Intrinsics.Parameters[0].ToString());
            color_params_dict.Add("py", calibration.ColorCameraCalibration.Intrinsics.Parameters[1].ToString());
            color_params_dict.Add("fx", calibration.ColorCameraCalibration.Intrinsics.Parameters[2].ToString());
            color_params_dict.Add("fy", calibration.ColorCameraCalibration.Intrinsics.Parameters[3].ToString());
            color_params_dict.Add("k1", calibration.ColorCameraCalibration.Intrinsics.Parameters[4].ToString());
            color_params_dict.Add("k2", calibration.ColorCameraCalibration.Intrinsics.Parameters[5].ToString());
            color_params_dict.Add("k3", calibration.ColorCameraCalibration.Intrinsics.Parameters[6].ToString());
            color_params_dict.Add("k4", calibration.ColorCameraCalibration.Intrinsics.Parameters[7].ToString());
            color_params_dict.Add("k5", calibration.ColorCameraCalibration.Intrinsics.Parameters[8].ToString());
            color_params_dict.Add("k6", calibration.ColorCameraCalibration.Intrinsics.Parameters[9].ToString());
            color_params_dict.Add("cx", calibration.ColorCameraCalibration.Intrinsics.Parameters[10].ToString());
            color_params_dict.Add("cy", calibration.ColorCameraCalibration.Intrinsics.Parameters[11].ToString());
            color_params_dict.Add("p2", calibration.ColorCameraCalibration.Intrinsics.Parameters[12].ToString());
            color_params_dict.Add("p1", calibration.ColorCameraCalibration.Intrinsics.Parameters[13].ToString());
            color_params_dict.Add("zero", calibration.ColorCameraCalibration.Intrinsics.Parameters[14].ToString());

            depth_params_dict.Add("px", calibration.DepthCameraCalibration.Intrinsics.Parameters[0].ToString());
            depth_params_dict.Add("py", calibration.DepthCameraCalibration.Intrinsics.Parameters[1].ToString());
            depth_params_dict.Add("fx", calibration.DepthCameraCalibration.Intrinsics.Parameters[2].ToString());
            depth_params_dict.Add("fy", calibration.DepthCameraCalibration.Intrinsics.Parameters[3].ToString());
            depth_params_dict.Add("k1", calibration.DepthCameraCalibration.Intrinsics.Parameters[4].ToString());
            depth_params_dict.Add("k2", calibration.DepthCameraCalibration.Intrinsics.Parameters[5].ToString());
            depth_params_dict.Add("k3", calibration.DepthCameraCalibration.Intrinsics.Parameters[6].ToString());
            depth_params_dict.Add("k4", calibration.DepthCameraCalibration.Intrinsics.Parameters[7].ToString());
            depth_params_dict.Add("k5", calibration.DepthCameraCalibration.Intrinsics.Parameters[8].ToString());
            depth_params_dict.Add("k6", calibration.DepthCameraCalibration.Intrinsics.Parameters[9].ToString());
            depth_params_dict.Add("cx", calibration.DepthCameraCalibration.Intrinsics.Parameters[10].ToString());
            depth_params_dict.Add("cy", calibration.DepthCameraCalibration.Intrinsics.Parameters[11].ToString());
            depth_params_dict.Add("p2", calibration.DepthCameraCalibration.Intrinsics.Parameters[12].ToString());
            depth_params_dict.Add("p1", calibration.DepthCameraCalibration.Intrinsics.Parameters[13].ToString());
            depth_params_dict.Add("zero", calibration.DepthCameraCalibration.Intrinsics.Parameters[14].ToString());

            ins_params_dict.Add("color", color_params_dict);
            ins_params_dict.Add("depth", depth_params_dict);
            ins_dict.Add("description", ins_decrip_dict);
            ins_dict.Add("parameters", ins_params_dict);

            // Convert Mode
            ArrayList cm_list = new ArrayList();
            Dictionary<string, object> d2c_dict = new Dictionary<string, object>();
            Dictionary<string, object> d2g_dict = new Dictionary<string, object>();
            Dictionary<string, object> d2a_dict = new Dictionary<string, object>();
            Dictionary<string, object> c2d_dict = new Dictionary<string, object>();
            Dictionary<string, object> c2g_dict = new Dictionary<string, object>();
            Dictionary<string, object> c2a_dict = new Dictionary<string, object>();
            Dictionary<string, object> g2d_dict = new Dictionary<string, object>();
            Dictionary<string, object> g2c_dict = new Dictionary<string, object>();
            Dictionary<string, object> g2a_dict = new Dictionary<string, object>();
            Dictionary<string, object> a2d_dict = new Dictionary<string, object>();
            Dictionary<string, object> a2c_dict = new Dictionary<string, object>();
            Dictionary<string, object> a2g_dict = new Dictionary<string, object>();

            // Depth to Color
            Extrinsics d2c_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Depth, (int)CalibrationDeviceType.Color);
            ArrayList d2c_params_rotation = GetExtRotation(d2c_ext);
            ArrayList d2c_params_translation = GetExtTranslation(d2c_ext);
            d2c_dict.Add("convert_id", "d2c");
            d2c_dict.Add("discription", "depth to color");
            d2c_dict.Add("rotation", d2c_params_rotation);
            d2c_dict.Add("translation", d2c_params_translation);

            // Depth to Gyro
            Extrinsics d2g_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Depth, (int)CalibrationDeviceType.Gyro);
            ArrayList d2g_params_rotation = GetExtRotation(d2g_ext);
            ArrayList d2g_params_translation = GetExtTranslation(d2g_ext);
            d2g_dict.Add("convert_id", "d2g");
            d2g_dict.Add("discription", "depth to gyro");
            d2g_dict.Add("rotation", d2g_params_rotation);
            d2g_dict.Add("translation", d2g_params_translation);

            // Depth to Accel
            Extrinsics d2a_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Depth, (int)CalibrationDeviceType.Accel);
            ArrayList d2a_params_rotation = GetExtRotation(d2a_ext); ;
            ArrayList d2a_params_translation = GetExtTranslation(d2a_ext);
            d2a_dict.Add("convert_id", "d2a");
            d2a_dict.Add("discription", "depth to accel");
            d2a_dict.Add("rotation", d2a_params_rotation);
            d2a_dict.Add("translation", d2a_params_translation);

            // Colr to Depth
            Extrinsics c2d_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Color, (int)CalibrationDeviceType.Depth);
            ArrayList c2d_params_rotation = GetExtRotation(c2d_ext);
            ArrayList c2d_params_translation = GetExtTranslation(c2d_ext);
            c2d_dict.Add("convert_id", "c2d");
            c2d_dict.Add("discription", "color to depth");
            c2d_dict.Add("rotation", c2d_params_rotation);
            c2d_dict.Add("translation", c2d_params_translation);

            // Color to Gyro
            Extrinsics c2g_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Color, (int)CalibrationDeviceType.Gyro);
            ArrayList c2g_params_rotation = GetExtRotation(c2g_ext);
            ArrayList c2g_params_translation = GetExtTranslation(c2g_ext);
            c2g_dict.Add("convert_id", "c2g");
            c2g_dict.Add("discription", "color to gyro");
            c2g_dict.Add("rotation", c2g_params_rotation);
            c2g_dict.Add("translation", c2g_params_translation);

            // Color to Accel
            Extrinsics c2a_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Color, (int)CalibrationDeviceType.Accel);
            ArrayList c2a_params_rotation = GetExtRotation(c2a_ext); ;
            ArrayList c2a_params_translation = GetExtTranslation(c2a_ext);
            c2a_dict.Add("convert_id", "c2a");
            c2a_dict.Add("discription", "color to accel");
            c2a_dict.Add("rotation", c2a_params_rotation);
            c2a_dict.Add("translation", c2a_params_translation);

            // Gyro to Color
            Extrinsics g2c_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Gyro, (int)CalibrationDeviceType.Color);
            ArrayList g2c_params_rotation = GetExtRotation(g2c_ext);
            ArrayList g2c_params_translation = GetExtTranslation(g2c_ext);
            g2c_dict.Add("convert_id", "g2c");
            g2c_dict.Add("discription", "gyro to color");
            g2c_dict.Add("rotation", g2c_params_rotation);
            g2c_dict.Add("translation", g2c_params_translation);

            // Gyro to Depth
            Extrinsics g2d_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Gyro, (int)CalibrationDeviceType.Depth);
            ArrayList g2d_params_rotation = GetExtRotation(g2d_ext);
            ArrayList g2d_params_translation = GetExtTranslation(g2d_ext);
            g2d_dict.Add("convert_id", "g2d");
            g2d_dict.Add("discription", "gyro to depth");
            g2d_dict.Add("rotation", g2d_params_rotation);
            g2d_dict.Add("translation", g2d_params_translation);

            // Gyro to Accel
            Extrinsics g2a_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Gyro, (int)CalibrationDeviceType.Accel);
            ArrayList g2a_params_rotation = GetExtRotation(g2a_ext); ;
            ArrayList g2a_params_translation = GetExtTranslation(g2a_ext);
            g2a_dict.Add("convert_id", "g2a");
            g2a_dict.Add("discription", "gyro to accel");
            g2a_dict.Add("rotation", g2a_params_rotation);
            g2a_dict.Add("translation", g2a_params_translation);

            // Accel to Color
            Extrinsics a2c_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Accel, (int)CalibrationDeviceType.Color);
            ArrayList a2c_params_rotation = GetExtRotation(a2c_ext);
            ArrayList a2c_params_translation = GetExtTranslation(a2c_ext);
            a2c_dict.Add("convert_id", "a2c");
            a2c_dict.Add("discription", "accel to color");
            a2c_dict.Add("rotation", a2c_params_rotation);
            a2c_dict.Add("translation", a2c_params_translation);

            // Accel to Depth
            Extrinsics a2d_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Accel, (int)CalibrationDeviceType.Depth);
            ArrayList a2d_params_rotation = GetExtRotation(a2d_ext);
            ArrayList a2d_params_translation = GetExtTranslation(a2d_ext);
            a2d_dict.Add("convert_id", "a2d");
            a2d_dict.Add("discription", "accel to depth");
            a2d_dict.Add("rotation", a2d_params_rotation);
            a2d_dict.Add("translation", a2d_params_translation);

            // Accel to Gyro
            Extrinsics a2g_ext = GetExtrinsics(calibration, (int)CalibrationDeviceType.Accel, (int)CalibrationDeviceType.Gyro);
            ArrayList a2g_params_rotation = GetExtRotation(a2g_ext); ;
            ArrayList a2g_params_translation = GetExtTranslation(a2g_ext);
            a2g_dict.Add("convert_id", "a2g");
            a2g_dict.Add("discription", "accel to gyro");
            a2g_dict.Add("rotation", a2g_params_rotation);
            a2g_dict.Add("translation", a2g_params_translation);

            cm_list.Add(d2c_dict);
            cm_list.Add(d2g_dict);
            cm_list.Add(d2a_dict);
            cm_list.Add(c2d_dict);
            cm_list.Add(c2g_dict);
            cm_list.Add(c2a_dict);
            cm_list.Add(g2d_dict);
            cm_list.Add(g2c_dict);
            cm_list.Add(g2a_dict);
            cm_list.Add(a2d_dict);
            cm_list.Add(a2c_dict);
            cm_list.Add(a2g_dict);

            main_dict.Add("id", id);
            main_dict.Add("location", kinect_loc);
            main_dict.Add("serial_number", serial_num);
            main_dict.Add("extrinsics", ext_dict);
            main_dict.Add("instrinsics", ins_dict);
            main_dict.Add("convert_mode", cm_list);

            string calibration_json = JsonConvert.SerializeObject(main_dict, Formatting.Indented);

            return calibration_json;
        }

        public Extrinsics GetExtrinsics(Calibration calibration, int from, int to)
        {
            Extrinsics extrinsics = new Extrinsics();

            int index = (int)CalibrationDeviceType.Num * from + to;
            extrinsics = calibration.DeviceExtrinsics[index];

            return extrinsics;
        }

        public ArrayList GetExtRotation(Extrinsics ext)
        {
            ArrayList params_rotation = new ArrayList();

            float[] rotation = ext.Rotation;
            int r_length = rotation.Length;
            for (int r_idx = 0; r_idx < r_length; r_idx++)
            {
                params_rotation.Add(rotation[r_idx]);
            }

            return params_rotation;
        }

        public ArrayList GetExtTranslation(Extrinsics ext)
        {
            ArrayList params_translation = new ArrayList();

            float[] translation = ext.Translation;
            int t_length = translation.Length;
            for (int t_idx = 0; t_idx < t_length; t_idx++)
            {
                params_translation.Add(translation[t_idx]);
            }

            return params_translation;
        }
    }
}
