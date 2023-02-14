# AzureKinectTool
- Azure Kinect DK를 최대 2대를 사용하여 Syncronize 및 Standalone Mode로 동작시켜 설정한 Frame Rate로 촬영을 하면서 1 Frame 마다 데이터를 수집하는 도구


# 데이터 유형
- Color Image(jpg)
- Depth Image(16bit png)
- Transformed Depth Image(16bit png)
- Infrared Image(16bit png)
- Calibration Information(json)
- Joint Tracker Data(json)
- Color Video(mp4) --> FFMPEG Library 이용
- CoCo Dataset Format Annotation Data(json)

# 시스템 요구사항
- GPU : RTX 3080 이상
- CPU : intel i5, i7 10th 이상
- RAM : 16GB 이상
- SSD 추천(1Frame마다 Data를 저장하기에 디스크의 읽기/쓰기 기능이 원활해야함)

# 사용 가이드
1. Setting버튼을 선택하여 Azure Kinect Dk의 Sensor 및 Tracker, Data 저장소 위치를 설정한다.
![image](https://user-images.githubusercontent.com/59715960/218610162-756ad42e-5800-4295-9a09-2bb33769b88f.png)

2. Data 저장 기능 활성화 여부에 관한 토글 버튼으로 Deactivate 상태로 전환해준다.
![image](https://user-images.githubusercontent.com/59715960/218610100-0389adf1-1cd8-4c37-9f0c-4603a7da90fa.png)

3. Azure Kinect DK의 전원을 켜주기 위하여 왼쪽의 버튼을 먼저 누르고 파란불로 전환이 되었다면, 우측의 촬영버튼을 눌러서 촬영을 시작한다.
![image](https://user-images.githubusercontent.com/59715960/218610280-6f3caa13-80dc-43bf-9b6f-114747c44935.png)

4. 촬영을 하면서 Azure Kinect DK의 정상 동작 여부 및 촬영 각도, 위치, 조명 등의 환경을 세팅한다.

5. 
