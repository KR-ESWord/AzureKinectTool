using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
// Azure Kinect SDK
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;
// JSON
using Newtonsoft.Json;

namespace AzureKinectTool.function
{
    public class AKTracker
    {
        public string PopTracker(Tracker tracker, Calibration calibration)
        {
            Dictionary<string, object> annotation_dict = new Dictionary<string, object>();
            ArrayList annotation_arr = new ArrayList();

            Dictionary<string, object> id_dict = new Dictionary<string, object>();

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

                        id_dict.Add("id", id.ToString());
                        id_dict.Add("target", target_chk.ToString());

                        Dictionary<int, object> position_2d_dict = new Dictionary<int, object>();
                        Dictionary<int, object> position_3d_dict = new Dictionary<int, object>();
                        
                        Dictionary<int, object> point_angle_dict = new Dictionary<int, object>();
                        Dictionary<int, object> point_axies_dict = new Dictionary<int, object>();
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

                                // Get 2D Angle Information
                            }
                            else
                            {
                                position_2d_arr.Add((double)0);
                                position_2d_arr.Add((double)0);
                            }
                            position_2d_dict.Add(joint_idx, position_2d_arr);

                            Vector3? position_3d = calibration.TransformTo3D(joint.Position,
                                CalibrationDeviceType.Depth,
                                CalibrationDeviceType.Color);
                            ArrayList position_3d_arr = new ArrayList();
                            Dictionary<string, object> angle_3d_value = new Dictionary<string, object>();
                            if (position_3d.HasValue)
                            {
                                // Get 3D Position Information
                                position_3d_arr.Add((double)position_3d.Value.X);
                                position_3d_arr.Add((double)position_3d.Value.Y);
                                position_3d_arr.Add((double)position_3d.Value.Z);

                                // Get 3D Angle Information
                                angle_3d_value.Add("x", Calc3DXAngle(position_3d));
                                angle_3d_value.Add("y", Calc3DYAngle(position_3d));
                                angle_3d_value.Add("z", Calc3DZAngle(position_3d));
                            }
                            else
                            {
                                position_3d_arr.Add((double)0);
                                position_3d_arr.Add((double)0);
                                position_3d_arr.Add((double)0);

                                angle_3d_value.Add("x", 0);
                                angle_3d_value.Add("y", 0);
                                angle_3d_value.Add("z", 0);
                            }
                            position_3d_dict.Add(joint_idx, position_3d_arr);
                            point_angle_dict.Add(joint_idx, angle_3d_value);

                            // Get Position Axies Information
                            Dictionary<string, object> axies_value = new Dictionary<string, object>();
                            double quaternion_x = joint.Quaternion.X;
                            double quaternion_y = joint.Quaternion.Y;
                            double quaternion_z = joint.Quaternion.Z;
                            double quaternion_w = joint.Quaternion.W;

                            double pitch = CalcPitch(quaternion_x, quaternion_y, quaternion_z, quaternion_w);
                            double yaw = CalcYaw(quaternion_x, quaternion_y, quaternion_z, quaternion_w);
                            double roll = CalcRoll(quaternion_x, quaternion_y, quaternion_z, quaternion_w);
                            axies_value.Add("pitch",pitch);
                            axies_value.Add("yaw",yaw);
                            axies_value.Add("roll",roll);
                            point_axies_dict.Add(joint_idx, axies_value);
                        }

                        // Get 2D & 3D Vector Angle Information
                        ArrayList vectors = VectorPair(position_2d_dict, position_3d_dict);

                        id_dict.Add("position_2d", position_2d_dict);
                        id_dict.Add("position_3d", position_3d_dict);
                        id_dict.Add("vector_angle", vectors);
                        id_dict.Add("point_3d_angle", point_angle_dict);
                        id_dict.Add("point_axies", point_axies_dict);

                        annotation_arr.Add(id_dict);
                    }
                }
            }
            else
            {
                id_dict.Add("id", "0");
                id_dict.Add("target", target_chk.ToString());

                id_dict.Add("position_2d", "");
                id_dict.Add("position_3d", "");
                id_dict.Add("vector_angle", "");
                id_dict.Add("point_3d_angle", "");
                id_dict.Add("point_axies", "");

                annotation_arr.Add(id_dict);
            }

            frame.Dispose();

            Dictionary<int, string> description_dict = JointMap();
            annotation_dict.Add("description", description_dict);
            annotation_dict.Add("annotation", annotation_arr);

            string annotation_json = JsonConvert.SerializeObject(annotation_dict, Formatting.Indented);

            return annotation_json;
        }

        // Joint Map Description
        public Dictionary<int, string> JointMap()
        {
            Dictionary<int, string> description_dict = new Dictionary<int, string>();
            description_dict.Add(0, "pelvis");
            description_dict.Add(1, "spine_naval");
            description_dict.Add(2, "spine_chest");
            description_dict.Add(3, "neck");
            description_dict.Add(4, "clavicle_left");
            description_dict.Add(5, "shoulder_left");
            description_dict.Add(6, "elbow_left");
            description_dict.Add(7, "wrist_left");
            description_dict.Add(8, "hand_left");
            description_dict.Add(9, "handtip_left");
            description_dict.Add(10, "thumb_left");
            description_dict.Add(11, "clavicle_right");
            description_dict.Add(12, "shoulder_right");
            description_dict.Add(13, "elbow_right");
            description_dict.Add(14, "wrist_right");
            description_dict.Add(15, "hand_right");
            description_dict.Add(16, "handtip_right");
            description_dict.Add(17, "thumb_right");
            description_dict.Add(18, "hip_left");
            description_dict.Add(19, "knee_left");
            description_dict.Add(20, "ankle_left");
            description_dict.Add(21, "foot_left");
            description_dict.Add(22, "hip_right");
            description_dict.Add(23, "knee_right");
            description_dict.Add(24, "ankle_right");
            description_dict.Add(25, "foot_right");
            description_dict.Add(26, "head");
            description_dict.Add(27, "nose");
            description_dict.Add(28, "eye_left");
            description_dict.Add(29, "ear_left");
            description_dict.Add(30, "eye_right");
            description_dict.Add(31, "ear_right");

            return description_dict;
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

        // Get Vector Angle Pair
        public ArrayList VectorPair(Dictionary<int, object> position_2d_dict, Dictionary<int, object> position_3d_dict)
        {
            ArrayList pair_arr = new ArrayList();
            // SPINE_NAVAL(1) - PELVIS(0) - HIP_LEFT(18)
            double pair0_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[18]);
            double pair0_3d = Calc3DVector( (ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[18]);
            Dictionary<string, object> pair0_value = new Dictionary<string, object>();
            pair0_value.Add("id",0);
            pair0_value.Add("pair", "spine_naval(1)-pelvis(0)-hip_left(18)");
            pair0_value.Add("target", "pelvis(0)");
            pair0_value.Add("2d", pair0_2d);
            pair0_value.Add("3d", pair0_3d);
            pair_arr.Add(pair0_value);

            // SPINE_NAVAL(1) - PELVIS(0) - HIP_RIGHT(22)
            double pair1_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[22]);
            double pair1_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[22]);
            Dictionary<string, object> pair1_value = new Dictionary<string, object>();
            pair1_value.Add("id", 1);
            pair1_value.Add("pair", "spine_naval(1)-pelvis(0)-hip_right(22)");
            pair1_value.Add("target", "pelvis(0)");
            pair1_value.Add("2d", pair1_2d);
            pair1_value.Add("3d", pair1_3d);
            pair_arr.Add(pair1_value);

            // HIP_LEFT(18) - PELVIS(0) - HIP_RIGHT(22)
            double pair2_2d = Calc2DVector((ArrayList)position_2d_dict[18], (ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[22]);
            double pair2_3d = Calc3DVector((ArrayList)position_3d_dict[18], (ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[22]);
            Dictionary<string, object> pair2_value = new Dictionary<string, object>();
            pair2_value.Add("id", 2);
            pair2_value.Add("pair", "hip_left(18)-pelvis(0)-hip_right(22)");
            pair2_value.Add("target", "pelvis(0)");
            pair2_value.Add("2d", pair2_2d);
            pair2_value.Add("3d", pair2_3d);
            pair_arr.Add(pair2_value);

            // PELVIS(0) - SPINE_NAVAL(1) - SPINE_CHEST(2)
            double pair3_2d = Calc2DVector((ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[2]);
            double pair3_3d = Calc3DVector((ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[2]);
            Dictionary<string, object> pair3_value = new Dictionary<string, object>();
            pair3_value.Add("id", 3);
            pair3_value.Add("pair", "pelvis(0)-spine_naval(1)-spine_chest(2)");
            pair3_value.Add("target", "spine_naval(1)");
            pair3_value.Add("2d", pair3_2d);
            pair3_value.Add("3d", pair3_3d);
            pair_arr.Add(pair3_value);

            // SPINE_NAVAL(1) - SPINE_CHEST(2) - CLAVICLE_LEFT(4)
            double pair4_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[4]);
            double pair4_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[4]);
            Dictionary<string, object> pair4_value = new Dictionary<string, object>();
            pair4_value.Add("id", 4);
            pair4_value.Add("pair", "spine_naval(1)-spine_chest(2)-clavicle_left(4)");
            pair4_value.Add("target", "spine_chest(2)");
            pair4_value.Add("2d", pair4_2d);
            pair4_value.Add("3d", pair4_3d);
            pair_arr.Add(pair4_value);

            // SPINE_NAVAL(1) - SPINE_CHEST(2) - NECK(3)
            double pair5_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[3]);
            double pair5_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[3]);
            Dictionary<string, object> pair5_value = new Dictionary<string, object>();
            pair5_value.Add("id", 5);
            pair5_value.Add("pair", "spine_naval(1)-spine_chest(2)-neck(3)");
            pair5_value.Add("target", "spine_chest(2)");
            pair5_value.Add("2d", pair5_2d);
            pair5_value.Add("3d", pair5_3d);
            pair_arr.Add(pair5_value);

            // SPINE_NAVAL(1) - SPINE_CHEST(2) - CLAVICLE_RIGHT(11)
            double pair6_2d = Calc2DVector((ArrayList)position_2d_dict[1], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[11]);
            double pair6_3d = Calc3DVector((ArrayList)position_3d_dict[1], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[11]);
            Dictionary<string, object> pair6_value = new Dictionary<string, object>();
            pair6_value.Add("id", 6);
            pair6_value.Add("pair", "spine_naval(1)-spine_chest(2)-clavicle_right(11)");
            pair6_value.Add("target", "spine_chest(2)");
            pair6_value.Add("2d", pair6_2d);
            pair6_value.Add("3d", pair6_3d);
            pair_arr.Add(pair6_value);

            // CLAVICLE_LEFT(4) - SPINE_CHEST(2) - NECK(3)
            double pair7_2d = Calc2DVector((ArrayList)position_2d_dict[4], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[3]);
            double pair7_3d = Calc3DVector((ArrayList)position_3d_dict[4], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[3]);
            Dictionary<string, object> pair7_value = new Dictionary<string, object>();
            pair7_value.Add("id", 7);
            pair7_value.Add("pair", "clavicle_left(4)-spine_chest(2)-neck(3)");
            pair7_value.Add("target", "spine_chest(2)");
            pair7_value.Add("2d", pair7_2d);
            pair7_value.Add("3d", pair7_3d);
            pair_arr.Add(pair7_value);

            // CLAVICLE_LEFT(4) - SPINE_CHEST(2) - CLAVICLE_RIGHT(11)
            double pair8_2d = Calc2DVector((ArrayList)position_2d_dict[4], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[11]);
            double pair8_3d = Calc3DVector((ArrayList)position_3d_dict[4], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[11]);
            Dictionary<string, object> pair8_value = new Dictionary<string, object>();
            pair8_value.Add("id", 8);
            pair8_value.Add("pair", "clavicle_left(4)-spine_chest(2)-clavicle_right(11)");
            pair8_value.Add("target", "spine_chest(2)");
            pair8_value.Add("2d", pair8_2d);
            pair8_value.Add("3d", pair8_3d);
            pair_arr.Add(pair8_value);

            // NECK(3) - SPINE_CHEST(2) - CLAVICLE_RIGHT(11)
            double pair9_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[11]);
            double pair9_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[11]);
            Dictionary<string, object> pair9_value = new Dictionary<string, object>();
            pair9_value.Add("id", 9);
            pair9_value.Add("pair", "neck(3)-spine_chest(2)-clavicle_right(11)");
            pair9_value.Add("target", "spine_chest(2)");
            pair9_value.Add("2d", pair9_2d);
            pair9_value.Add("3d", pair9_3d);
            pair_arr.Add(pair9_value);

            // SPINE_CHEST(2) - NECK(3) - HEAD(26)
            double pair10_2d = Calc2DVector((ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26]);
            double pair10_3d = Calc3DVector((ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26]);
            Dictionary<string, object> pair10_value = new Dictionary<string, object>();
            pair10_value.Add("id", 10);
            pair10_value.Add("pair", "spine_chest(2)-neck(3)-head(26)");
            pair10_value.Add("target", "neck(3)");
            pair10_value.Add("2d", pair10_2d);
            pair10_value.Add("3d", pair10_3d);
            pair_arr.Add(pair10_value);

            // SPINE_CHEST(2) - CLAVICLE_LEFT(4) - SHOULDER_LEFT(5)
            double pair11_2d = Calc2DVector((ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[4], (ArrayList)position_2d_dict[5]);
            double pair11_3d = Calc3DVector((ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[4], (ArrayList)position_3d_dict[5]);
            Dictionary<string, object> pair11_value = new Dictionary<string, object>();
            pair11_value.Add("id", 11);
            pair11_value.Add("pair", "spine_chest(2)-clavicle_left(4)-shoulder_left(5)");
            pair11_value.Add("target", "clavicle_left(4)");
            pair11_value.Add("2d", pair11_2d);
            pair11_value.Add("3d", pair11_3d);
            pair_arr.Add(pair11_value);

            // CLAVICLE_LEFT(4) - SHOULDER_LEFT(5) - ELVOW_LEFT(6)
            double pair12_2d = Calc2DVector((ArrayList)position_2d_dict[4], (ArrayList)position_2d_dict[5], (ArrayList)position_2d_dict[6]);
            double pair12_3d = Calc3DVector((ArrayList)position_3d_dict[4], (ArrayList)position_3d_dict[5], (ArrayList)position_3d_dict[6]);
            Dictionary<string, object> pair12_value = new Dictionary<string, object>();
            pair12_value.Add("id", 12);
            pair12_value.Add("pair", "clavicle_left(4)-shoulder_left(5)-elvow_left(6)");
            pair12_value.Add("target", "shoulder_left(5)");
            pair12_value.Add("2d", pair12_2d);
            pair12_value.Add("3d", pair12_3d);
            pair_arr.Add(pair12_value);

            // SHOULDER_LEFT(5) - ELBOW_LEFT(6) - WRIST_LEFT(7)
            double pair13_2d = Calc2DVector((ArrayList)position_2d_dict[5], (ArrayList)position_2d_dict[6], (ArrayList)position_2d_dict[7]);
            double pair13_3d = Calc3DVector((ArrayList)position_3d_dict[5], (ArrayList)position_3d_dict[6], (ArrayList)position_3d_dict[7]);
            Dictionary<string, object> pair13_value = new Dictionary<string, object>();
            pair13_value.Add("id", 13);
            pair13_value.Add("pair", "shoulder_left(5)-elvow_left(6)-wrist_left(7)");
            pair13_value.Add("target", "elvow_left(6)");
            pair13_value.Add("2d", pair13_2d);
            pair13_value.Add("3d", pair13_3d);
            pair_arr.Add(pair13_value);

            // ELBOW_LEFT(6) - WRIST_LEFT(7) - HAND_LEFT(8)
            double pair14_2d = Calc2DVector((ArrayList)position_2d_dict[6], (ArrayList)position_2d_dict[7], (ArrayList)position_2d_dict[8]);
            double pair14_3d = Calc3DVector((ArrayList)position_3d_dict[6], (ArrayList)position_3d_dict[7], (ArrayList)position_3d_dict[8]);
            Dictionary<string, object> pair14_value = new Dictionary<string, object>();
            pair14_value.Add("id", 14);
            pair14_value.Add("pair", "elvow_left(6)-wrist_left(7)-hand_left(8)");
            pair14_value.Add("target", "wrist_left(7)");
            pair14_value.Add("2d", pair14_2d);
            pair14_value.Add("3d", pair14_3d);
            pair_arr.Add(pair14_value);

            // ELBOW_LEFT(6) - WRIST_LEFT(7) - THUMB_LEFT(10)
            double pair15_2d = Calc2DVector((ArrayList)position_2d_dict[6], (ArrayList)position_2d_dict[7], (ArrayList)position_2d_dict[10]);
            double pair15_3d = Calc3DVector((ArrayList)position_3d_dict[6], (ArrayList)position_3d_dict[7], (ArrayList)position_3d_dict[10]);
            Dictionary<string, object> pair15_value = new Dictionary<string, object>();
            pair15_value.Add("id", 15);
            pair15_value.Add("pair", "elvow_left(6)-wrist_left(7)-thumb_left(10)");
            pair15_value.Add("target", "wrist_left(7)");
            pair15_value.Add("2d", pair15_2d);
            pair15_value.Add("3d", pair15_3d);
            pair_arr.Add(pair15_value);

            // WRIST_LEFT(7) - HAND_LEFT(8) - HANDTIP_LEFT(9)
            double pair16_2d = Calc2DVector((ArrayList)position_2d_dict[7], (ArrayList)position_2d_dict[8], (ArrayList)position_2d_dict[9]);
            double pair16_3d = Calc3DVector((ArrayList)position_3d_dict[7], (ArrayList)position_3d_dict[8], (ArrayList)position_3d_dict[9]);
            Dictionary<string, object> pair16_value = new Dictionary<string, object>();
            pair16_value.Add("id", 16);
            pair16_value.Add("pair", "wrist_left(7)-hand_left(8)-handtip_left(9)");
            pair16_value.Add("target", "hand_left(8)");
            pair16_value.Add("2d", pair16_2d);
            pair16_value.Add("3d", pair16_3d);
            pair_arr.Add(pair16_value);

            // SPINE_CHEST(2) - CLAVICLE_RIGHT(11) - SHOULDER_RIGHT(12)
            double pair17_2d = Calc2DVector((ArrayList)position_2d_dict[2], (ArrayList)position_2d_dict[11], (ArrayList)position_2d_dict[12]);
            double pair17_3d = Calc3DVector((ArrayList)position_3d_dict[2], (ArrayList)position_3d_dict[11], (ArrayList)position_3d_dict[12]);
            Dictionary<string, object> pair17_value = new Dictionary<string, object>();
            pair17_value.Add("id", 17);
            pair17_value.Add("pair", "spine_chest(2)-clavicle_right(11)-shoulder_right(12)");
            pair17_value.Add("target", "clavicle_right(11)");
            pair17_value.Add("2d", pair17_2d);
            pair17_value.Add("3d", pair17_3d);
            pair_arr.Add(pair17_value);

            // CLAVICLE_RIGHT(11) - SHOULDER_RIGHT(12) - ELBOW_RIGHT(13)
            double pair18_2d = Calc2DVector((ArrayList)position_2d_dict[11], (ArrayList)position_2d_dict[12], (ArrayList)position_2d_dict[13]);
            double pair18_3d = Calc3DVector((ArrayList)position_3d_dict[11], (ArrayList)position_3d_dict[12], (ArrayList)position_3d_dict[13]);
            Dictionary<string, object> pair18_value = new Dictionary<string, object>();
            pair18_value.Add("id", 18);
            pair18_value.Add("pair", "clavicle_right(11)-shoulder_right(12)-elbow_right(13)");
            pair18_value.Add("target", "shoulder_right(12)");
            pair18_value.Add("2d", pair18_2d);
            pair18_value.Add("3d", pair18_3d);
            pair_arr.Add(pair18_value);

            // SHOULDER_RIGHT(12) - ELBOW_RIGHT(13) - WRIST_RIGHT(14)
            double pair19_2d = Calc2DVector((ArrayList)position_2d_dict[12], (ArrayList)position_2d_dict[13], (ArrayList)position_2d_dict[14]);
            double pair19_3d = Calc3DVector((ArrayList)position_3d_dict[12], (ArrayList)position_3d_dict[13], (ArrayList)position_3d_dict[14]);
            Dictionary<string, object> pair19_value = new Dictionary<string, object>();
            pair19_value.Add("id", 19);
            pair19_value.Add("pair", "shoulder_right(12)-elbow_right(13)-wrist_right(14)");
            pair19_value.Add("target", "elbow_right(13)");
            pair19_value.Add("2d", pair19_2d);
            pair19_value.Add("3d", pair19_3d);
            pair_arr.Add(pair19_value);

            // ELBOW_RIGHT(13) - WRIST_RIGHT(14) - HAND_RIGHT(15)
            double pair20_2d = Calc2DVector((ArrayList)position_2d_dict[13], (ArrayList)position_2d_dict[14], (ArrayList)position_2d_dict[15]);
            double pair20_3d = Calc3DVector((ArrayList)position_3d_dict[13], (ArrayList)position_3d_dict[14], (ArrayList)position_3d_dict[15]);
            Dictionary<string, object> pair20_value = new Dictionary<string, object>();
            pair20_value.Add("id", 20);
            pair20_value.Add("pair", "elbow_right(13)-wrist_right(14)-hand_right(15)");
            pair20_value.Add("target", "wrist_right(14)");
            pair20_value.Add("2d", pair20_2d);
            pair20_value.Add("3d", pair20_3d);
            pair_arr.Add(pair20_value);

            // ELBOW_RIGHT(13) - WRIST_RIGHT(14) - THUMB_RIGHT(17)
            double pair21_2d = Calc2DVector((ArrayList)position_2d_dict[13], (ArrayList)position_2d_dict[14], (ArrayList)position_2d_dict[17]);
            double pair21_3d = Calc3DVector((ArrayList)position_3d_dict[13], (ArrayList)position_3d_dict[14], (ArrayList)position_3d_dict[17]);
            Dictionary<string, object> pair21_value = new Dictionary<string, object>();
            pair21_value.Add("id", 21);
            pair21_value.Add("pair", "elbow_right(13)-wrist_right(14)-thumb_right(17)");
            pair21_value.Add("target", "wrist_right(14)");
            pair21_value.Add("2d", pair21_2d);
            pair21_value.Add("3d", pair21_3d);
            pair_arr.Add(pair21_value);

            // WRIST_RIGHT(14) - HAND_RIGHT(15) - HANDTIP_RIGHT(16)
            double pair22_2d = Calc2DVector((ArrayList)position_2d_dict[14], (ArrayList)position_2d_dict[15], (ArrayList)position_2d_dict[16]);
            double pair22_3d = Calc3DVector((ArrayList)position_3d_dict[14], (ArrayList)position_3d_dict[15], (ArrayList)position_3d_dict[16]);
            Dictionary<string, object> pair22_value = new Dictionary<string, object>();
            pair22_value.Add("id", 22);
            pair22_value.Add("pair", "wrist_right(14)-hand_right(15)-handtip_right(16)");
            pair22_value.Add("target", "hand_right(15)");
            pair22_value.Add("2d", pair22_2d);
            pair22_value.Add("3d", pair22_3d);
            pair_arr.Add(pair22_value);

            // PELVIS(0) - HIP_LEFT(18) - KNEE_LEFT(19)
            double pair23_2d = Calc2DVector((ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[18], (ArrayList)position_2d_dict[19]);
            double pair23_3d = Calc3DVector((ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[18], (ArrayList)position_3d_dict[19]);
            Dictionary<string, object> pair23_value = new Dictionary<string, object>();
            pair23_value.Add("id", 23);
            pair23_value.Add("pair", "pelvis(0)-hip_left(18)-knee_left(19)");
            pair23_value.Add("target", "hip_left(18)");
            pair23_value.Add("2d", pair23_2d);
            pair23_value.Add("3d", pair23_3d);
            pair_arr.Add(pair23_value);

            // HIP_LEFT(18) - KNEE_LEFT(19) - ANKLE_LEFT(20)
            double pair24_2d = Calc2DVector((ArrayList)position_2d_dict[18], (ArrayList)position_2d_dict[19], (ArrayList)position_2d_dict[20]);
            double pair24_3d = Calc3DVector((ArrayList)position_3d_dict[18], (ArrayList)position_3d_dict[19], (ArrayList)position_3d_dict[20]);
            Dictionary<string, object> pair24_value = new Dictionary<string, object>();
            pair24_value.Add("id", 24);
            pair24_value.Add("pair", "hip_left(18)-knee_left(19)-ankle_left(20)");
            pair24_value.Add("target", "knee_left(19)");
            pair24_value.Add("2d", pair24_2d);
            pair24_value.Add("3d", pair24_3d);
            pair_arr.Add(pair24_value);

            // KNEE_LEFT(19) - ANKLE_LEFT(20) - FOOT_LEFT(21)
            double pair25_2d = Calc2DVector((ArrayList)position_2d_dict[19], (ArrayList)position_2d_dict[20], (ArrayList)position_2d_dict[21]);
            double pair25_3d = Calc3DVector((ArrayList)position_3d_dict[19], (ArrayList)position_3d_dict[20], (ArrayList)position_3d_dict[21]);
            Dictionary<string, object> pair25_value = new Dictionary<string, object>();
            pair25_value.Add("id", 25);
            pair25_value.Add("pair", "knee_left(19)-ankle_left(20)-foot_left(21)");
            pair25_value.Add("target", "ankle_left(20)");
            pair25_value.Add("2d", pair25_2d);
            pair25_value.Add("3d", pair25_3d);
            pair_arr.Add(pair25_value);

            // PELVIS(0) - HIP_RIGHT(22) - KNEE_RIGHT(23)
            double pair26_2d = Calc2DVector((ArrayList)position_2d_dict[0], (ArrayList)position_2d_dict[22], (ArrayList)position_2d_dict[23]);
            double pair26_3d = Calc3DVector((ArrayList)position_3d_dict[0], (ArrayList)position_3d_dict[22], (ArrayList)position_3d_dict[23]);
            Dictionary<string, object> pair26_value = new Dictionary<string, object>();
            pair26_value.Add("id", 26);
            pair26_value.Add("pair", "pelvis(0)-hip_right(22)-knee_right(23)");
            pair26_value.Add("target", "hip_right(22)");
            pair26_value.Add("2d", pair26_2d);
            pair26_value.Add("3d", pair26_3d);
            pair_arr.Add(pair26_value);

            // HIP_RIGHT(22) - KNEE_RIGHT(23) - ANKLE_RIGHT(24)
            double pair27_2d = Calc2DVector((ArrayList)position_2d_dict[22], (ArrayList)position_2d_dict[23], (ArrayList)position_2d_dict[24]);
            double pair27_3d = Calc3DVector((ArrayList)position_3d_dict[22], (ArrayList)position_3d_dict[23], (ArrayList)position_3d_dict[24]);
            Dictionary<string, object> pair27_value = new Dictionary<string, object>();
            pair27_value.Add("id", 27);
            pair27_value.Add("pair", "hip_right(22)-knee_right(23)-ankle_right(24)");
            pair27_value.Add("target", "knee_right(23)");
            pair27_value.Add("2d", pair27_2d);
            pair27_value.Add("3d", pair27_3d);
            pair_arr.Add(pair27_value);

            // KNEE_RIGHT(23) - ANKLE_RIGHT(24) - FOOT_RIGHT(25)
            double pair28_2d = Calc2DVector((ArrayList)position_2d_dict[23], (ArrayList)position_2d_dict[24], (ArrayList)position_2d_dict[25]);
            double pair28_3d = Calc3DVector((ArrayList)position_3d_dict[23], (ArrayList)position_3d_dict[24], (ArrayList)position_3d_dict[25]);
            Dictionary<string, object> pair28_value = new Dictionary<string, object>();
            pair28_value.Add("id", 28);
            pair28_value.Add("pair", "knee_right(23)-ankle_right(24)-foot_right(25)");
            pair28_value.Add("target", "ankle_right(24)");
            pair28_value.Add("2d", pair28_2d);
            pair28_value.Add("3d", pair28_3d);
            pair_arr.Add(pair28_value);

            // NECK(3) - HEAD(26) - NOSE(27)
            double pair29_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[27]);
            double pair29_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[27]);
            Dictionary<string, object> pair29_value = new Dictionary<string, object>();
            pair29_value.Add("id", 29);
            pair29_value.Add("pair", "neck(3)-head(26)-nose(27)");
            pair29_value.Add("target", "head(26)");
            pair29_value.Add("2d", pair29_2d);
            pair29_value.Add("3d", pair29_3d);
            pair_arr.Add(pair29_value);

            // NECK(3) - HEAD(26) - EYE_LEFT(28)
            double pair30_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[28]);
            double pair30_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[28]);
            Dictionary<string, object> pair30_value = new Dictionary<string, object>();
            pair30_value.Add("id", 30);
            pair30_value.Add("pair", "neck(3)-head(26)-eye_left(28)");
            pair30_value.Add("target", "head(26)");
            pair30_value.Add("2d", pair30_2d);
            pair30_value.Add("3d", pair30_3d);
            pair_arr.Add(pair30_value);

            // NECK(3) - HEAD(26) - EAR_LEFT(29)
            double pair31_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[29]);
            double pair31_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[29]);
            Dictionary<string, object> pair31_value = new Dictionary<string, object>();
            pair31_value.Add("id", 31);
            pair31_value.Add("pair", "neck(3)-head(26)-ear_left(29)");
            pair31_value.Add("target", "head(26)");
            pair31_value.Add("2d", pair31_2d);
            pair31_value.Add("3d", pair31_3d);
            pair_arr.Add(pair31_value);

            // NECK(3) - HEAD(26) - EYE_RIGHT(30)
            double pair32_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[30]);
            double pair32_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[30]);
            Dictionary<string, object> pair32_value = new Dictionary<string, object>();
            pair32_value.Add("id", 32);
            pair32_value.Add("pair", "neck(3)-head(26)-eye_right(30)");
            pair32_value.Add("target", "head(26)");
            pair32_value.Add("2d", pair32_2d);
            pair32_value.Add("3d", pair32_3d);
            pair_arr.Add(pair32_value);

            // NECK(3) - HEAD(26) - EAR_RIGHT(31)
            double pair33_2d = Calc2DVector((ArrayList)position_2d_dict[3], (ArrayList)position_2d_dict[26], (ArrayList)position_2d_dict[31]);
            double pair33_3d = Calc3DVector((ArrayList)position_3d_dict[3], (ArrayList)position_3d_dict[26], (ArrayList)position_3d_dict[31]);
            Dictionary<string, object> pair33_value = new Dictionary<string, object>();
            pair33_value.Add("id", 33);
            pair33_value.Add("pair", "neck(3)-head(26)-ear_right(31)");
            pair33_value.Add("target", "head(26)");
            pair33_value.Add("2d", pair33_2d);
            pair33_value.Add("3d", pair33_3d);
            pair_arr.Add(pair33_value);

            return pair_arr;
        }
    }
}
