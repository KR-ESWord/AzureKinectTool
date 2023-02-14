// 기본 라이브러리 호출
using System.Collections;
// Azure Kinect SDK 라이브러리 호출
using Microsoft.Azure.Kinect.Sensor;

namespace AzureKinectTool.function
{
    public class AKPower
    {
        // Azure Kinect와 프로그램간 연결 작업 수행 함수
        public ArrayList AKPWON(int device_cnt)
        {
            ArrayList device_list = new ArrayList(); // Azure Kinect device 객체를 저장할 ArrayList 선언
            // 연결된 키넥트의 수 만큼 반복문 실행하여 Azure Kinect와 프로그램간의 연결 실행
            for (int idx = 0; idx < device_cnt; idx++)
            {
                try
                {
                    Device kinect = Device.Open(idx);
                    device_list.Add(kinect);
                }
                catch // 연결이 되지 않았을 경우 해당 Index의 device 객체는 null로 저장
                {
                    Device kinect = null;
                    device_list.Add(kinect);
                }
            }

            return device_list;
        }

        // Azure Kinect와 프로그램간 연결 해제 작업 수행 함수
        public void AKPWOFF(Device kinect)
        {
            kinect.Dispose();
        }

        // 연결되어진 Azure Kinect의 Syncronize 상태 확인 작업 수행 함수
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
