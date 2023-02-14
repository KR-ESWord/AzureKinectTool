using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureKinectTool.function
{
    public class CocoCreater
    {
        public void CocoSet(string storage, string subject_id, string date, string game_info, Dictionary<int, string> capture_time_dict)
        {
            Dictionary<string, object> coco_dict = new Dictionary<string, object>();
            Dictionary<string, string> empty_dict = new Dictionary<string, string>();
            ArrayList empty_list = new ArrayList();

            var basic_path = Path.Combine(storage, "AzureKinectData",subject_id, date, game_info); // 저장 경로 지정
            DirectoryInfo basic_dir = new DirectoryInfo(basic_path);
            if (!basic_dir.Exists) // 여기서 저장 경로의 폴더가 없는 경우는 Data 저장을 비활성화 하였기에 함수를 바로 빠져나오도록 구성
            {
                return;
            }
            else
            {
                // Calibration 정보 추출
                ArrayList calib_info = new ArrayList();
                var calib_path = Path.Combine(basic_path, "0_calibration");
                DirectoryInfo calib_dir = new DirectoryInfo(calib_path);
                if (!calib_dir.Exists)// Not Exist Calibration File
                {
                    Dictionary<string, object> calib_dict = new Dictionary<string, object>();
                    calib_dict.Add("id", -1);
                    calib_dict.Add("name", "None");
                    calib_dict.Add("instrinsics", empty_list);
                    calib_dict.Add("extrinsics", empty_list);
                    calib_dict.Add("convert_mode", empty_list);
                    calib_info.Add(calib_dict);
                }
                else// Exist Calibration File
                {
                    foreach(FileInfo calib_json in calib_dir.GetFiles())
                    {
                        if (calib_json.Extension.ToLower().CompareTo(".json") == 0)
                        {
                            string file_name_full = calib_json.Name.Substring(0, calib_json.Name.Length);
                            var file_path = Path.Combine(calib_path, file_name_full);
                            
                            using (StreamReader calib_file = File.OpenText(file_path))
                            using (JsonTextReader reader = new JsonTextReader(calib_file))
                            {
                                string file_name = calib_json.Name.Substring(0, calib_json.Name.Length - 4);
                                string[] name_list = file_name.Split('_');

                                string cap_sub = name_list[0];
                                string cap_date = name_list[1];
                                string cap_loc = name_list[2];
                                string cap_game = name_list[3];

                                int camera_id = 0;
                                string camera_name = "";
                                if (cap_loc.Equals("kf"))
                                {
                                    camera_id = 0;
                                    camera_name = "Azure Kinect Front";
                                }
                                else if (cap_loc.Equals("kl"))
                                {
                                    camera_id = 1;
                                    camera_name = "Azure Kinect Left";
                                }
                                else
                                {
                                    camera_id = 2;
                                    camera_name = "Azure Kinect Right";
                                }

                                JObject json = (JObject)JToken.ReadFrom(reader);
                                var instrinsics_info = json.SelectToken("instrinsics");
                                var extrinsics_info = json.SelectToken("extrinsics");
                                var convert_info = json.SelectToken("convert_mode");

                                Dictionary<string, object> calib_dict = new Dictionary<string, object>();
                                calib_dict.Add("id", camera_id);
                                calib_dict.Add("name", camera_name);
                                calib_dict.Add("instrinsics", instrinsics_info);
                                calib_dict.Add("extrinsics", extrinsics_info);
                                calib_dict.Add("convert_mode", convert_info);
                                calib_info.Add(calib_dict);
                            }
                        }

                    }
                }

                // Image 목록 정보 추출
                ArrayList images_list = new ArrayList();
                var color_path = Path.Combine(basic_path, "1_color");
                DirectoryInfo color_dir = new DirectoryInfo(color_path);
                if (!color_dir.Exists)// Not Exist Color File
                {
                    Dictionary<string, object> img_dict = new Dictionary<string, object>();
                    img_dict.Add("camera_id", -1);
                    img_dict.Add("video_id", -1);
                    img_dict.Add("v_images", empty_list);
                    images_list.Add(img_dict);
                }
                else// Exist Color File
                {
                    string[] cl_dirs = Directory.GetDirectories(@color_path, "*", SearchOption.TopDirectoryOnly);
                    foreach (string cl_dir in cl_dirs)
                    {
                        int cv_id = 0;
                        
                        var cl_path = Path.Combine(color_path, cl_dir);
                        DirectoryInfo cl_dr = new DirectoryInfo(cl_path);
                        Dictionary<string, object> img_dict = new Dictionary<string, object>();
                        ArrayList v_img_list = new ArrayList();
                        int v_img_id = 0;
                        foreach (FileInfo File in cl_dr.GetFiles())
                        {
                            if (File.Extension.ToLower().CompareTo(".jpg") == 0) //jpg만
                            {
                                string file_name_full = File.Name.Substring(0, File.Name.Length);
                                string file_name = File.Name.Substring(0, File.Name.Length - 4);
                                string[] name_list = file_name.Split('_');
                                string cap_sub = name_list[0];
                                string cap_date = name_list[1];
                                string cap_loc = name_list[2];
                                if (cap_loc.Equals("kf"))
                                {
                                    cv_id = 0;
                                }
                                else if (cap_loc.Equals("kl"))
                                {
                                    cv_id = 1;
                                }
                                else
                                {
                                    cv_id = 2;
                                }
                                string cap_game = name_list[3];
                                string cap_frame = name_list[5].Split('.')[0];
                                int frame_num = int.Parse(cap_frame);

                                // Data Capture Time
                                string date_capture = capture_time_dict[frame_num];

                                string url = "./" + cap_sub + "/" + cap_date + "/" + cap_game + "/1_color/"+ cl_dir + "/" + file_name_full;
                                Dictionary<string, object> v_img_dict = new Dictionary<string, object>();
                                v_img_dict.Add("file_name", file_name);
                                v_img_dict.Add("date_captured", date_capture);
                                v_img_dict.Add("url", url);
                                v_img_dict.Add("id", v_img_id);
                                v_img_list.Add(v_img_dict);

                                v_img_id += 1;
                            }
                        }
                        img_dict.Add("camera_id", cv_id);
                        img_dict.Add("video_id", cv_id);
                        img_dict.Add("v_images", v_img_list);
                        images_list.Add(img_dict);
                    }
                }

                // Video 목록 정보 추출
                ArrayList video_list = new ArrayList();
                var video_path = Path.Combine(basic_path, "6_video");
                DirectoryInfo video_dir = new DirectoryInfo(video_path);
                if (!video_dir.Exists)// Not Exist Video File
                {
                    Dictionary<string, object> video_dict = new Dictionary<string, object>();

                    video_dict.Add("license", 1);
                    video_dict.Add("id", -1);
                    video_dict.Add("file_name", "None");
                    video_dict.Add("url", "None");
                    video_dict.Add("camera_id", -1);
                    video_list.Add(video_dict);
                }
                else// Exist Video File
                {
                    int video_id = 0;
                    foreach (FileInfo File in video_dir.GetFiles())
                    {
                        if (File.Extension.ToLower().CompareTo(".mp4") == 0)
                        {
                            string file_name_full = File.Name.Substring(0, File.Name.Length);
                            string file_name = File.Name.Substring(0, File.Name.Length - 4);
                            string[] name_list = file_name.Split('_');
                            string cap_sub = name_list[0];
                            string cap_date = name_list[1];
                            string cap_loc = name_list[2];
                            string cap_game = name_list[3];

                            string url = "./" + cap_sub + "/"+cap_date+"/"+cap_game+"/6_video/"+ file_name_full;

                            int camera_id = 0;
                            if (cap_loc.Equals("kf"))
                            {
                                camera_id = 0;
                            }
                            else if (cap_loc.Equals("kl"))
                            {
                                camera_id = 1;
                            }
                            else
                            {
                                camera_id = 2;
                            }

                            Dictionary<string, object> video_dict = new Dictionary<string, object>();
                            video_dict.Add("license", 1);
                            video_dict.Add("id", video_id);
                            video_dict.Add("file_name", file_name);
                            video_dict.Add("url", url);
                            video_dict.Add("camera_id", camera_id);
                            video_list.Add(video_dict);

                            video_id += 1;
                        }
                    }
                }

                // joint에서 카테고리 목록 정보 추출
                ArrayList category_list = new ArrayList();
                int cat_flag = 0;
                ArrayList joint_list = new ArrayList();
                var joint_path = Path.Combine(basic_path, "5_joint");
                DirectoryInfo joint_dir = new DirectoryInfo(joint_path);
                if (!joint_dir.Exists) // Not Exist Joint File
                {
                    category_list = empty_list;
                    Dictionary<string, object> joint_dict = new Dictionary<string, object>();
                    joint_dict.Add("camera_id", -1);
                    joint_dict.Add("video_id", -1);
                    joint_dict.Add("v_annotations", empty_dict);
                    joint_list.Add(joint_dict);
                }
                else // Exist Joint File
                {
                    string[] jl_dirs = Directory.GetDirectories(joint_path, "*", SearchOption.TopDirectoryOnly);
                    foreach (string jl_dir in jl_dirs)
                    {
                        int cv_id = 0;
                        var jl_path = Path.Combine(joint_path, jl_dir);
                        DirectoryInfo jl_dr = new DirectoryInfo(jl_path);

                        int anno_id = 0;
                        ArrayList v_anno_list = new ArrayList();
                        foreach (FileInfo anno_json in jl_dr.GetFiles())
                        {
                            if (anno_json.Extension.ToLower().CompareTo(".json") == 0) //json만
                            {
                                string file_name_full = anno_json.Name.Substring(0, anno_json.Name.Length);
                                var file_path = Path.Combine(jl_path, file_name_full);

                                using (StreamReader anno_file = File.OpenText(file_path))
                                using (JsonTextReader reader = new JsonTextReader(anno_file))
                                {
                                    string file_name = anno_json.Name.Substring(0, anno_json.Name.Length - 4);
                                    string[] name_list = file_name.Split('_');
                                    string cap_sub = name_list[0];
                                    string cap_date = name_list[1];
                                    string cap_loc = name_list[2];
                                    if (cap_loc.Equals("kf"))
                                    {
                                        cv_id = 0;
                                    }
                                    else if (cap_loc.Equals("kl"))
                                    {
                                        cv_id = 1;
                                    }
                                    else
                                    {
                                        cv_id = 2;
                                    }
                                    string cap_frame = name_list[5].Split('.')[0];
                                    int frame_num = int.Parse(cap_frame);

                                    JObject json = (JObject)JToken.ReadFrom(reader);
                                    if (cat_flag.Equals(0))
                                    {
                                        var categories = json.SelectToken("categories");
                                        category_list.Add(categories);
                                        cat_flag = 1;
                                    }
                                    // Json Read 후 Body ID와 Target 확인하여 대상자만 추출
                                    var annotations = json.SelectToken("annotations");
                                    int anno_cnt = annotations.Count();
                                    for (int anno_idx = 0; anno_idx < anno_cnt; anno_idx++)
                                    {
                                        Dictionary<string, object> v_anno_dict = new Dictionary<string, object>();

                                        var v_annotations = annotations[anno_idx].SelectToken("v_annotation");
                                        int v_anno_cnt = v_annotations.Count();
                                        int trg_flag = 0;
                                        // 전체 v_annotation 루프
                                        for (int v_anno_idx = 0; v_anno_idx < v_anno_cnt; v_anno_idx++)
                                        {
                                            string chk_tg = v_annotations[v_anno_idx]["target"].ToString();
                                            // Target 일 경우만 추출
                                            if (chk_tg.Equals("1"))
                                            {
                                                trg_flag = 1;
                                                var keypoints = v_annotations[v_anno_idx].SelectToken("keypoints");
                                                var vector_angle = v_annotations[v_anno_idx].SelectToken("vector_angle");

                                                v_anno_dict.Add("segmentation", empty_dict);
                                                v_anno_dict.Add("category_id", (int)v_annotations[v_anno_idx]["category_id"]);
                                                v_anno_dict.Add("id", (int)v_annotations[v_anno_idx]["id"]);
                                                v_anno_dict.Add("image_id", frame_num);
                                                v_anno_dict.Add("iscrowd", 1);
                                                v_anno_dict.Add("keypoints_2d",(int)v_annotations[v_anno_idx]["keypoints_2d"]);
                                                v_anno_dict.Add("keypoints_3d",(int)v_annotations[v_anno_idx]["keypoints_3d"]);
                                                v_anno_dict.Add("keypoints", keypoints);
                                                v_anno_dict.Add("vector_angle", vector_angle);
                                                v_anno_dict.Add("area", 0);
                                                v_anno_dict.Add("bbox", empty_list);
                                            }
                                        }

                                        // Target이 없을 경우
                                        if (trg_flag.Equals(0))
                                        {
                                            v_anno_dict.Add("segmentation", empty_dict);
                                            v_anno_dict.Add("category_id", 0);
                                            v_anno_dict.Add("id", anno_id);
                                            v_anno_dict.Add("image_id", frame_num);
                                            v_anno_dict.Add("iscrowd", 1);
                                            v_anno_dict.Add("keypoints_2d", 0);
                                            v_anno_dict.Add("keypoints_3d", 0);
                                            v_anno_dict.Add("keypoints", empty_list);
                                            v_anno_dict.Add("vector_angle", empty_list);
                                            v_anno_dict.Add("area", 0);
                                            v_anno_dict.Add("bbox", empty_list);
                                        }

                                        v_anno_list.Add(v_anno_dict);
                                    }
                                    anno_id += 1;
                                }
                            }
                        }

                        Dictionary<string, object> joint_dict = new Dictionary<string, object>();
                        joint_dict.Add("camera_id", cv_id);
                        joint_dict.Add("video_id", cv_id);
                        joint_dict.Add("v_annotations", v_anno_list);
                        joint_list.Add(joint_dict);
                    }
                }

                // coco dataset Info 항목 구성
                string date_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                Dictionary<string, string> info_dict = new Dictionary<string, string>();
                info_dict.Add("desciption", "Physical Activity Dataset");
                info_dict.Add("version", "1.0.0");
                info_dict.Add("contributor", "BRFrame, Hanyang Digital Healthcare Center, Kookmin University, HUFS");
                info_dict.Add("date_created", date_time);

                // coco dataset 구성
                coco_dict.Add("info",info_dict);
                coco_dict.Add("lisences",empty_list);
                coco_dict.Add("categories", category_list[0]);
                coco_dict.Add("survey_info", empty_dict);
                coco_dict.Add("inbody_info", empty_dict);
                coco_dict.Add("camera_info", calib_info);
                coco_dict.Add("video_info",video_list);
                coco_dict.Add("images",images_list);
                coco_dict.Add("annotations", joint_list);

                var save_json = JsonConvert.SerializeObject(coco_dict);
                string save_file = subject_id + "_" + date + "_" + game_info + ".json";
                var save_path = Path.Combine(basic_path, "7_annotation", save_file);
                File.WriteAllText(save_path, save_json);
            }
        }
    }
}
