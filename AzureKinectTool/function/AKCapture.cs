using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// Azure Kinect SDK
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;

namespace AzureKinectTool.function
{
    public class AKCapture
    {
        public void AKGetCapture(int sync_mode, ArrayList kinect_list)
        {
            int k_cnt = kinect_list.Count;

            if (sync_mode == 1)
            {
                Dictionary<string, object> sbk_info = (Dictionary<string, object>)kinect_list[0];
                Device sb_kinect = (Device)sbk_info["kinect_device"];
                DeviceConfiguration sbs_config = (DeviceConfiguration)sbk_info["sensor_config"];
                Tracker sb_tracker = (Tracker)sbk_info["kinect_tracker"];

                sb_kinect.StartCameras(sbs_config);
                Thread.Sleep(300);

                Dictionary<string, object>mtk_info = (Dictionary<string, object>)kinect_list[1];
                Device mt_kinect = (Device)mtk_info["kinect_device"];
                DeviceConfiguration mts_config = (DeviceConfiguration)mtk_info["sensor_config"];
                Tracker mt_tracker = (Tracker)mtk_info["kinect_tracker"];

                mt_kinect.StartCameras(mts_config);

                while (true)
                {
                    Capture mt_capture = null;
                    Capture sb_capture = null;
                    Parallel.Invoke(
                        () =>
                        {
                            mt_capture = mt_kinect.GetCapture();
                            mt_tracker.EnqueueCapture(mt_capture);
                        },
                        () =>
                        {
                            Thread.Sleep(2);
                            sb_capture = sb_kinect.GetCapture();
                            sb_tracker.EnqueueCapture(sb_capture);
                        }
                    );
                    if (mt_capture != null && sb_capture != null)
                    {
                        Parallel.Invoke(
                            () => {
                                Image sb_cimg = sb_capture.Color;
                            },
                            () => {
                                Image mt_cimg = mt_capture.Color;
                            },
                            () => {
                                Image sb_dimg = sb_capture.Depth;
                            },
                            () => {
                                Image mt_dimg = mt_capture.Depth;
                            },
                            () => {
                                Image sb_iimg = sb_capture.IR;
                            },
                            () => {
                                Image mt_iimg = mt_capture.IR;
                            },
                            () =>
                            {
                                Frame sb_frame = sb_tracker.PopResult();
                            },
                            () =>
                            {
                                Frame mt_frame = mt_tracker.PopResult();
                            }
                        );
                    }
                }
            }
            else
            {
                Parallel.For(0, k_cnt, k_idx =>
                {
                    Dictionary<string, object> k_info = (Dictionary<string, object>)kinect_list[k_idx];
                    Device kinect = (Device)k_info["kinect_device"];
                    DeviceConfiguration config = (DeviceConfiguration)k_info["sensor_config"];
                    Tracker tracker = (Tracker)k_info["kinect_tracker"];

                    kinect.StartCameras(config);
                    while (true)
                    {
                        Capture capture = kinect.GetCapture();
                        tracker.EnqueueCapture(capture);

                        if (capture != null)
                        {
                            Parallel.Invoke(
                                () => {
                                    Image cimg = capture.Color;
                                },
                                () => {
                                    Image dimg = capture.Depth;
                                },
                                () => {
                                    Image iimg = capture.IR;
                                },
                                () =>
                                {
                                    Frame frame = tracker.PopResult();
                                }
                            );
                        }
                    }
                });
            }
        }
    }
}
