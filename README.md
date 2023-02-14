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
2. ![image](https://user-images.githubusercontent.com/59715960/218610100-0389adf1-1cd8-4c37-9f0c-4603a7da90fa.png)
