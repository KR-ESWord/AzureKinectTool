using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
// Azure Kinect SDK
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;
// JSON
using Newtonsoft.Json;

namespace AzureKinectTool.function
{
    public class AKTracker
    {
        public string PopTracker(int camera_id, int frame_num, Tracker tracker, Calibration calibration)
        {
            Dictionary<string, object> trg_dict = new Dictionary<string, object>();
            Dictionary<string, object> category_dict = new Dictionary<string, object>();
            ArrayList category_arr = new ArrayList();
            ArrayList annotation_arr = new ArrayList();
            ArrayList v_annotation_arr = new ArrayList();

            // 이미지 정보에 삽입필요
            //string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentUICulture.DateTimeFormat);

            Frame frame = tracker.PopResult();

            int target_chk = 0;
            uint target_idx = 999;
            double z_chk = 100000;
            if (frame != null)
            {
                // Get Detected Body Count
                uint body_cnt = frame.NumberOfBodies;
                if (body_cnt > 0)
                {
                    // Get Target Body ID Index
                    for (uint body_idx = 0; body_idx < body_cnt; body_idx++)
                    {
                        // Get Skeleton Information
                        Skeleton skeleton = frame.GetBodySkeleton(body_idx);
                        Joint joint = skeleton.GetJoint(1); // (1 : SPINE_NAVAL)
                        Vector3? position_3d = calibration.TransformTo3D(joint.Position,
                            CalibrationDeviceType.Depth,
                            CalibrationDeviceType.Color);

                        if (position_3d.Value.Z < z_chk)
                        {
                            z_chk = position_3d.Value.Z;
                            target_idx = body_idx;
                        }
                    }

                    // Get Target Body ID Index
                    for (uint body_idx = 0; body_idx < body_cnt; body_idx++)
                    {
                        // Get Skeleton Information
                        Skeleton skeleton = frame.GetBodySkeleton(body_idx);

                        // Get Body ID from ONNX Model Predict Value
                        uint id = frame.GetBodyId(body_idx);

                        // Check Target Body
                        if (body_idx == target_idx)
                        {
                            target_chk = 1;
                        }
                        else
                        {
                            target_chk = 0;
                        }

                        // Get Joint Data
                        Dictionary<int, object> position_2d_dict = new Dictionary<int, object>();
                        Dictionary<int, object> position_3d_dict = new Dictionary<int, object>();

                        int key_chk_2d = 0;
                        int key_chk_3d = 0;
                        Dictionary<string, ArrayList> anno_keypoint = new Dictionary<string, ArrayList>();
                        ArrayList keypoint_2d_list = new ArrayList();
                        ArrayList keypoint_3d_list = new ArrayList();
                        ArrayList keypoint_angle_list = new ArrayList();
                        ArrayList keypoint_axies_list = new ArrayList();
                        for (int joint_idx = 0; joint_idx < Joint.Size; joint_idx++)
                        {
                            Joint joint = skeleton.GetJoint(joint_idx);

                            Vector2? position_2d = calibration.TransformTo2D(joint.Position,
                                CalibrationDeviceType.Depth,
                                CalibrationDeviceType.Color);
                            ArrayList position_2d_arr = new ArrayList();
                            if (position_2d.HasValue)
                            {
                                // Get 2D Position Information
                                position_2d_arr.Add((double)position_2d.Value.X);
                                position_2d_arr.Add((double)position_2d.Value.Y);

                                keypoint_2d_list.Add((double)position_2d.Value.X);
                                keypoint_2d_list.Add((double)position_2d.Value.Y);
                                keypoint_2d_list.Add(2);

                                key_chk_2d += 1;
                            }
                            else
                            {
                                position_2d_arr.Add((double)0);
                                position_2d_arr.Add((double)0);

                                keypoint_2d_list.Add(0);
                                keypoint_2d_list.Add(0);
                                keypoint_2d_list.Add(0);
                            }
                            position_2d_dict.Add(joint_idx, position_2d_arr);

                            Vector3? position_3d = calibration.TransformTo3D(joint.Position,
                                CalibrationDeviceType.Depth,
                                CalibrationDeviceType.Color);
                            ArrayList position_3d_arr = new ArrayList();
                            if (position_3d.HasValue)
                            {
                                // Get 3D Position Information
                                position_3d_arr.Add((double)position_3d.Value.X);
                                position_3d_arr.Add((double)position_3d.Value.Y);
                                position_3d_arr.Add((double)position_3d.Value.Z);

                                keypoint_3d_list.Add((double)position_3d.Value.X);
                                keypoint_3d_list.Add((double)position_3d.Value.Y);
                                keypoint_3d_list.Add((double)position_3d.Value.Z);
                                keypoint_3d_list.Add(2);

                                // Get 3D Angle Information
                                keypoint_angle_list.Add(Calc3DXAngle(position_3d));
                                keypoint_angle_list.Add(Calc3DYAngle(position_3d));
                                keypoint_angle_list.Add(Calc3DZAngle(position_3d));

                                key_chk_3d += 1;
                            }
                            else
                            {
                                position_3d_arr.Add((double)0);
                                position_3d_arr.Add((double)0);
                                position_3d_arr.Add((double)0);

                                keypoint_3d_list.Add(0);
                                keypoint_3d_list.Add(0);
                                keypoint_3d_list.Add(0);
                                keypoint_3d_list.Add(0);

                                keypoint_angle_list.Add(0);
                                keypoint_angle_list.Add(0);
                                keypoint_angle_list.Add(0);
                            }
                            position_3d_dict.Add(joint_idx, position_3d_arr);

                            // Get Position Axies Information
                            double quaternion_x = joint.Quaternion.X;
                            double quaternion_y = joint.Quaternion.Y;
                            double quaternion_z = joint.Quaternion.Z;
                            double quaternion_w = joint.Quaternion.W;

                            double pitch = CalcPitch(quaternion_x, quaternion_y, quaternion_z, quaternion_w);
                            double yaw = CalcYaw(quaternion_x, quaternion_y, quaternion_z, quaternion_w);
                            double roll = CalcRoll(quaternion_x, quaternion_y, quaternion_z, quaternion_w);

                            keypoint_axies_list.Add(yaw);
                            keypoint_axies_list.Add(pitch);
                            keypoint_axies_list.Add(roll);
                        }

                        // Get 2D & 3D Vector Angle Value
                        Dictionary<string,ArrayList> vectors = VectorPair(position_2d_dict, position_3d_dict);
                        ArrayList bbox_list = new ArrayList();

                        Dictionary<string, object> id_dict = new Dictionary<string, object>();
                        id_dict.Add("category_id", 1);
                        id_dict.Add("id", frame_num);
                        id_dict.Add("body_id", id.ToString());
                        id_dict.Add("target", target_chk.ToString());
                        id_dict.Add("iscrowd", 1);

                        id_dict.Add("keypoints_2d", key_chk_2d);
                        id_dict.Add("keypoints_3d", key_chk_3d);

                        anno_keypoint.Add("2d", keypoint_2d_list);
                        anno_keypoint.Add("3d", keypoint_3d_list);
                        anno_keypoint.Add("3d_point_angle", keypoint_angle_list);
                        anno_keypoint.Add("axies", keypoint_axies_list);
                        id_dict.Add("keypoints", anno_keypoint);

                        id_dict.Add("vector_angle", vectors);
                        id_dict.Add("area", 0);
                        id_dict.Add("bbox", bbox_list);

                        v_annotation_arr.Add(id_dict);
                    }
                }
            }
            else
            {
                Dictionary<string, ArrayList> anno_keypoint = new Dictionary<string, ArrayList>();
                ArrayList keypoint_2d_list = new ArrayList();
                ArrayList keypoint_3d_list = new ArrayList();
                ArrayList keypoint_angle_list = new ArrayList();
                ArrayList keypoint_axies_list = new ArrayList();
                Dictionary<string, ArrayList> vectors = new Dictionary<string, ArrayList>();
                ArrayList vector_2d_list = new ArrayList();
                ArrayList vector_3d_list = new ArrayList();
                ArrayList bbox_list = new ArrayList();
                Dictionary<string, object> id_dict = new Dictionary<string, object>();
                id_dict.Add("category_id", 1);
                id_dict.Add("id", frame_num);
                id_dict.Add("body_id", 0);
                id_dict.Add("target", target_chk);
                id_dict.Add("num_keypoints", 0);
                id_dict.Add("iscrowd", 0);

                anno_keypoint.Add("2d", keypoint_2d_list);
                anno_keypoint.Add("3d", keypoint_3d_list);
                anno_keypoint.Add("3d_point_angle", keypoint_angle_list);
                anno_keypoint.Add("axies", keypoint_axies_list);
                id_dict.Add("keypoints", anno_keypoint);

                vectors.Add("2d", vector_2d_list);
                vectors.Add("3d", vector_3d_list);
                id_dict.Add("vector_angle", vectors);
                id_dict.Add("area", 0);
                id_dict.Add("bbox", bbox_list);

                v_annotation_arr.Add(id_dict);
            }

            frame.Dispose();

            //coco - categories
            List<string> description_dict = KeyPoint();
            ArrayList skeleton_arr = SkeletonMap();
            ArrayList vectorangle_arr = VectorAngleMap();
            List<string> axies_list = AxiesList();

            category_dict.Add("supercategory", "person");
            category_dict.Add("id", 1);
            category_dict.Add("name", "person");
            category_dict.Add("keypoints", description_dict);
            category_dict.Add("skeleton", skeleton_arr);
            category_dict.Add("vector_angle", vectorangle_arr);
            category_dict.Add("axies", axies_list);
            category_arr.Add(category_dict);

            // coco - annotation
            Dictionary<string, object> trg_anno_dict = new Dictionary<string, object>();
            trg_anno_dict.Add("camera_id", camera_id);
            trg_anno_dict.Add("video_id", camera_id);
            trg_anno_dict.Add("v_annotation", v_annotation_arr);
            annotation_arr.Add(trg_anno_dict);

            // categories & annotaion json
            trg_dict.Add("categories", category_arr);
            trg_dict.Add("annotations", annotation_arr);
           
            string annotation_json = JsonConvert.SerializeObject(trg_dict, Formatting.Indented);

            return annotation_json;
        }

        // Key Point Categories
        public List<string> KeyPoint()
        {
            List<string> keypoint_list = new List<string>();
            keypoint_list.Add("pelvis");
            keypoint_list.Add("spine_naval");
            keypoint_list.Add("spine_chest");
            keypoint_list.Add("neck");
            keypoint_list.Add("clavicle_left");
            keypoint_list.Add("shoulder_left");
            keypoint_list.Add("elbow_left");
            keypoint_list.Add("wrist_left");
            keypoint_list.Add("hand_left");
            keypoint_list.Add("handtip_left");
            keypoint_list.Add("thumb_left");
            keypoint_list.Add("clavicle_right");
            keypoint_list.Add("shoulder_right");
            keypoint_list.Add("elbow_right");
            keypoint_list.Add("wrist_right");
            keypoint_list.Add("hand_right");
            keypoint_list.Add("handtip_right");
            keypoint_list.Add("thumb_right");
            keypoint_list.Add("hip_left");
            keypoint_list.Add("knee_left");
            keypoint_list.Add("ankle_left");
            keypoint_list.Add("foot_left");
            keypoint_list.Add("hip_right");
            keypoint_list.Add("knee_right");
            keypoint_list.Add("ankle_right");
            keypoint_list.Add("foot_right");
            keypoint_list.Add("head");
            keypoint_list.Add("nose");
            keypoint_list.Add("eye_left");
            keypoint_list.Add("ear_left");
            keypoint_list.Add("eye_right");
            keypoint_list.Add("ear_right");

            return keypoint_list;
        }

        // Skeleton Categories
        public ArrayList SkeletonMap()
        {
            ArrayList skeleton = new ArrayList();
            
            List<int> s1 = new List<int>();
            s1.Add(0);
            s1.Add(1);
            skeleton.Add(s1);

            List<int> s2 = new List<int>();
            s2.Add(1);
            s2.Add(2);
            skeleton.Add(s2);

            List<int> s3 = new List<int>();
            s3.Add(2);
            s3.Add(3);
            skeleton.Add(s3);

            List<int> s4 = new List<int>();
            s4.Add(2);
            s4.Add(4);
            skeleton.Add(s4);

            List<int> s5 = new List<int>();
            s5.Add(4);
            s5.Add(5);
            skeleton.Add(s5);

            List<int> s6 = new List<int>();
            s6.Add(5);
            s6.Add(6);
            skeleton.Add(s6);

            List<int> s7 = new List<int>();
            s7.Add(6);
            s7.Add(7);
            skeleton.Add(s7);

            List<int> s8 = new List<int>();
            s8.Add(7);
            s8.Add(8);
            skeleton.Add(s8);

            List<int> s9 = new List<int>();
            s9.Add(8);
            s9.Add(9);
            skeleton.Add(s9);

            List<int> s10 = new List<int>();
            s10.Add(7);
            s10.Add(10);
            skeleton.Add(s10);

            List<int> s11 = new List<int>();
            s11.Add(2);
            s11.Add(11);
            skeleton.Add(s11);

            List<int> s12 = new List<int>();
            s12.Add(11);
            s12.Add(12);
            skeleton.Add(s12);

            List<int> s13 = new List<int>();
            s13.Add(12);
            s13.Add(13);
            skeleton.Add(s13);

            List<int> s14 = new List<int>();
            s14.Add(13);
            s14.Add(14);
            skeleton.Add(s14);

            List<int> s15 = new List<int>();
            s15.Add(14);
            s15.Add(15);
            skeleton.Add(s15);

            List<int> s16 = new List<int>();
            s16.Add(15);
            s16.Add(16);
            skeleton.Add(s16);

            List<int> s17 = new List<int>();
            s17.Add(14);
            s17.Add(17);
            skeleton.Add(s17);

            List<int> s18 = new List<int>();
            s18.Add(0);
            s18.Add(18);
            skeleton.Add(s18);

            List<int> s19 = new List<int>();
            s19.Add(18);
            s19.Add(19);
            skeleton.Add(s19);

            List<int> s20 = new List<int>();
            s20.Add(19);
            s20.Add(20);
            skeleton.Add(s20);

            List<int> s21 = new List<int>();
            s21.Add(20);
            s21.Add(21);
            skeleton.Add(s21);

            List<int> s22 = new List<int>();
            s22.Add(0);
            s22.Add(22);
            skeleton.Add(s22);

            List<int> s23 = new List<int>();
            s23.Add(22);
            s23.Add(23);
            skeleton.Add(s23);

            List<int> s24 = new List<int>();
            s24.Add(23);
            s24.Add(24);
            skeleton.Add(s24);

            List<int> s25 = new List<int>();
            s25.Add(24);
            s25.Add(25);
            skeleton.Add(s25);

            List<int> s26 = new List<int>();
            s26.Add(3);
            s26.Add(26);
            skeleton.Add(s26);

            List<int> s27 = new List<int>();
            s27.Add(26);
            s27.Add(27);
            skeleton.Add(s27);

            List<int> s28 = new List<int>();
            s28.Add(26);
            s28.Add(28);
            skeleton.Add(s28);

            List<int> s29 = new List<int>();
            s29.Add(26);
            s29.Add(29);
            skeleton.Add(s29);

            List<int> s30 = new List<int>();
            s30.Add(26);
            s30.Add(30);
            skeleton.Add(s30);

            List<int> s31 = new List<int>();
            s31.Add(26);
            s31.Add(31);
            skeleton.Add(s31);

            return skeleton;
        }

        // Vector Angle Categories
        public ArrayList VectorAngleMap()
        {
            ArrayList vector_angle = new ArrayList();

            List<int> v1 = new List<int>();
            v1.Add(1);
            v1.Add(0);
            v1.Add(18);
            vector_angle.Add(v1);

            List<int> v2 = new List<int>();
            v2.Add(1);
            v2.Add(0);
            v2.Add(22);
            vector_angle.Add(v2);

            List<int> v3 = new List<int>();
            v3.Add(18);
            v3.Add(0);
            v3.Add(22);
            vector_angle.Add(v3);

            List<int> v4 = new List<int>();
            v4.Add(0);
            v4.Add(1);
            v4.Add(2);
            vector_angle.Add(v4);

            List<int> v5 = new List<int>();
            v5.Add(1);
            v5.Add(2);
            v5.Add(3);
            vector_angle.Add(v5);

            List<int> v6 = new List<int>();
            v6.Add(1);
            v6.Add(2);
            v6.Add(4);
            vector_angle.Add(v6);

            List<int> v7 = new List<int>();
            v7.Add(1);
            v7.Add(2);
            v7.Add(11);
            vector_angle.Add(v7);

            List<int> v8 = new List<int>();
            v8.Add(4);
            v8.Add(2);
            v8.Add(3);
            vector_angle.Add(v8);

            List<int> v9 = new List<int>();
            v9.Add(4);
            v9.Add(2);
            v9.Add(11);
            vector_angle.Add(v9);

            List<int> v10 = new List<int>();
            v10.Add(3);
            v10.Add(2);
            v10.Add(11);
            vector_angle.Add(v10);

            List<int> v11 = new List<int>();
            v11.Add(2);
            v11.Add(3);
            v11.Add(26);
            vector_angle.Add(v11);

            List<int> v12 = new List<int>();
            v12.Add(2);
            v12.Add(4);
            v12.Add(5);
            vector_angle.Add(v12);

            List<int> v13 = new List<int>();
            v13.Add(4);
            v13.Add(5);
            v13.Add(6);
            vector_angle.Add(v13);

            List<int> v14 = new List<int>();
            v14.Add(5);
            v14.Add(6);
            v14.Add(7);
            vector_angle.Add(v14);

            List<int> v15 = new List<int>();
            v15.Add(6);
            v15.Add(7);
            v15.Add(8);
            vector_angle.Add(v15);

            List<int> v16 = new List<int>();
            v16.Add(6);
            v16.Add(7);
            v16.Add(10);
            vector_angle.Add(v16);

            List<int> v17 = new List<int>();
            v17.Add(7);
            v17.Add(8);
            v17.Add(9);
            vector_angle.Add(v17);

            List<int> v18 = new List<int>();
            v18.Add(2);
            v18.Add(11);
            v18.Add(12);
            vector_angle.Add(v18);

            List<int> v19 = new List<int>();
            v19.Add(11);
            v19.Add(12);
            v19.Add(13);
            vector_angle.Add(v19);

            List<int> v20 = new List<int>();
            v20.Add(12);
            v20.Add(13);
            v20.Add(14);
            vector_angle.Add(v20);

            List<int> v21 = new List<int>();
            v21.Add(13);
            v21.Add(14);
            v21.Add(15);
            vector_angle.Add(v21);

            List<int> v22 = new List<int>();
            v22.Add(13);
            v22.Add(14);
            v22.Add(17);
            vector_angle.Add(v22);

            List<int> v23 = new List<int>();
            v23.Add(14);
            v23.Add(15);
            v23.Add(16);
            vector_angle.Add(v23);

            List<int> v24 = new List<int>();
            v24.Add(0);
            v24.Add(18);
            v24.Add(19);
            vector_angle.Add(v24);

            List<int> v25 = new List<int>();
            v25.Add(18);
            v25.Add(19);
            v25.Add(20);
            vector_angle.Add(v25);

            List<int> v26 = new List<int>();
            v26.Add(19);
            v26.Add(20);
            v26.Add(21);
            vector_angle.Add(v26);

            List<int> v27 = new List<int>();
            v27.Add(0);
            v27.Add(22);
            v27.Add(23);
            vector_angle.Add(v27);

            List<int> v28 = new List<int>();
            v28.Add(22);
            v28.Add(23);
            v28.Add(24);
            vector_angle.Add(v28);

            List<int> v29 = new List<int>();
            v29.Add(23);
            v29.Add(24);
            v29.Add(25);
            vector_angle.Add(v29);

            List<int> v30 = new List<int>();
            v30.Add(3);
            v30.Add(26);
            v30.Add(27);
            vector_angle.Add(v30);

            List<int> v31 = new List<int>();
            v31.Add(3);
            v31.Add(26);
            v31.Add(28);
            vector_angle.Add(v31);

            List<int> v32 = new List<int>();
            v32.Add(3);
            v32.Add(26);
            v32.Add(29);
            vector_angle.Add(v32);

            List<int> v33 = new List<int>();
            v33.Add(3);
            v33.Add(26);
            v33.Add(30);
            vector_angle.Add(v33);

            List<int> v34 = new List<int>();
            v34.Add(3);
            v34.Add(26);
            v34.Add(31);
            vector_angle.Add(v34);

            return vector_angle;
        }

        // Axies Categories
        public List<string> AxiesList()
        {
            List<string> axies_list = new List<string>();
            axies_list.Add("pelvis_yaw");
            axies_list.Add("pelvis_pitch");
            axies_list.Add("pelvis_roll");
            
            axies_list.Add("spine_naval_yaw");
            axies_list.Add("spine_naval_pitch");
            axies_list.Add("spine_naval_roll");
            
            axies_list.Add("spine_chest_yaw");
            axies_list.Add("spine_chest_pitch");
            axies_list.Add("spine_chest_roll");
            
            axies_list.Add("neck_yaw");
            axies_list.Add("neck_pitch");
            axies_list.Add("neck_roll");
            
            axies_list.Add("clavicle_left_yaw");
            axies_list.Add("clavicle_left_pitch");
            axies_list.Add("clavicle_left_roll");
            
            axies_list.Add("shoulder_left_yaw");
            axies_list.Add("shoulder_left_pitch");
            axies_list.Add("shoulder_left_roll");

            axies_list.Add("elbow_left_yaw");
            axies_list.Add("elbow_left_pitch");
            axies_list.Add("elbow_left_roll");

            axies_list.Add("wrist_left_yaw");
            axies_list.Add("wrist_left_pitch");
            axies_list.Add("wrist_left_roll");

            axies_list.Add("hand_left_yaw");
            axies_list.Add("hand_left_pitch");
            axies_list.Add("hand_left_roll");

            axies_list.Add("handtip_left_yaw");
            axies_list.Add("handtip_left_pitch");
            axies_list.Add("handtip_left_roll");

            axies_list.Add("thumb_left_yaw");
            axies_list.Add("thumb_left_pitch");
            axies_list.Add("thumb_left_roll");

            axies_list.Add("clavicle_right_yaw");
            axies_list.Add("clavicle_right_pitch");
            axies_list.Add("clavicle_right_roll");

            axies_list.Add("shoulder_right_yaw");
            axies_list.Add("shoulder_right_pitch");
            axies_list.Add("shoulder_right_roll");

            axies_list.Add("elbow_right_yaw");
            axies_list.Add("elbow_right_pitch");
            axies_list.Add("elbow_right_roll");

            axies_list.Add("wrist_right_yaw");
            axies_list.Add("wrist_right_pitch");
            axies_list.Add("wrist_right_roll");

            axies_list.Add("hand_right_yaw");
            axies_list.Add("hand_right_pitch");
            axies_list.Add("hand_right_roll");

            axies_list.Add("handtip_right_yaw");
            axies_list.Add("handtip_right_pitch");
            axies_list.Add("handtip_right_roll");

            axies_list.Add("thumb_right_yaw");
            axies_list.Add("thumb_right_pitch");
            axies_list.Add("thumb_right_roll");

            axies_list.Add("hip_left_yaw");
            axies_list.Add("hip_left_pitch");
            axies_list.Add("hip_left_roll");

            axies_list.Add("knee_left_yaw");
            axies_list.Add("knee_left_pitch");
            axies_list.Add("knee_left_roll");

            axies_list.Add("ankle_left_yaw");
            axies_list.Add("ankle_left_pitch");
            axies_list.Add("ankle_left_roll");

            axies_list.Add("foot_left_yaw");
            axies_list.Add("foot_left_pitch");
            axies_list.Add("foot_left_roll");

            axies_list.Add("hip_right_yaw");
            axies_list.Add("hip_right_pitch");
            axies_list.Add("hip_right_roll");

            axies_list.Add("knee_right_yaw");
            axies_list.Add("knee_right_pitch");
            axies_list.Add("knee_right_roll");

            axies_list.Add("ankle_right_yaw");
            axies_list.Add("ankle_right_pitch");
            axies_list.Add("ankle_right_roll");

            axies_list.Add("foot_right_yaw");
            axies_list.Add("foot_right_pitch");
            axies_list.Add("foot_right_roll");

            axies_list.Add("head_yaw");
            axies_list.Add("head_pitch");
            axies_list.Add("head_roll");

            axies_list.Add("nose_yaw");
            axies_list.Add("nose_pitch");
            axies_list.Add("nose_roll");

            axies_list.Add("eye_left_yaw");
            axies_list.Add("eye_left_pitch");
            axies_list.Add("eye_left_roll");

            axies_list.Add("ear_left_yaw");
            axies_list.Add("ear_left_pitch");
            axies_list.Add("ear_left_roll");

            axies_list.Add("eye_right_yaw");
            axies_list.Add("eye_right_pitch");
            axies_list.Add("eye_right_roll");

            axies_list.Add("ear_right_yaw");
            axies_list.Add("ear_right_pitch");
            axies_list.Add("ear_right_roll");

            return axies_list;
        }


        // Calculate 3D X Angle
        public double Calc3DXAngle(Vector3? position_3d)
        {
            double angle = Math.Atan2(position_3d.Value.Y, position_3d.Value.Z) / Math.PI * 180;
            angle = Math.Min(Math.Abs(angle), angle + 180);
            return angle;
        }

        // Calculate 3D Y Angle
        public double Calc3DYAngle(Vector3? position_3d)
        {
            double angle = Math.Atan2(position_3d.Value.X, position_3d.Value.Z) / Math.PI * 180;
            angle = Math.Min(Math.Abs(angle), angle + 180);
            return angle;
        }

        // Calculate 3D Z Angle
        public double Calc3DZAngle(Vector3? position_3d)
        {
            double angle = Math.Atan2(position_3d.Value.Y, position_3d.Value.X) / Math.PI * 180;
            angle = Math.Min(Math.Abs(angle), angle + 180);
            return angle;
        }

        // Calculate Pitch
        public double CalcPitch(double quaternion_x, double quaternion_y, double quaternion_z, double quaternion_w)
        {
            double value1 = 2.0 * (quaternion_w * quaternion_x + quaternion_y * quaternion_z);
            double value2 = 1.0 - 2.0 * (quaternion_x * quaternion_x + quaternion_y * quaternion_y);

            double roll = Math.Atan2(value1, value2);
            double pitch = roll * (180.0 / Math.PI);
            return pitch;
        }

        // Calculate Yaw
        public double CalcYaw(double quaternion_x, double quaternion_y, double quaternion_z, double quaternion_w)
        {
            double value = +2.0 * (quaternion_w * quaternion_y - quaternion_z * quaternion_x);
            value = value > 1.0 ? 1.0 : value;
            value = value < -1.0 ? -1.0 : value;

            double pitch = Math.Asin(value);
            double yaw = pitch * (180.0 / Math.PI);

            return yaw;
        }

        // Calculate Roll
        public double CalcRoll(double quaternion_x, double quaternion_y, double quaternion_z, double quaternion_w)
        {
            double value1 = 2.0 * (quaternion_w * quaternion_z + quaternion_x * quaternion_y);
            double value2 = 1.0 - 2.0 * (quaternion_y * quaternion_y + quaternion_z * quaternion_z);

            double yaw = Math.Atan2(value1, value2);
            double roll = yaw * (180.0 / Math.PI);

            return roll;
        }

        // Calculate 2D Vector Angle
        public double Calc2DVector(ArrayList p1, ArrayList p2, ArrayList p3)
        {
            double vector_2d = 0;
            double p1_x = (double)p1[0];
            double p1_y = (double)p1[1];
            double p2_x = (double)p2[0];
            double p2_y = (double)p2[1];
            double p3_x = (double)p3[0];
            double p3_y = (double)p3[1];

            if (p1_x != 0 && p1_y != 0 &&
                p2_x != 0 && p2_y != 0 &&
                p3_x != 0 && p3_y != 0)
            {
                Vector2 v1 = new Vector2((float)p1_x, (float)p1_y);
                Vector2 v2 = new Vector2((float)p2_x, (float)p2_y);
                Vector2 v3 = new Vector2((float)p3_x, (float)p3_y);

                Vector2 d1 = Vector2.Normalize(v1 - v2);
                Vector2 d2 = Vector2.Normalize(v3 - v2);

                float vector_dot = Vector2.Dot(d1, d2);

                if (vector_dot < -1.0f)
                {
                    vector_dot = -1.0f;
                }
                else if (vector_dot > 1.0f)
                {
                    vector_dot = 1.0f;
                }

                vector_2d = Math.Acos(vector_dot) * 180 / Math.PI;
            }
            else
            {
                vector_2d = 0;
            }
            return vector_2d;
        }

        // Calculate 3D Vector Angle
        public double Calc3DVector(ArrayList p1, ArrayList p2, ArrayList p3)
        {
            double vector_3d = 0;
            double p1_x = (double)p1[0];
            double p1_y = (double)p1[1];
            double p1_z = (double)p1[2];
            double p2_x = (double)p2[0];
            double p2_y = (double)p2[1];
            double p2_z = (double)p2[2];
            double p3_x = (double)p3[0];
            double p3_y = (double)p3[1];
            double p3_z = (double)p3[2];

            if (p1_x != 0 && p1_y != 0 && p1_z != 0 &&
                p2_x != 0 && p2_y != 0 && p2_z != 0 &&
                p3_x != 0 && p3_y != 0 && p3_z != 0)
            {
                Vector3 v1 = new Vector3((float)p1_x, (float)p1_y, (float)p1_z);
                Vector3 v2 = new Vector3((float)p2_x, (float)p2_y, (float)p2_z);
                Vector3 v3 = new Vector3((float)p3_x, (float)p3_y, (float)p3_z);

                Vector3 d1 = Vector3.Normalize(v1 - v2);
                Vector3 d2 = Vector3.Normalize(v3 - v2);

                float vector_dot = Vector3.Dot(d1, d2);

                if (vector_dot < -1.0f)
                {
                    vector_dot = -1.0f;
                }
                else if (vector_dot > 1.0f)
                {
                    vector_dot = 1.0f;
                }

                vector_3d = Math.Acos(vector_dot) * 180 / Math.PI;
            }
            else
            {
                vector_3d = 0;
            }
            return vector_3d;
        }

        public Dictionary<string,ArrayList> VectorPair(Dictionary<int, object> position_2d_dict, Dictionary<int, object> position_3d_dict)
        {
            Dictionary<string, ArrayList> vector_anlges = new Dictionary<string, ArrayList>();
            ArrayList vector_2d_list = new ArrayList();
            ArrayList vector_3d_list = new ArrayList();

            // SPINE_NAVAL(1) - PELVIS(0) - HIP_LEFT(18)
            double pair1_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[18]);
            double pair1_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[18]);
            // SPINE_NAVAL(1) - PELVIS(0) - HIP_RIGHT(22)
            double pair2_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[22]);
            double pair2_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[22]);
            // HIP_LEFT(18) - PELVIS(0) - HIP_RIGHT(22)
            double pair3_2d = Calc2DVector((ArrayList)position_2d_dict[18], (ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[22]);
            double pair3_3d = Calc3DVector((ArrayList)position_3d_dict[18], (ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[22]);
            // PELVIS(0) - SPINE_NAVAL(1) - SPINE_CHEST(2)
            double pair4_2d = Calc2DVector((ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[2]);
            double pair4_3d = Calc3DVector((ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[2]);
            // SPINE_NAVAL(1) - SPINE_CHEST(2) - NECK(3)
            double pair5_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[3]);
            double pair5_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[3]);
            // SPINE_NAVAL(1) - SPINE_CHEST(2) - CLAVICLE_LEFT(4)
            double pair6_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[4]);
            double pair6_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[4]);
            // SPINE_NAVAL(1) - SPINE_CHEST(2) - CLAVICLE_RIGHT(11)
            double pair7_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[11]);
            double pair7_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[11]);
            // CLAVICLE_LEFT(4) - SPINE_CHEST(2) - NECK(3)
            double pair8_2d = Calc2DVector((ArrayList)position_2d_dict[4], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[3]);
            double pair8_3d = Calc3DVector((ArrayList)position_3d_dict[4], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[3]);
            // CLAVICLE_LEFT(4) - SPINE_CHEST(2) - CLAVICLE_RIGHT(11)
            double pair9_2d = Calc2DVector((ArrayList)position_2d_dict[4], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[11]);
            double pair9_3d = Calc3DVector((ArrayList)position_3d_dict[4], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[11]);
            // NECK(3) - SPINE_CHEST(2) - CLAVICLE_RIGHT(11)
            double pair10_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[11]);
            double pair10_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[11]);
            // SPINE_CHEST(2) - NECK(3) - HEAD(26)
            double pair11_2d = Calc2DVector((ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26]);
            double pair11_3d = Calc3DVector((ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26]);
            // SPINE_CHEST(2) - CLAVICLE_LEFT(4) - SHOULDER_LEFT(5)
            double pair12_2d = Calc2DVector((ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[4], (ArrayList)position_2d_dict[5]);
            double pair12_3d = Calc3DVector((ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[4], (ArrayList)position_3d_dict[5]);
            // CLAVICLE_LEFT(4) - SHOULDER_LEFT(5) - ELVOW_LEFT(6)
            double pair13_2d = Calc2DVector((ArrayList)position_2d_dict[4], (ArrayList)position_2d_dict[5], (ArrayList)position_2d_dict[6]);
            double pair13_3d = Calc3DVector((ArrayList)position_3d_dict[4], (ArrayList)position_3d_dict[5], (ArrayList)position_3d_dict[6]);
            // SHOULDER_LEFT(5) - ELBOW_LEFT(6) - WRIST_LEFT(7)
            double pair14_2d = Calc2DVector((ArrayList)position_2d_dict[5], (ArrayList)position_2d_dict[6], (ArrayList)position_2d_dict[7]);
            double pair14_3d = Calc3DVector((ArrayList)position_3d_dict[5], (ArrayList)position_3d_dict[6], (ArrayList)position_3d_dict[7]);
            // ELBOW_LEFT(6) - WRIST_LEFT(7) - HAND_LEFT(8)
            double pair15_2d = Calc2DVector((ArrayList)position_2d_dict[6], (ArrayList)position_2d_dict[7], (ArrayList)position_2d_dict[8]);
            double pair15_3d = Calc3DVector((ArrayList)position_3d_dict[6], (ArrayList)position_3d_dict[7], (ArrayList)position_3d_dict[8]);
            // ELBOW_LEFT(6) - WRIST_LEFT(7) - THUMB_LEFT(10)
            double pair16_2d = Calc2DVector((ArrayList)position_2d_dict[6], (ArrayList)position_2d_dict[7], (ArrayList)position_2d_dict[10]);
            double pair16_3d = Calc3DVector((ArrayList)position_3d_dict[6], (ArrayList)position_3d_dict[7], (ArrayList)position_3d_dict[10]);
            // WRIST_LEFT(7) - HAND_LEFT(8) - HANDTIP_LEFT(9)
            double pair17_2d = Calc2DVector((ArrayList)position_2d_dict[7], (ArrayList)position_2d_dict[8], (ArrayList)position_2d_dict[9]);
            double pair17_3d = Calc3DVector((ArrayList)position_3d_dict[7], (ArrayList)position_3d_dict[8], (ArrayList)position_3d_dict[9]);
            // SPINE_CHEST(2) - CLAVICLE_RIGHT(11) - SHOULDER_RIGHT(12)
            double pair18_2d = Calc2DVector((ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[11], (ArrayList)position_2d_dict[12]);
            double pair18_3d = Calc3DVector((ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[11], (ArrayList)position_3d_dict[12]);
            // CLAVICLE_RIGHT(11) - SHOULDER_RIGHT(12) - ELBOW_RIGHT(13)
            double pair19_2d = Calc2DVector((ArrayList)position_2d_dict[11], (ArrayList)position_2d_dict[12], (ArrayList)position_2d_dict[13]);
            double pair19_3d = Calc3DVector((ArrayList)position_3d_dict[11], (ArrayList)position_3d_dict[12], (ArrayList)position_3d_dict[13]);
            // SHOULDER_RIGHT(12) - ELBOW_RIGHT(13) - WRIST_RIGHT(14)
            double pair20_2d = Calc2DVector((ArrayList)position_2d_dict[12], (ArrayList)position_2d_dict[13], (ArrayList)position_2d_dict[14]);
            double pair20_3d = Calc3DVector((ArrayList)position_3d_dict[12], (ArrayList)position_3d_dict[13], (ArrayList)position_3d_dict[14]);
            // ELBOW_RIGHT(13) - WRIST_RIGHT(14) - HAND_RIGHT(15)
            double pair21_2d = Calc2DVector((ArrayList)position_2d_dict[13], (ArrayList)position_2d_dict[14], (ArrayList)position_2d_dict[15]);
            double pair21_3d = Calc3DVector((ArrayList)position_3d_dict[13], (ArrayList)position_3d_dict[14], (ArrayList)position_3d_dict[15]);
            // ELBOW_RIGHT(13) - WRIST_RIGHT(14) - THUMB_RIGHT(17)
            double pair22_2d = Calc2DVector((ArrayList)position_2d_dict[13], (ArrayList)position_2d_dict[14], (ArrayList)position_2d_dict[17]);
            double pair22_3d = Calc3DVector((ArrayList)position_3d_dict[13], (ArrayList)position_3d_dict[14], (ArrayList)position_3d_dict[17]);
            // WRIST_RIGHT(14) - HAND_RIGHT(15) - HANDTIP_RIGHT(16)
            double pair23_2d = Calc2DVector((ArrayList)position_2d_dict[14], (ArrayList)position_2d_dict[15], (ArrayList)position_2d_dict[16]);
            double pair23_3d = Calc3DVector((ArrayList)position_3d_dict[14], (ArrayList)position_3d_dict[15], (ArrayList)position_3d_dict[16]);
            // PELVIS(0) - HIP_LEFT(18) - KNEE_LEFT(19)
            double pair24_2d = Calc2DVector((ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[18], (ArrayList)position_2d_dict[19]);
            double pair24_3d = Calc3DVector((ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[18], (ArrayList)position_3d_dict[19]);
            // HIP_LEFT(18) - KNEE_LEFT(19) - ANKLE_LEFT(20)
            double pair25_2d = Calc2DVector((ArrayList)position_2d_dict[18], (ArrayList)position_2d_dict[19], (ArrayList)position_2d_dict[20]);
            double pair25_3d = Calc3DVector((ArrayList)position_3d_dict[18], (ArrayList)position_3d_dict[19], (ArrayList)position_3d_dict[20]);
            // KNEE_LEFT(19) - ANKLE_LEFT(20) - FOOT_LEFT(21)
            double pair26_2d = Calc2DVector((ArrayList)position_2d_dict[19], (ArrayList)position_2d_dict[20], (ArrayList)position_2d_dict[21]);
            double pair26_3d = Calc3DVector((ArrayList)position_3d_dict[19], (ArrayList)position_3d_dict[20], (ArrayList)position_3d_dict[21]);
            // PELVIS(0) - HIP_RIGHT(22) - KNEE_RIGHT(23)
            double pair27_2d = Calc2DVector((ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[22], (ArrayList)position_2d_dict[23]);
            double pair27_3d = Calc3DVector((ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[22], (ArrayList)position_3d_dict[23]);
            // HIP_RIGHT(22) - KNEE_RIGHT(23) - ANKLE_RIGHT(24)
            double pair28_2d = Calc2DVector((ArrayList)position_2d_dict[22], (ArrayList)position_2d_dict[23], (ArrayList)position_2d_dict[24]);
            double pair28_3d = Calc3DVector((ArrayList)position_3d_dict[22], (ArrayList)position_3d_dict[23], (ArrayList)position_3d_dict[24]);
            // KNEE_RIGHT(23) - ANKLE_RIGHT(24) - FOOT_RIGHT(25)
            double pair29_2d = Calc2DVector((ArrayList)position_2d_dict[23], (ArrayList)position_2d_dict[24], (ArrayList)position_2d_dict[25]);
            double pair29_3d = Calc3DVector((ArrayList)position_3d_dict[23], (ArrayList)position_3d_dict[24], (ArrayList)position_3d_dict[25]);
            // NECK(3) - HEAD(26) - NOSE(27)
            double pair30_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[27]);
            double pair30_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[27]);
            // NECK(3) - HEAD(26) - EYE_LEFT(28)
            double pair31_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[28]);
            double pair31_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[28]);
            // NECK(3) - HEAD(26) - EAR_LEFT(29)
            double pair32_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[29]);
            double pair32_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[29]);
            // NECK(3) - HEAD(26) - EYE_RIGHT(30)
            double pair33_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[30]);
            double pair33_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[30]);
            // NECK(3) - HEAD(26) - EAR_RIGHT(31)
            double pair34_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[31]);
            double pair34_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[31]);

            vector_2d_list.Add(pair1_2d);
            vector_3d_list.Add(pair1_3d);
            vector_2d_list.Add(pair2_2d);
            vector_3d_list.Add(pair2_3d);
            vector_2d_list.Add(pair3_2d);
            vector_3d_list.Add(pair3_3d);
            vector_2d_list.Add(pair4_2d);
            vector_3d_list.Add(pair4_3d);
            vector_2d_list.Add(pair5_2d);
            vector_3d_list.Add(pair5_3d);
            vector_2d_list.Add(pair6_2d);
            vector_3d_list.Add(pair6_3d);
            vector_2d_list.Add(pair7_2d);
            vector_3d_list.Add(pair7_3d);
            vector_2d_list.Add(pair8_2d);
            vector_3d_list.Add(pair8_3d);
            vector_2d_list.Add(pair9_2d);
            vector_3d_list.Add(pair9_3d);
            vector_2d_list.Add(pair10_2d);
            vector_3d_list.Add(pair10_3d);
            vector_2d_list.Add(pair11_2d);
            vector_3d_list.Add(pair11_3d);
            vector_2d_list.Add(pair12_2d);
            vector_3d_list.Add(pair12_3d);
            vector_2d_list.Add(pair13_2d);
            vector_3d_list.Add(pair13_3d);
            vector_2d_list.Add(pair14_2d);
            vector_3d_list.Add(pair14_3d);
            vector_2d_list.Add(pair15_2d);
            vector_3d_list.Add(pair15_3d);
            vector_2d_list.Add(pair16_2d);
            vector_3d_list.Add(pair16_3d);
            vector_2d_list.Add(pair17_2d);
            vector_3d_list.Add(pair17_3d);
            vector_2d_list.Add(pair18_2d);
            vector_3d_list.Add(pair18_3d);
            vector_2d_list.Add(pair19_2d);
            vector_3d_list.Add(pair19_3d);
            vector_2d_list.Add(pair20_2d);
            vector_3d_list.Add(pair20_3d);
            vector_2d_list.Add(pair21_2d);
            vector_3d_list.Add(pair21_3d);
            vector_2d_list.Add(pair22_2d);
            vector_3d_list.Add(pair22_3d);
            vector_2d_list.Add(pair23_2d);
            vector_3d_list.Add(pair23_3d);
            vector_2d_list.Add(pair24_2d);
            vector_3d_list.Add(pair24_3d);
            vector_2d_list.Add(pair25_2d);
            vector_3d_list.Add(pair25_3d);
            vector_2d_list.Add(pair26_2d);
            vector_3d_list.Add(pair26_3d);
            vector_2d_list.Add(pair27_2d);
            vector_3d_list.Add(pair27_3d);
            vector_2d_list.Add(pair28_2d);
            vector_3d_list.Add(pair28_3d);
            vector_2d_list.Add(pair29_2d);
            vector_3d_list.Add(pair29_3d);
            vector_2d_list.Add(pair30_2d);
            vector_3d_list.Add(pair30_3d);
            vector_2d_list.Add(pair31_2d);
            vector_3d_list.Add(pair31_3d);
            vector_2d_list.Add(pair32_2d);
            vector_3d_list.Add(pair32_3d);
            vector_2d_list.Add(pair33_2d);
            vector_3d_list.Add(pair33_3d);
            vector_2d_list.Add(pair34_2d);
            vector_3d_list.Add(pair34_3d);

            vector_anlges.Add("2d", vector_2d_list);
            vector_anlges.Add("3d", vector_3d_list);

            return vector_anlges;
        }
    }
}
