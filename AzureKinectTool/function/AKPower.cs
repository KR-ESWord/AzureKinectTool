using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Azure Kinect SDK
using Microsoft.Azure.Kinect.Sensor;

namespace AzureKinectTool.function
{
    public class AKPower
    {
        public ArrayList AKPWON(int device_cnt)
        {
            ArrayList device_list = new ArrayList();

            for (int idx = 0; idx < device_cnt; idx++)
            {
                try
                {
                    Device kinect = Device.Open(idx);
                    device_list.Add(kinect);
                }
                catch
                {
                    Device kinect = null;
                    device_list.Add(kinect);
                }
            }

            return device_list;
        }

        public void AKPWOFF(Device kinect)
        {
            kinect.Dispose();
        }

        public string AKSync(Device kinect)
        {
            string sync_mode = "";

            bool syncIn = kinect.SyncInJackConnected;
            bool syncOut = kinect.SyncOutJackConnected;

            if (!syncIn && !syncOut)
            {
                sync_mode = "StandAlone";
            }
            else if (!syncIn && syncOut)
            {
                sync_mode = "Master";
            }
            else
            {
                sync_mode = "Subordinate";
            }

            return sync_mode;
        }
    }
}
