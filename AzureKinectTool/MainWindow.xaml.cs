// 기본 라이브러리 호출
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// Azure Kinect SDK 라이브러리 호출
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;
// MahApps 라이브러리 호출 - UI 관련 라이브러리
using MahApps.Metro.Controls;

namespace AzureKinectTool
{
    public partial class MainWindow : MetroWindow
    {
        // Class 호출 및 선언
        function.AKCalibration AKCalibration = new function.AKCalibration();
        function.AKConfig AKConfig = new function.AKConfig();
        function.AKDataSave AKDataSave = new function.AKDataSave();
        function.AKImageConvert AKImageConvert = new function.AKImageConvert();
        function.AKPower AKPower = new function.AKPower();
        function.AKTracker AKTracker = new function.AKTracker();
        function.CocoCreater CocoCreater = new function.CocoCreater();

        public int init_device_cnt = 0; // 연결된 Azure Kinect 수 Count 관련 전역 변수
        public ArrayList synctxt_list = new ArrayList(); // Azure Kinect 동기화 상태 Text UI 관련 변수 담을 전역 리스트
        public ArrayList loctxt_list = new ArrayList(); // Azure Kinect 위치 Text UI 관련 변수 담을 전역 리스트
        public ArrayList img_list = new ArrayList(); // Azure Kinect의 Capture Image를 표시하기 위한 UI 관련 변수 담을 전역 리스트

        public MainWindow()
        {
            InitializeComponent();

            synctxt_list.Add(SyncText_1); // 왼쪽 동기화 상태 TextBox를 리스트에 추가
            synctxt_list.Add(SyncText_2); // 오른쪽 동기화 상태 TextBox를 리스트에 추가

            loctxt_list.Add(KLocationBox_1); // 왼쪽 Azure Kinect 위치 TextBox를 리스트에 추가
            loctxt_list.Add(KLocationBox_2); // 오른쪽 Azure Kinect 위치 TextBox를 리스트에 추가

            img_list.Add(KImage_1); // 왼쪽 Azure Kinect의 Capture를 표시할 Image UI를 리스트에 추가
            img_list.Add(KImage_2); // 오른쪽 Azure Kinect의 Capture를 표시할 Image UI를 리스트에 추가

            init_device_cnt = Device.GetInstalledCount(); // 프로그램 시작 시 연결되어 있는 Azure Kinect의 수 가져오기
        }

        // Text Integer Input Event //
        // TextBox에 Text 입력 시 숫자만 입력할 수 있게하는 함수
        private void IntegerInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Check Storage Disc Free Space //
        // PC 저장소의 잔여용량을 계산하는 함수로 프로그램 시작 또는 저장소 변경 시 잔여 용량이 100GiB 미만이면 안내창 호출
        public void CheckStorageSpace()
        {
            DriveInfo[] driveInfos = DriveInfo.GetDrives();

            foreach (DriveInfo driveInfo in driveInfos)
            {
                if (driveInfo.DriveType == DriveType.Fixed)
                {
                    if (driveInfo.Name.Contains(SDText.Text))
                    {
                        int total_space = Convert.ToInt32(driveInfo.TotalSize / 1024 / 1024);
                        double free_space = Convert.ToInt32(driveInfo.AvailableFreeSpace / 1024 / 1024) / 1000.0;
                        int used_space = Convert.ToInt32(driveInfo.TotalSize / 1024 / 1024) - Convert.ToInt32(driveInfo.AvailableFreeSpace / 1024 / 1024);

                        DriveBar.Maximum = total_space;
                        DriveBar.Value = used_space;

                        if (free_space < 100)
                        {
                            _ = MessageBox.Show("Not Enough Storage Space!\nCurrent Free Space : " + free_space + "GiB");
                        }
                    }
                }
            }
        }

        // Azure Kinect Setting //
        // Azure Kinect 설정 메뉴 활성화 및 초기 연결된 Azure Kinect의 수 표시하는 함수
        private void AK_Setting(object sender, RoutedEventArgs e)
        {
            AK_Menu.IsOpen = true;

            KCText.Text = init_device_cnt.ToString();
        }

        // Select Storage Disk //
        // PC 저장소 선택하는 버튼 동작 관련 함수로 저장소를 바꿀때 사용되며 저장소를 변경 후 용량을 확인하는 작업 수행
        private void SSButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SDText.Text = fbd.SelectedPath;
                CheckStorageSpace();
            }
        }

        // Storage Free Space Check //
        // 단순히 현재 선택되어져 있는 저장소의 잔여 용량을 새로고침 해주는 동작을 하는 버튼관련 함수
        private void SCButton_Click(object sender, RoutedEventArgs e)
        {
            CheckStorageSpace();
        }

        // Confirm Subject Information //
        // 대상자 및 PA Game의 현재 정보를 지정하는 함수로 여기에서 대상자의 ID, PA Game의 단계, 촬영장소를 입력하여 및 지정하여 데이터 저장하기 위한 폴더를 생성하는 함수
        public int ic_flg = 0; // 대상자 및 PA Game관련 설정이 이루어졌는지 확인하는 정수형 변수(0 : 설정하지 않음 / 1 : 설정함)
        public string storage = ""; // 현재 저장소 표시 문자열 변수
        public string trg_path = ""; // 설정된 PA Game까지의 경로를 담을 문자열 변수
        public string subject = ""; // 대상자 ID값을 담을 문자열 변수
        public string date = ""; // 현재 일자를 담을 문자열 변수
        public string game_info = ""; // PA Game의 선택된 단계를 담을 문자열 변수
        private void ICButton_Click(object sender, RoutedEventArgs e)
        {
            // 대상자 정보를 입력할 수 있게 UI가 활성화 되어있는지 확인하여 UI가 활성화되어 있으면 정보를 입력 및 저장, UI가 비활성화 되어있으면 정보를 입력할 수 있게 UI를 활성화하는 작업을 수행
            if (IDBox.IsEnabled.Equals(true))
            {
                if (SCSwitch.IsOn) // 데이터 저장을 활성화 했을 경우에만 대상자 및 PA Game 관련 정보 기입 수행
                {
                    // 대상자 ID 번호 설정 작업 수행
                    int id_chk = 0; // ID 번호 가 설정되었는지 확인하는 정수형 변수
                    string subject_id = IDBox.Text.PadLeft(4, '0'); // ID 번호를 입력하는 TextBox로부터 입력되어져 있는 문자열 정보 추출 후 4자리의 문자열이 아닐경우에는 왼쪽에서부터 4자리가 되도록 0으로 채운 문자열 변수로 만들어 줌
                    if (subject_id.Equals("----")) // ID 번호의 TextBox의 초기값이 "----"이므로 대상자 ID를 입력하지 않았을 경우에는 ID를 입력하도록 안내창 출력
                    {
                        _ = MessageBox.Show("ID has not been entered.\nPlease Check ID!");
                        id_chk = 0;
                    }
                    else // 대상자 ID 번호를 획득했다면 ID 번호 획득 확인 정수형 변수의 값을 1로 변경하여 상태 표시
                    {
                        id_chk = 1;
                    }

                    // 데이터 수집 장소 추출 작업 수행
                    int plc_idx = LocationBox.SelectedIndex; // 수집 장소 ComboBox에서 선택되어져 있는 값의 Index를 획득
                    string plc = ""; // 수집 장소를 나타낼 문자열을 담을 변수
                    switch (plc_idx)
                    {
                        case 0:
                            plc = "d"; // 한양대 DHC 센터
                            break;
                        case 1:
                            plc = "h"; // 한양대 병원
                            break;
                        case 2:
                            plc = "s"; // 삼성 서울 병원
                            break;
                    }
                    // 데이터 수집 장소와 입력 받은 대상자 ID 번호를 통해서 대상자 ID를 설정
                    subject = plc + subject_id;


                    // 아래의 3줄은 지정한 3개의 PA Game만 수행이 아닌 전체 PA Game의 데이터를 수집할 경우 사용하는 코드로 현재 실험은 3개의 PA Game을 지정하여 사용하기에
                    // 추후 PA Game 선택 관련 UI 변경하여 같이 사용하면 됨
                    //string game_level = GLNUD.Value.ToString();
                    //string game_stage = GSNUD.Value.ToString();
                    //game_info = game_level + game_stage;

                    // PA Game의 Module을 선택하는 작업 수행
                    int game_select_idx = GameBox.SelectedIndex; // 현재 선택되어져 있는 PA Game Module의 ComboBox 값의 Index를 획득
                                                                 // 획득한 Index의 값에 따른 PA Game 모듈의 문자열을 지정
                    switch (game_select_idx)
                    {
                        case 0:
                            game_info = "62";
                            break;
                        case 1:
                            game_info = "63";
                            break;
                        case 2:
                            game_info = "12";
                            break;
                    }

                    if (id_chk.Equals(1))
                    {
                        // 선택되어져 있는 PC 저장소의 문자열 획득
                        storage = SDText.Text;
                        // 현재 데이터 수집일자의 문자열 획득
                        date = DateTime.Now.ToString("yyMMdd", CultureInfo.CurrentUICulture.DateTimeFormat);

                        // Set Basic Path
                        // PC 저장소/AzureKinectData/대상자ID/일자 의 경로 문자열 생성 및 폴더 생성
                        var basic_path = System.IO.Path.Combine(storage, "AzureKinectData", subject, date);
                        DirectoryInfo basic_dir = new DirectoryInfo(basic_path);
                        if (!basic_dir.Exists)
                        {
                            basic_dir.Create();
                        }

                        var game_path = System.IO.Path.Combine(basic_path, game_info); // 기본 경로 + PA Game Module의 폴더 경로 문자열 생성 및 폴더 생성
                        DirectoryInfo game_dir = new DirectoryInfo(game_path);
                        if (!game_dir.Exists)
                        {
                            game_dir.Create();
                            trg_path = game_path;
                            ic_flg = 1;

                            // 정상적으로 대상자의 정보 및 폴더가 정상적으로 설정이 되었다면 이를 UI를 통해 확인을 시켜주고 정보를 더이상 수정할 수 없게 조치하는 비활성화 작업 수행
                            _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                ICButton.Background = new SolidColorBrush(Colors.SkyBlue);
                                LocationBox.IsEnabled = false;
                                IDBox.IsEnabled = false;
                                GameBox.IsEnabled = false;
                            }));
                        }
                        else
                        {
                            _ = MessageBox.Show("해당 대상자의 PA Game Module의 폴더가 이미 생성되어 있습니다.\n\n생성되어져 있는 폴더를 확인 후 폴더를 삭제 또는 정보를 변경 하여\n다시 시도해 주십시오.");
                            ic_flg = 0;
                        }
                    }
                    else
                    {
                        _ = MessageBox.Show("대상자 ID가 정상적으로 설정되지 않았습니다.\n확인 후 다시 시도해 주십시오.");
                        ic_flg = 0;
                    }
                }
                else
                {
                    ic_flg = 1;
                }
                _ = MessageBox.Show("Successfully modify Subject Information.");
            }
            else
            {
                // 대상자 정보를 수정하기 위하여 UI를 활성화 작업 수행
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    ICButton.Background = new SolidColorBrush(Colors.Beige);
                    LocationBox.IsEnabled = true;
                    IDBox.IsEnabled = true;
                    GameBox.IsEnabled = true;
                }));
                ic_flg = 0;
            }
        }

        // Connected Kinect Check //
        // 연결되어진 Azure Kinect의 수를 파악하여 TextBlock에 표시해주는 함수
        private void KCButton_Click(object sender, RoutedEventArgs e)
        {
            int device_cnt = Device.GetInstalledCount();
            KCText.Text = device_cnt.ToString();
        }

        // Save Activate Toggle Switch //
        // Azure Kinect로부터 촬영 시 데이터를 수집할 것인지 수집하지 않을 것인지 미리 선택하는 함수
        // Azure Kinect 설치 후 위치 조정 혹은 정상적으로 촬영이 되어지는지 확인할 때 Deactivate상태로 Toggle 버튼을 지정함
        private void SCSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SCSwitch.IsOn) // 데이터 수집 활성화
                {
                    CISwitch.IsEnabled = true;
                    DISwitch.IsEnabled = true;
                    IISwitch.IsEnabled = true;
                    JDSwitch.IsEnabled = true;
                    CVSwitch.IsEnabled = true;
                }
                else // 데이터 수집 비활성화
                {
                    CISwitch.IsEnabled = false;
                    DISwitch.IsEnabled = false;
                    IISwitch.IsEnabled = false;
                    JDSwitch.IsEnabled = false;
                    CVSwitch.IsEnabled = false;
                }
            }
            catch
            {
                
            }
        }

        // Azure Kinect Power Control //
        // Azure Kinect와 PC 프로그램과 연결하는 함수
        public int sync_mode = 0; // Azure Kinect를 Syncronize하여 사용할지 체크하는 정수형 변수
        public int kp_flg = 0; // Azure Kinect Power가 수행되었는지 확인하는 정수형 변수
        public ArrayList kinect_list = new ArrayList(); // Azure Kinect의 device 객체를 담을 전역 리스트
        private void PWButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int device_cnt = Device.GetInstalledCount();
                ArrayList device_list = new ArrayList(); // Azure Kinect의 device 객체를 담을 임시 리스트
                ArrayList mtk_list = new ArrayList(); // Syncronize Mode가 Master인 Kinect를 담을 리스트
                ArrayList sbk_list = new ArrayList(); // Syncronize Mode가 Subordinate인 Kinect를 담을 리스트
                ArrayList sak_list = new ArrayList(); // Syncronize Mode가 Standalone인 Kinect를 담을 리스트
                // Why? Azure Kinect의 device를 객체를 가져올 때에는 Device의 serial number의 순서대로 가져오기 때문에 Master와 Subordinate의 위치를 구별하기 위한 작업이 필요함
                switch (kp_flg)
                {
                    case 0: // Azure Kinect Power ON
                        // 대상자 정보가 정상적으로 설정이 되어있는지 확인하는 작업 수행
                        int subject_info_chk = 0;
                        if (SCSwitch.IsOn)
                        {
                            subject_info_chk = ic_flg;
                        }
                        else
                        {
                            subject_info_chk = 1;
                        }

                        switch (subject_info_chk)
                        {
                            case 0: // 대상자 정보가 정상적으로 설정이 되지 않았을 경우 안내창 호출
                                _ = MessageBox.Show("대상자 정보가 정상적으로 설정되어있지 않습니다.\n대상자 정보를 확인하여 다시 시도해 주십시오.");
                                break;

                            case 1:
                                try
                                {
                                    // Device List 초기화 후 연결된 Kinect의 수 만큼 Power ON 수행
                                    device_list.Clear();
                                    device_list = AKPower.AKPWON(device_cnt);

                                    // Azure Kinect의 Syncronize 상태에 따른 분류를 위한 임시 리스트 초기화
                                    mtk_list.Clear();
                                    sbk_list.Clear();
                                    sak_list.Clear();

                                    // Azure Kinect 설정 선택 값 호출
                                    int dm_idx = DMBox.SelectedIndex; // Depth Mode
                                    int cf_idx = CFBox.SelectedIndex; // Color Format
                                    int cr_idx = CRBox.SelectedIndex; // Color Resolution
                                    int fr_idx = FRBox.SelectedIndex; // Frame Rate

                                    int tm_idx = TMBox.SelectedIndex; // Tracker Mode
                                    int om_idx = OMBox.SelectedIndex; // onnx Model
                                    int so_idx = SOBox.SelectedIndex; // Sensor Orientation
                                    int gi_idx = (int)GINUD.Value; // GPU Index

                                    // Azure Kinect 정보 추출하여 전역 딕셔너리에 정보 저장
                                    for (int idx = 0; idx < device_cnt; idx++)
                                    {
                                        Device kinect = (Device)device_list[idx]; // 연결되어진 Azure Kinect의 리스트의 원소 호출
                                        string sync_stat = AKPower.AKSync(kinect); // 해당 Azure Kinect의 Syncronize 상태 호출

                                        // Azure Kinect의 Sensor Configuration 객체 정의
                                        DeviceConfiguration sensor_config = AKConfig.SensorConfig(dm_idx, cf_idx, cr_idx, fr_idx);
                                        if (sync_stat.Equals("Master")) // Master 모드일 경우
                                        {
                                            sensor_config.WiredSyncMode = WiredSyncMode.Master;
                                        }
                                        else if (sync_stat.Equals("Subordinate")) // Subordinate 모드일 경우
                                        {
                                            sensor_config.WiredSyncMode = WiredSyncMode.Subordinate;
                                        }
                                        else // Standalone 모드일 경우
                                        {
                                            sensor_config.WiredSyncMode = WiredSyncMode.Standalone;
                                        }

                                        TextBlock sync_text = (TextBlock)synctxt_list[idx]; // 해당 Azure Kinect의 Suncronize 상태 표시를 위한 TextBlock UI 객체 호출
                                        sync_text.Text = sync_stat; // 해당 UI에 Text 쓰기

                                        // Azure Kinect의 Tracker Configuration 객체 정의
                                        TrackerConfiguration tracker_config = AKConfig.TrackerConfig(tm_idx, om_idx, so_idx, gi_idx);
                                        // Azure Kinect의 Calibration 객체 정의
                                        Calibration calibration = kinect.GetCalibration(sensor_config.DepthMode, sensor_config.ColorResolution);
                                        // Azure Kinect의 Tranformation 객체 정의
                                        Transformation transfromation = new Transformation(calibration);
                                        // Azure Kinect의 Tracker 객체 정의
                                        Tracker kinect_tracker = null;
                                        try
                                        {
                                            // Tracker 객체 생성
                                            kinect_tracker = Tracker.Create(calibration, tracker_config);
                                        }
                                        catch (Exception ex) // Tracker 객체 생성 실패 시
                                        {
                                            _ = MessageBox.Show("Fail to Create Tracker Instance!\n\n"+ex.ToString());
                                            kp_flg = 0;
                                            break;
                                        }

                                        // Azure Kinect의 전체적인 정보를 딕셔너리에 저장
                                        Dictionary<string, object> kinect_info = new Dictionary<string, object>();
                                        kinect_info.Add("index", idx);
                                        kinect_info.Add("kinect_device", kinect);
                                        kinect_info.Add("sync_mode", sync_mode);
                                        kinect_info.Add("sensor_config", sensor_config);
                                        kinect_info.Add("tracker_config", tracker_config);
                                        kinect_info.Add("calibration", calibration);
                                        kinect_info.Add("transformation", transfromation);
                                        kinect_info.Add("kinect_tracker", kinect_tracker);

                                        // Azure Kinect의 Syncronize 상태에 따른 전체적인 정보를 임시 리스트에 저장
                                        if (sync_mode.Equals("Master"))
                                        {
                                            mtk_list.Add(kinect_info);
                                        }
                                        else if (sync_mode.Equals("Subordinate"))
                                        {
                                            sbk_list.Add(kinect_info);
                                        }
                                        else
                                        {
                                            sak_list.Add(kinect_info);
                                        }
                                    }

                                    // Captrue 시작 시 우선 순위를 Subordinate > Standalone > Master로 구성하기 위한 로직
                                    if (sbk_list.Count > 0)
                                    {
                                        kinect_list.Add(sbk_list[0]);
                                        sync_mode = 1;
                                    }

                                    if (sak_list.Count > 0)
                                    {
                                        for (int sa_idx = 0; sa_idx < sak_list.Count; sa_idx++)
                                        {
                                            kinect_list.Add(sak_list[sa_idx]);
                                        }
                                    }

                                    if (mtk_list.Count > 0)
                                    {
                                        kinect_list.Add(mtk_list[0]);
                                    }

                                    // 정상적으로 함수가 동작했을 시 UI에 표시하기 위한 로직
                                    _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                    {
                                        PWButton.Background = new SolidColorBrush(Colors.SkyBlue);
                                        RCButton.IsEnabled = true;
                                    }));
                                    kp_flg = 1;
                                }
                                catch
                                {
                                    _ = MessageBox.Show("Fail to Open Azure Kinect!\nAzure Kinect의 연결 상태를 확인 후 다시 시도해 주십시오.");
                                    kp_flg = 0;
                                }
                                break;
                        }
                        break;

                    case 1: // Azure Kinect Power OFF
                        // 연결된 Kinect의 수 만큼 Power OFF 수행
                        for (int idx = 0; idx < device_cnt; idx++)
                        {
                            Device kinect = (Device)device_list[idx]; // device 객체의 리스트에서 객체 추출
                            AKPower.AKPWOFF(kinect); // OFF 작업 수행

                            // UI에 관련 정보 쵸시
                            TextBlock sync_text = (TextBlock)synctxt_list[idx];
                            sync_text.Text = "Offline";
                        }

                        // UI에 관련 정보 표시
                        _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            PWButton.Background = new SolidColorBrush(Colors.LightCoral);
                            RCButton.IsEnabled = false;

                            ICButton.Background = new SolidColorBrush(Colors.Beige);
                            LocationBox.IsEnabled = true;
                            IDBox.IsEnabled = true;
                            GameBox.IsEnabled = true;
                        }));

                        kinect_list.Clear();
                        sync_mode = 0;
                        kp_flg = 0;
                        ic_flg = 0;
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.ToString());
            }
            
        }

        // Azure Kinect Record Control //
        // Azure kinect에서 Capture를 시작하여 데이터를 획득하는 함수
        public int kr_flg = 0;
        public bool record_chk = false;
        private void RCButton_Click(object sender, RoutedEventArgs e)
        {
            CheckStorageSpace();

            switch (kr_flg)
            {
                // Azure Kinect Record Start
                case 0:
                    // Data 저장 활성화 여부에 따른 변수 값 설정 조건문
                    if (SCSwitch.IsOn)
                    {
                        // Data 저장이 활성화 되어 있을 때에는 대상자 정보가 정확히 입력이 되어있는지 확인하여 전달
                        ic_flg = ic_flg;
                    }
                    else
                    {
                        // Data 저장이 비활성화 상태이면 1로 고정값을 주어 Capture를 시작하게 변수 전달
                        ic_flg = 1;
                    }

                    switch (ic_flg)
                    {
                        case 0: // 대상자 정보가 정상적으로 확인되어있지 않았을 경우 메세지 출력
                            _ = MessageBox.Show("Subject information not set.\nPlease Setting Subject Information!");
                            break;

                        case 1:
                            // 대상자 정보 확인 후 Data 수집 전 Azure Kinect의 위치가 중복되어 있는지 확인
                            if (SCSwitch.IsOn && kinect_list.Count > 1 && KLocationBox_1.SelectedIndex == KLocationBox_2.SelectedIndex)
                            {
                                _ = MessageBox.Show("Azure Kinect Location is Duplicated!\nPlease set Azure Kinect Location!");
                            }
                            else
                            {
                                kr_flg = 1; // Kinect Record 상태 확인 변수
                                record_chk = true;

                                // Azure Kinect의 Capture Loop 함수 비동기로 호출
                                Task kinect_record_start = Task.Run(() => KinectRecordStart());

                                // UI 관련 Control 부분(촬영 표시 버튼 색상 및 Power 버튼 비활성화, Record 버튼 색상 Control)
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (init_device_cnt.Equals(1))
                                        {
                                            RecordStat_1.Background = new SolidColorBrush(Colors.Crimson);
                                        }
                                        else if (init_device_cnt.Equals(2))
                                        {
                                            RecordStat_1.Background = new SolidColorBrush(Colors.Crimson);
                                            RecordStat_2.Background = new SolidColorBrush(Colors.Crimson);
                                        }
                                    }

                                    RCButton.Background = new SolidColorBrush(Colors.SkyBlue);
                                    PWButton.IsEnabled = false;
                                }));
                            }
                            break;
                    }
                    break;
                // Azure Kinect Record Stop
                case 1:
                    record_chk = false;
                    // Azure Kinect 촬영 종료 함수 호출
                    //Task kinect_record_stop = Task.Run(() => KinectRecordStop());
                    KinectRecordStop();

                    // 관련 UI Control
                    _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        if (init_device_cnt.Equals(1))
                        {
                            RecordStat_1.Background = new SolidColorBrush(Colors.Gray);
                        }
                        else if (init_device_cnt.Equals(2))
                        {
                            RecordStat_1.Background = new SolidColorBrush(Colors.Gray);
                            RecordStat_2.Background = new SolidColorBrush(Colors.Gray);
                        }
                        
                        RCButton.Background = new SolidColorBrush(Colors.Beige);
                        PWButton.IsEnabled = true;

                        KImage_1.Visibility = Visibility.Hidden;
                        KImage_2.Visibility = Visibility.Hidden;

                        ICButton.Background = new SolidColorBrush(Colors.Beige);
                        LocationBox.IsEnabled = true;
                        IDBox.IsEnabled = true;
                        GameBox.IsEnabled = true;
                    }));

                    ic_flg = 0;
                    kr_flg = 0;

                    // COCO Data Create Task
                    Task coco_create = Task.Run(() => CocoCreater.CocoSet(storage, subject, date, game_info, capture_time_dict));

                    break;
            }
        }

        // Azure Kinect Record Start Function //
        // Azure Kinect의 데이터 수집을 위한 Loop 부분 작성 함수
        public Stopwatch stop_watch = new Stopwatch(); // 촬영 시간을 표시하기 위한 Stopwatch 객체
        public int last_frame_num = 0;
        public Dictionary<int, string> capture_time_dict = new Dictionary<int, string>();
        public void KinectRecordStart()
        {
            int k_cnt = kinect_list.Count;
            capture_time_dict = new Dictionary<int, string>();
            // Azure Kinect 2대 이상 및 Syncronize Mode 사용 시
            if (sync_mode > 0 && k_cnt > 1)
            {
                Dictionary<string, object> sbk_info = (Dictionary<string, object>)kinect_list[0]; // Subordinate Azure Kinect 정보를 딕셔너리에서 호출
                int sb_idx = (int)sbk_info["index"]; // Subordinate Azure Kinect가 연결되어 있는 인덱스 정보 추출
                Device sb_kinect = (Device)sbk_info["kinect_device"]; // Subordinate Azure Kinect의 device 객체 추출
                DeviceConfiguration sbs_config = (DeviceConfiguration)sbk_info["sensor_config"]; // Subordinate Azure Kinect의 설정 정보 객체 추출
                Tracker sb_tracker = (Tracker)sbk_info["kinect_tracker"]; // Subordinate Azure Kinect의Tracker 객체 추출
                Calibration sb_calibration = (Calibration)sbk_info["calibration"]; // Subordinate Azure Kinect의 Calibration 객체 추출
                Transformation sb_transformation = (Transformation)sbk_info["transformation"]; // Subordinate Azure Kinect의 Transformation 객체 추출

                // Subordinate Azure Kinect 위치 반환
                ComboBox sb_loc_combox = (ComboBox)loctxt_list[sb_idx];
                int sb_loc_idx = 0;
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    sb_loc_idx = sb_loc_combox.SelectedIndex;
                }));
                string sb_kinect_loc = "";
                int sb_camera_id = 0;
                switch (sb_loc_idx)
                {
                    case 0:
                        sb_kinect_loc = "kf";
                        sb_camera_id = 0;
                        break;
                    case 1:
                        sb_kinect_loc = "kl";
                        sb_camera_id = 1;
                        break;
                    case 2:
                        sb_kinect_loc = "kr";
                        sb_camera_id = 2;
                        break;
                }

                Dictionary<string, object> mtk_info = (Dictionary<string, object>)kinect_list[1]; // Master Azure Kinect 정보를 딕셔너리에서 호출
                int mt_idx = (int)mtk_info["index"]; // Master Azure Kinect가 연결되어 있는 인덱스 정보 추출
                Device mt_kinect = (Device)mtk_info["kinect_device"]; // Master Azure Kinect의 device 객체 추출
                DeviceConfiguration mts_config = (DeviceConfiguration)mtk_info["sensor_config"]; // Master Azure Kinect의 설정 정보 객체 추출
                Tracker mt_tracker = (Tracker)mtk_info["kinect_tracker"]; // Master Azure Kinect의Tracker 객체 추출
                Calibration mt_calibration = (Calibration)mtk_info["calibration"]; // Master Azure Kinect의 Calibration 객체 추출
                Transformation mt_transformation = (Transformation)mtk_info["transformation"]; // Master Azure Kinect의 Transformation 객체 추출

                // Master Azure Kinect 위치 반환
                ComboBox mt_loc_combox = (ComboBox)loctxt_list[mt_idx];
                int mt_loc_idx = 0;
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    mt_loc_idx = mt_loc_combox.SelectedIndex;
                }));
                string mt_kinect_loc = "";
                int mt_camera_id = 0;
                switch (mt_loc_idx)
                {
                    case 0:
                        mt_kinect_loc = "kf";
                        mt_camera_id = 0;
                        break;
                    case 1:
                        mt_kinect_loc = "kl";
                        mt_camera_id = 1;
                        break;
                    case 2:
                        mt_kinect_loc = "kr";
                        mt_camera_id = 2;
                        break;
                }

                // Date 저장 경로 변수 초기화
                var sb_ci_path = "";
                var mt_ci_path = "";
                var sb_di_path = "";
                var mt_di_path = "";
                var sb_ti_path = "";
                var mt_ti_path = "";
                var sb_ii_path = "";
                var mt_ii_path = "";
                var sb_ji_path = "";
                var mt_ji_path = "";
                var cv_path = "";
                var an_path = "";
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    if (SCSwitch.IsOn) // Data 저장 활성화 되어있을 경우 각 경로 변수 생성 후 폴더 생성
                    {
                        // Save Calibration Information
                        var cd_path = System.IO.Path.Combine(trg_path, "0_calibration");
                        DirectoryInfo cal_dir = new DirectoryInfo(cd_path);

                        if (!cal_dir.Exists)
                        {
                            cal_dir.Create();

                            object[] sb_cal_name_arr = { subject, date, sb_kinect_loc, game_info, "calibration" };
                            object[] mt_cal_name_arr = { subject, date, mt_kinect_loc, game_info, "calibration" };
                            string sb_cal_name = string.Join("_", sb_cal_name_arr);
                            string mt_cal_name = string.Join("_", mt_cal_name_arr);

                            object[] sb_cal_file_arr = { sb_cal_name, "json" };
                            object[] mt_cal_file_arr = { mt_cal_name, "json" };
                            string sb_cal_file = string.Join(".", sb_cal_file_arr);
                            string mt_cal_file = string.Join(".", mt_cal_file_arr);

                            var sb_cal_path = System.IO.Path.Combine(cd_path, sb_cal_file);
                            var mt_cal_path = System.IO.Path.Combine(cd_path, mt_cal_file);
                            string sb_serial_num = sb_kinect.SerialNum;
                            string mt_serial_num = mt_kinect.SerialNum;

                            string sbk_calibration = AKCalibration.AKCalibrations(sb_camera_id, sb_kinect_loc, sb_serial_num, sb_calibration);
                            string mtk_calibration = AKCalibration.AKCalibrations(mt_camera_id, mt_kinect_loc, mt_serial_num, mt_calibration);
                            AKDataSave.AKCalibration(sbk_calibration, sb_cal_path);
                            AKDataSave.AKCalibration(mtk_calibration, mt_cal_path);
                        }

                        // Set Color Path
                        if (CISwitch.IsOn)
                        {
                            sb_ci_path = System.IO.Path.Combine(trg_path, "1_color", sb_kinect_loc);
                            DirectoryInfo sb_ci_dir = new DirectoryInfo(sb_ci_path);
                            if (!sb_ci_dir.Exists)
                            {
                                sb_ci_dir.Create();
                            }
                            mt_ci_path = System.IO.Path.Combine(trg_path, "1_color", mt_kinect_loc);
                            DirectoryInfo mt_ci_dir = new DirectoryInfo(mt_ci_path);
                            if (!mt_ci_dir.Exists)
                            {
                                mt_ci_dir.Create();
                            }
                        }

                        // Set Depth Path
                        if (DISwitch.IsOn)
                        {
                            sb_di_path = System.IO.Path.Combine(trg_path, "2_depth", sb_kinect_loc);
                            DirectoryInfo sb_di_dir = new DirectoryInfo(sb_di_path);
                            if (!sb_di_dir.Exists)
                            {
                                sb_di_dir.Create();
                            }
                            mt_di_path = System.IO.Path.Combine(trg_path, "2_depth", mt_kinect_loc);
                            DirectoryInfo mt_di_dir = new DirectoryInfo(mt_di_path);
                            if (!mt_di_dir.Exists)
                            {
                                mt_di_dir.Create();
                            }

                            sb_ti_path = System.IO.Path.Combine(trg_path, "3_trdepth", sb_kinect_loc);
                            DirectoryInfo sb_ti_dir = new DirectoryInfo(sb_ti_path);
                            if (!sb_ti_dir.Exists)
                            {
                                sb_ti_dir.Create();
                            }
                            mt_ti_path = System.IO.Path.Combine(trg_path, "3_trdepth", mt_kinect_loc);
                            DirectoryInfo mt_ti_dir = new DirectoryInfo(mt_ti_path);
                            if (!mt_ti_dir.Exists)
                            {
                                mt_ti_dir.Create();
                            }
                        }

                        // Set IR Path
                        if (IISwitch.IsOn)
                        {
                            sb_ii_path = System.IO.Path.Combine(trg_path, "4_ir", sb_kinect_loc);
                            DirectoryInfo sb_ii_dir = new DirectoryInfo(sb_ii_path);
                            if (!sb_ii_dir.Exists)
                            {
                                sb_ii_dir.Create();
                            }
                            mt_ii_path = System.IO.Path.Combine(trg_path, "4_ir", mt_kinect_loc);
                            DirectoryInfo mt_ii_dir = new DirectoryInfo(mt_ii_path);
                            if (!mt_ii_dir.Exists)
                            {
                                mt_ii_dir.Create();
                            }
                        }

                        // Set Joint Path
                        if (JDSwitch.IsOn)
                        {
                            sb_ji_path = System.IO.Path.Combine(trg_path, "5_joint", sb_kinect_loc);
                            DirectoryInfo sb_ji_dir = new DirectoryInfo(sb_ji_path);
                            if (!sb_ji_dir.Exists)
                            {
                                sb_ji_dir.Create();
                            }
                            mt_ji_path = System.IO.Path.Combine(trg_path, "5_joint", mt_kinect_loc);
                            DirectoryInfo mt_ji_dir = new DirectoryInfo(mt_ji_path);
                            if (!mt_ji_dir.Exists)
                            {
                                mt_ji_dir.Create();
                            }
                        }

                        // Set Video Path
                        if (CVSwitch.IsOn)
                        {
                            cv_path = System.IO.Path.Combine(trg_path, "6_video");
                            DirectoryInfo cv_dir = new DirectoryInfo(cv_path);
                            if (!cv_dir.Exists)
                            {
                                cv_dir.Create();
                            }
                        }

                        // Set Annotation Path
                        an_path = System.IO.Path.Combine(trg_path, "7_annotation");
                        DirectoryInfo an_dir = new DirectoryInfo(an_path);
                        if (!an_dir.Exists)
                        {
                            an_dir.Create();
                        }
                    }
                }));

                // Subordinate Azure Kinect 우선 Capture 시작하여 대기상태 유지
                sb_kinect.StartCameras(sbs_config);
                Thread.Sleep(300);

                // Mater Azure Kinect의 Capture를 시작하면서 Subordinate의 대기상태를 활성상태로 전환하여 Mater와 같은 타이밍에 Capture 수행
                mt_kinect.StartCameras(mts_config);

                stop_watch.Start(); // Stopwatch 시작
                int frame_cnt = 0;
                // 무한 루프를 이용하여 Capture 정보 추출
                while (record_chk)
                {
                    // 촬영시간을 UI에 표현하기 위한 부분
                    TimeSpan time_span = stop_watch.Elapsed;
                    string elapsed_time = string.Format("{0:00}:{1:00}.{2:00}",
                        time_span.Minutes, time_span.Seconds, time_span.Milliseconds / 10);
                    string date_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentUICulture.DateTimeFormat);
                    capture_time_dict.Add(frame_cnt, date_time);

                    // Capture 객체 초기화
                    Microsoft.Azure.Kinect.Sensor.Capture mt_capture = null;
                    Microsoft.Azure.Kinect.Sensor.Capture sb_capture = null;
                    // 병렬로 함수를 동작시켜서 Mater와 Subordinate의 Capture 정보를 동시에 추출
                    Parallel.Invoke(
                        () =>
                        {
                            mt_capture = mt_kinect.GetCapture();
                            _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                TimeText.Text = elapsed_time;
                                if (SCSwitch.IsOn)
                                {
                                    if (JDSwitch.IsOn)
                                    {
                                        mt_tracker.EnqueueCapture(mt_capture);
                                    }
                                }
                            }));
                        },
                        () =>
                        {
                            Thread.Sleep(2);
                            sb_capture = sb_kinect.GetCapture();
                            _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                if (SCSwitch.IsOn)
                                {
                                    if (JDSwitch.IsOn)
                                    {
                                        sb_tracker.EnqueueCapture(sb_capture);
                                    }
                                }
                            }));
                        }
                    );
                    // 정상적으로 Mater와 Subordinate의 Capture가 수행 되었을 경우의 조건문으로 UI에 이미지 표현 및 저장 동작 함수 호출
                    if (mt_capture != null && sb_capture != null)
                    {
                        string capture_time = DateTime.Now.ToString("yyMMdd_HHmmss_ffff", CultureInfo.CurrentUICulture.DateTimeFormat);
                        Parallel.Invoke(
                            () => {
                                // Subordinate의 Color Image 처리 구문
                                Microsoft.Azure.Kinect.Sensor.Image sb_cimg = sb_capture.Color;
                                WriteableBitmap sb_color_wbitmap = AKImageConvert.ColorConvert(sb_cimg);
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    System.Windows.Controls.Image sbk_image = (System.Windows.Controls.Image)img_list[sb_idx];
                                    sbk_image.Source = sb_color_wbitmap;
                                    sbk_image.Visibility = Visibility.Visible;
                                    if (SCSwitch.IsOn)
                                    {
                                        if (CISwitch.IsOn)
                                        {
                                            object[] sb_ci_name_arr = { subject, date, sb_kinect_loc, game_info, "color", frame_cnt.ToString().PadLeft(9, '0') };
                                            string sb_ci_name = string.Join("_", sb_ci_name_arr);
                                            object[] sb_ci_file_arr = { sb_ci_name, "jpg" };
                                            string sb_ci_file = string.Join(".", sb_ci_file_arr);

                                            var sb_cimg_path = Path.Combine(sb_ci_path, sb_ci_file);

                                            Task sb_save_color = Task.Run(() => AKDataSave.AKColorImage(sb_cimg_path, sb_color_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                // Master의 Color Image 처리 구문
                                Microsoft.Azure.Kinect.Sensor.Image mt_cimg = mt_capture.Color;
                                WriteableBitmap mt_color_wbitmap = AKImageConvert.ColorConvert(mt_cimg);
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    System.Windows.Controls.Image mtk_image = (System.Windows.Controls.Image)img_list[mt_idx];
                                    mtk_image.Source = mt_color_wbitmap;
                                    mtk_image.Visibility = Visibility.Visible;
                                    if (SCSwitch.IsOn)
                                    {
                                        if (CISwitch.IsOn)
                                        {
                                            object[] mt_ci_name_arr = { subject, date, mt_kinect_loc, game_info, "color", frame_cnt.ToString().PadLeft(9, '0') };
                                            string mt_ci_name = string.Join("_", mt_ci_name_arr);
                                            object[] mt_ci_file_arr = { mt_ci_name, "jpg" };
                                            string mt_ci_file = string.Join(".", mt_ci_file_arr);

                                            var mt_cimg_path = System.IO.Path.Combine(mt_ci_path, mt_ci_file);

                                            Task mt_save_color = Task.Run(() => AKDataSave.AKColorImage(mt_cimg_path, mt_color_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    // Subordinate의 Depth Image 처리 구문
                                    if (SCSwitch.IsOn)
                                    {
                                        if (DISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image sb_dimg = sb_capture.Depth;
                                            WriteableBitmap sb_depth_wbitmap = AKImageConvert.DepthConvert(sb_dimg);
                                            WriteableBitmap sb_tr_depth_wbitmap = AKImageConvert.TrDepthConvert(sb_transformation, sb_dimg);

                                            object[] sb_di_name_arr = { subject, date, sb_kinect_loc, game_info, "depth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string sb_di_name = string.Join("_", sb_di_name_arr);
                                            object[] sb_di_file_arr = { sb_di_name, "png" };
                                            string sb_di_file = string.Join(".", sb_di_file_arr);

                                            var sb_dimg_path = System.IO.Path.Combine(sb_di_path, sb_di_file);

                                            object[] sb_trdi_name_arr = { subject, date, sb_kinect_loc, game_info, "trdepth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string sb_trdi_name = string.Join("_", sb_trdi_name_arr);
                                            object[] sb_trdi_file_arr = { sb_trdi_name, "png" };
                                            string sb_trdi_file = string.Join(".", sb_trdi_file_arr);

                                            var sb_trdimg_path = System.IO.Path.Combine(sb_ti_path, sb_trdi_file);

                                            Task sb_save_depth = Task.Run(() => AKDataSave.AKPNGImage(sb_dimg_path, sb_depth_wbitmap));
                                            Task sb_save_trdepth = Task.Run(() => AKDataSave.AKPNGImage(sb_trdimg_path, sb_tr_depth_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    // Master의 Depth Image 처리 구문
                                    if (SCSwitch.IsOn)
                                    {
                                        if (DISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image mt_dimg = mt_capture.Depth;
                                            WriteableBitmap mt_depth_wbitmap = AKImageConvert.DepthConvert(mt_dimg);
                                            WriteableBitmap mt_tr_depth_wbitmap = AKImageConvert.TrDepthConvert(mt_transformation, mt_dimg);

                                            object[] mt_di_name_arr = { subject, date, mt_kinect_loc, game_info, "depth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string mt_di_name = string.Join("_", mt_di_name_arr);
                                            object[] mt_di_file_arr = { mt_di_name, "png" };
                                            string mt_di_file = string.Join(".", mt_di_file_arr);

                                            var mt_dimg_path = System.IO.Path.Combine(mt_di_path, mt_di_file);

                                            object[] mt_trdi_name_arr = { subject, date, mt_kinect_loc, game_info, "trdepth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string mt_trdi_name = string.Join("_", mt_trdi_name_arr);
                                            object[] mt_trdi_file_arr = { mt_trdi_name, "png" };
                                            string mt_trdi_file = string.Join(".", mt_trdi_file_arr);

                                            var mt_trdimg_path = System.IO.Path.Combine(mt_ti_path, mt_trdi_file);

                                            Task mt_save_depth = Task.Run(() => AKDataSave.AKPNGImage(mt_dimg_path, mt_depth_wbitmap));
                                            Task mt_save_trdepth = Task.Run(() => AKDataSave.AKPNGImage(mt_trdimg_path, mt_tr_depth_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    // Subordinate의 IR Image 처리 구문
                                    if (SCSwitch.IsOn)
                                    {
                                        if (IISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image sb_iimg = sb_capture.IR;
                                            WriteableBitmap sb_ir_wbitmap = AKImageConvert.IRConvert(sb_iimg);

                                            object[] sb_ii_name_arr = { subject, date, sb_kinect_loc, game_info, "ir", frame_cnt.ToString().PadLeft(9, '0') };
                                            string sb_ii_name = string.Join("_", sb_ii_name_arr);
                                            object[] sb_ii_file_arr = { sb_ii_name, "png" };
                                            string sb_ii_file = string.Join(".", sb_ii_file_arr);

                                            var sb_iimg_path = System.IO.Path.Combine(sb_ii_path, sb_ii_file);
                                            Task sb_save_ir = Task.Run(() => AKDataSave.AKPNGImage(sb_iimg_path, sb_ir_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    // Master의 IR Image 처리 구문
                                    if (SCSwitch.IsOn)
                                    {
                                        if (IISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image mt_iimg = mt_capture.IR;
                                            WriteableBitmap mt_ir_wbitmap = AKImageConvert.IRConvert(mt_iimg);

                                            object[] mt_ii_name_arr = { subject, date, mt_kinect_loc, game_info, "ir", frame_cnt.ToString().PadLeft(9, '0') };
                                            string mt_ii_name = string.Join("_", mt_ii_name_arr);
                                            object[] mt_ii_file_arr = { mt_ii_name, "png" };
                                            string mt_ii_file = string.Join(".", mt_ii_file_arr);

                                            var mt_iimg_path = System.IO.Path.Combine(mt_ii_path, mt_ii_file);
                                            Task mt_save_ir = Task.Run(() => AKDataSave.AKPNGImage(mt_iimg_path, mt_ir_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () =>
                            {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    // Subordinate의 joint data 처리 구문
                                    if (SCSwitch.IsOn)
                                    {
                                        if (JDSwitch.IsOn)
                                        {
                                            string sb_annotation_json = Task.Run(() => AKTracker.PopTracker(sb_camera_id, frame_cnt, date_time, sb_tracker, sb_calibration)).Result;

                                            object[] jd_name_arr = { subject, date, sb_kinect_loc, game_info, "joint", frame_cnt.ToString().PadLeft(9, '0') };
                                            string jd_name = string.Join("_", jd_name_arr);
                                            object[] jd_file_arr = { jd_name, "json" };
                                            string jd_file = string.Join(".", jd_file_arr);

                                            var jd_path = System.IO.Path.Combine(sb_ji_path, jd_file);
                                            Task save_jd = Task.Run(() => AKDataSave.AKJointData(sb_annotation_json, jd_path));
                                        }
                                    }
                                }));
                            },
                            () =>
                            {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    // Master의 joint data 처리 구문
                                    if (SCSwitch.IsOn)
                                    {
                                        if (JDSwitch.IsOn)
                                        {
                                            string mt_annotation_json = Task.Run(() => AKTracker.PopTracker(mt_camera_id, frame_cnt, date_time, mt_tracker, mt_calibration)).Result;

                                            object[] jd_name_arr = { subject, date, mt_kinect_loc, game_info, "joint", frame_cnt.ToString().PadLeft(9, '0') };
                                            string jd_name = string.Join("_", jd_name_arr);
                                            object[] jd_file_arr = { jd_name, "json" };
                                            string jd_file = string.Join(".", jd_file_arr);

                                            var jd_path = System.IO.Path.Combine(mt_ji_path, jd_file);
                                            Task save_jd = Task.Run(() => AKDataSave.AKJointData(mt_annotation_json, jd_path));
                                        }
                                    }
                                }));
                            }
                        );
                    }

                    frame_cnt += 1;
                    last_frame_num = frame_cnt;

                    sb_capture.Dispose();
                    mt_capture.Dispose();
                }
            }
            // Azure Kinect 1대만 사용 시
            else if (k_cnt == 1)
            {
                int frame_cnt = 0;

                Dictionary<string, object> k_info = (Dictionary<string, object>)kinect_list[0];
                int idx = (int)k_info["index"];
                Device kinect = (Device)k_info["kinect_device"];
                DeviceConfiguration config = (DeviceConfiguration)k_info["sensor_config"];
                Tracker tracker = (Tracker)k_info["kinect_tracker"];

                Calibration calibration = (Calibration)k_info["calibration"];
                Transformation transformation = (Transformation)k_info["transformation"];

                string kinect_loc = "";
                var ci_path = "";
                var di_path = "";
                var ti_path = "";
                var ii_path = "";
                var ji_path = "";
                var cv_path = "";
                var an_path = "";
                int cameras_id = 0;
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    if (SCSwitch.IsOn)
                    {
                        ComboBox loc_combox = (ComboBox)loctxt_list[idx];
                        int loc_idx = loc_combox.SelectedIndex;
                        
                        switch (loc_idx)
                        {
                            case 0:
                                kinect_loc = "kf";
                                cameras_id = 0;
                                break;
                            case 1:
                                kinect_loc = "kl";
                                cameras_id = 1;
                                break;
                            case 2:
                                kinect_loc = "kr";
                                cameras_id = 2;
                                break;
                        }

                        // Save Calibration Information
                        var cd_path = System.IO.Path.Combine(trg_path, "0_calibration");
                        DirectoryInfo cal_dir = new DirectoryInfo(cd_path);

                        if (!cal_dir.Exists)
                        {
                            cal_dir.Create();
                        }
                        object[] cal_name_arr = { subject, date, kinect_loc, game_info, "calibration" };
                        string cal_name = string.Join("_", cal_name_arr);

                        object[] cal_file_arr = { cal_name, "json" };
                        string cal_file = string.Join(".", cal_file_arr);

                        var cal_path = System.IO.Path.Combine(cd_path, cal_file);
                        string serial_num = kinect.SerialNum;

                        string k_calibration = AKCalibration.AKCalibrations(cameras_id, kinect_loc, serial_num, calibration);
                        AKDataSave.AKCalibration(k_calibration, cal_path);

                        // Set Color Path
                        if (CISwitch.IsOn)
                        {
                            ci_path = System.IO.Path.Combine(trg_path, "1_color", kinect_loc);
                            DirectoryInfo ci_dir = new DirectoryInfo(ci_path);
                            if (!ci_dir.Exists)
                            {
                                ci_dir.Create();
                            }
                        }

                        // Set Depth Path
                        if (DISwitch.IsOn)
                        {
                            di_path = System.IO.Path.Combine(trg_path, "2_depth", kinect_loc);
                            DirectoryInfo di_dir = new DirectoryInfo(di_path);
                            if (!di_dir.Exists)
                            {
                                di_dir.Create();
                            }

                            ti_path = System.IO.Path.Combine(trg_path, "3_trdepth", kinect_loc);
                            DirectoryInfo ti_dir = new DirectoryInfo(ti_path);
                            if (!ti_dir.Exists)
                            {
                                ti_dir.Create();
                            }
                        }

                        // Set IR Path
                        if (IISwitch.IsOn)
                        {
                            ii_path = System.IO.Path.Combine(trg_path, "4_ir", kinect_loc);
                            DirectoryInfo ii_dir = new DirectoryInfo(ii_path);
                            if (!ii_dir.Exists)
                            {
                                ii_dir.Create();
                            }
                        }

                        // Set Joint Path
                        if (JDSwitch.IsOn)
                        {
                            ji_path = System.IO.Path.Combine(trg_path, "5_joint", kinect_loc);
                            DirectoryInfo ji_dir = new DirectoryInfo(ji_path);
                            if (!ji_dir.Exists)
                            {
                                ji_dir.Create();
                            }
                        }

                        // Set Video Path
                        if (CVSwitch.IsOn)
                        {
                            cv_path = System.IO.Path.Combine(trg_path, "6_video");
                            DirectoryInfo cv_dir = new DirectoryInfo(cv_path);
                            if (!cv_dir.Exists)
                            {
                                cv_dir.Create();
                            }
                        }
                        
                        // Set Annotation Path
                        an_path = System.IO.Path.Combine(trg_path, "7_annotation");
                        DirectoryInfo an_dir = new DirectoryInfo(an_path);
                        if (!an_dir.Exists)
                        {
                            an_dir.Create();
                        }
                    }
                }));

                System.Windows.Controls.Image k_image = (System.Windows.Controls.Image)img_list[idx];

                kinect.StartCameras(config);
                stop_watch.Start();

                while (record_chk)
                {
                    TimeSpan time_span = stop_watch.Elapsed;
                    string elapsed_time = string.Format("{0:00}:{1:00}.{2:00}",
                        time_span.Minutes, time_span.Seconds, time_span.Milliseconds / 10);

                    string date_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentUICulture.DateTimeFormat);
                    capture_time_dict.Add(frame_cnt, date_time);

                    Microsoft.Azure.Kinect.Sensor.Capture capture = kinect.GetCapture();
                    _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        TimeText.Text = elapsed_time;
                        if (SCSwitch.IsOn)
                        {
                            if (JDSwitch.IsOn)
                            {
                                tracker.EnqueueCapture(capture);
                            }
                        }
                    }));

                    if (capture != null)
                    {
                        Parallel.Invoke(
                            () => {
                                Microsoft.Azure.Kinect.Sensor.Image cimg = capture.Color;
                                WriteableBitmap color_wbitmap = AKImageConvert.ColorConvert(cimg);
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    k_image.Source = color_wbitmap;
                                    k_image.Visibility = Visibility.Visible;

                                    if (SCSwitch.IsOn)
                                    {
                                        if (CISwitch.IsOn)
                                        {
                                            object[] ci_name_arr = { subject, date, kinect_loc, game_info, "color", frame_cnt.ToString().PadLeft(9, '0') };
                                            string ci_name = string.Join("_", ci_name_arr);
                                            object[] ci_file_arr = { ci_name, "jpg" };
                                            string ci_file = string.Join(".", ci_file_arr);

                                            var cimg_path = System.IO.Path.Combine(ci_path, ci_file);

                                            Task save_color = Task.Run(() => AKDataSave.AKColorImage(cimg_path, color_wbitmap));
                                        }
                                    }
                                }));

                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (DISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image dimg = capture.Depth;
                                            WriteableBitmap depth_wbitmap = AKImageConvert.DepthConvert(dimg);
                                            WriteableBitmap tr_depth_wbitmap = AKImageConvert.TrDepthConvert(transformation, dimg);

                                            object[] di_name_arr = { subject, date, kinect_loc, game_info, "depth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string di_name = string.Join("_", di_name_arr);
                                            object[] di_file_arr = { di_name, "png" };
                                            string di_file = string.Join(".", di_file_arr);

                                            var dimg_path = System.IO.Path.Combine(di_path, di_file);

                                            object[] trdi_name_arr = { subject, date, kinect_loc, game_info, "trdepth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string trdi_name = string.Join("_", trdi_name_arr);
                                            object[] trdi_file_arr = { trdi_name, "png" };
                                            string trdi_file = string.Join(".", trdi_file_arr);

                                            var trdimg_path = System.IO.Path.Combine(ti_path, trdi_file);

                                            Task save_depth = Task.Run(() => AKDataSave.AKPNGImage(dimg_path, depth_wbitmap));
                                            Task save_trdepth = Task.Run(() => AKDataSave.AKPNGImage(trdimg_path, tr_depth_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (IISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image iimg = capture.IR;
                                            WriteableBitmap ir_wbitmap = AKImageConvert.IRConvert(iimg);

                                            object[] ii_name_arr = { subject, date, kinect_loc, game_info, "ir", frame_cnt.ToString().PadLeft(9, '0') };
                                            string ii_name = string.Join("_", ii_name_arr);
                                            object[] ii_file_arr = { ii_name, "png" };
                                            string ii_file = string.Join(".", ii_file_arr);

                                            var iimg_path = System.IO.Path.Combine(ii_path, ii_file);
                                            Task save_ir = Task.Run(() => AKDataSave.AKPNGImage(iimg_path, ir_wbitmap));
                                        }
                                    }
                                }));

                            },
                            () =>
                            {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (JDSwitch.IsOn)
                                        {
                                            string annotation_json = Task.Run(() => AKTracker.PopTracker(cameras_id, frame_cnt, date_time, tracker, calibration)).Result;

                                            object[] jd_name_arr = { subject, date, kinect_loc, game_info, "joint", frame_cnt.ToString().PadLeft(9, '0') };
                                            string jd_name = string.Join("_", jd_name_arr);
                                            object[] jd_file_arr = { jd_name, "json" };
                                            string jd_file = string.Join(".", jd_file_arr);

                                            var jd_path = System.IO.Path.Combine(ji_path, jd_file);
                                            Task save_jd = Task.Run(() => AKDataSave.AKJointData(annotation_json, jd_path));
                                        }
                                    }
                                }));
                            }
                        );
                    }

                    frame_cnt += 1;
                    last_frame_num = frame_cnt;

                    capture.Dispose();
                }
            }
            // Azure Kinect 2대를 Standalone으로 사용 시
            else
            {
                Dictionary<string, object> k1_info = (Dictionary<string, object>)kinect_list[0];
                int k1_idx = (int)k1_info["index"];
                Device k1_kinect = (Device)k1_info["kinect_device"];
                DeviceConfiguration k1s_config = (DeviceConfiguration)k1_info["sensor_config"];
                Tracker k1_tracker = (Tracker)k1_info["kinect_tracker"];
                Calibration k1_calibration = (Calibration)k1_info["calibration"];
                Transformation k1_transformation = (Transformation)k1_info["transformation"];

                ComboBox k1_loc_combox = (ComboBox)loctxt_list[k1_idx];
                int k1_loc_idx = 0;
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    k1_loc_idx = k1_loc_combox.SelectedIndex;
                }));
                string k1_kinect_loc = "";
                int k1_camera_id = 0;
                switch (k1_loc_idx)
                {
                    case 0:
                        k1_kinect_loc = "kf";
                        k1_camera_id = 0;
                        break;
                    case 1:
                        k1_kinect_loc = "kl";
                        k1_camera_id = 1;
                        break;
                    case 2:
                        k1_kinect_loc = "kr";
                        k1_camera_id = 2;
                        break;
                }

                Dictionary<string, object> k2_info = (Dictionary<string, object>)kinect_list[1];
                int k2_idx = (int)k2_info["index"];
                Device k2_kinect = (Device)k2_info["kinect_device"];
                DeviceConfiguration k2s_config = (DeviceConfiguration)k2_info["sensor_config"];
                Tracker k2_tracker = (Tracker)k2_info["kinect_tracker"];
                Calibration k2_calibration = (Calibration)k2_info["calibration"];
                Transformation k2_transformation = (Transformation)k2_info["transformation"];

                ComboBox k2_loc_combox = (ComboBox)loctxt_list[k2_idx];
                int k2_loc_idx = 0;
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    k2_loc_idx = k2_loc_combox.SelectedIndex;
                }));
                string k2_kinect_loc = "";
                int k2_camera_id = 0;
                switch (k2_loc_idx)
                {
                    case 0:
                        k2_kinect_loc = "kf";
                        k2_camera_id = 0;
                        break;
                    case 1:
                        k2_kinect_loc = "kl";
                        k2_camera_id = 1;
                        break;
                    case 2:
                        k2_kinect_loc = "kr";
                        k2_camera_id = 2;
                        break;
                }

                var k1_ci_path = "";
                var k2_ci_path = "";
                var k1_di_path = "";
                var k2_di_path = "";
                var k1_ti_path = "";
                var k2_ti_path = "";
                var k1_ii_path = "";
                var k2_ii_path = "";
                var k1_ji_path = "";
                var k2_ji_path = "";
                var cv_path = "";
                var an_path = "";
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    if (SCSwitch.IsOn)
                    {
                        // Save Calibration Information
                        var cd_path = System.IO.Path.Combine(trg_path, "0_calibration");
                        DirectoryInfo cal_dir = new DirectoryInfo(cd_path);

                        if (!cal_dir.Exists)
                        {
                            cal_dir.Create();

                            object[] k1_cal_name_arr = { subject, date, k1_kinect_loc, game_info, "calibration" };
                            object[] k2_cal_name_arr = { subject, date, k2_kinect_loc, game_info, "calibration" };
                            string k1_cal_name = string.Join("_", k1_cal_name_arr);
                            string k2_cal_name = string.Join("_", k2_cal_name_arr);

                            object[] k1_cal_file_arr = { k1_cal_name, "json" };
                            object[] k2_cal_file_arr = { k2_cal_name, "json" };
                            string k1_cal_file = string.Join(".", k1_cal_file_arr);
                            string k2_cal_file = string.Join(".", k2_cal_file_arr);

                            var k1_cal_path = System.IO.Path.Combine(cd_path, k1_cal_file);
                            var k2_cal_path = System.IO.Path.Combine(cd_path, k2_cal_file);
                            string k1_serial_num = k1_kinect.SerialNum;
                            string k2_serial_num = k2_kinect.SerialNum;

                            string k1k_calibration = AKCalibration.AKCalibrations(k1_camera_id, k1_kinect_loc,k1_serial_num, k1_calibration);
                            string k2k_calibration = AKCalibration.AKCalibrations(k2_camera_id, k2_kinect_loc, k2_serial_num, k2_calibration);
                            AKDataSave.AKCalibration(k1k_calibration, k1_cal_path);
                            AKDataSave.AKCalibration(k2k_calibration, k2_cal_path);
                        }

                        // Set Color Path
                        if (CISwitch.IsOn)
                        {
                            k1_ci_path = System.IO.Path.Combine(trg_path, "1_color", k1_kinect_loc);
                            DirectoryInfo k1_ci_dir = new DirectoryInfo(k1_ci_path);
                            if (!k1_ci_dir.Exists)
                            {
                                k1_ci_dir.Create();
                            }
                            k2_ci_path = System.IO.Path.Combine(trg_path, "1_color", k2_kinect_loc);
                            DirectoryInfo k2_ci_dir = new DirectoryInfo(k2_ci_path);
                            if (!k2_ci_dir.Exists)
                            {
                                k2_ci_dir.Create();
                            }
                        }

                        // Set Depth Path
                        if (DISwitch.IsOn)
                        {
                            k1_di_path = System.IO.Path.Combine(trg_path, "2_depth", k1_kinect_loc);
                            DirectoryInfo k1_di_dir = new DirectoryInfo(k1_di_path);
                            if (!k1_di_dir.Exists)
                            {
                                k1_di_dir.Create();
                            }
                            k2_di_path = System.IO.Path.Combine(trg_path, "2_depth", k2_kinect_loc);
                            DirectoryInfo k2_di_dir = new DirectoryInfo(k2_di_path);
                            if (!k2_di_dir.Exists)
                            {
                                k2_di_dir.Create();
                            }

                            k1_ti_path = System.IO.Path.Combine(trg_path, "3_trdepth", k1_kinect_loc);
                            DirectoryInfo k1_ti_dir = new DirectoryInfo(k1_ti_path);
                            if (!k1_ti_dir.Exists)
                            {
                                k1_ti_dir.Create();
                            }
                            k2_ti_path = System.IO.Path.Combine(trg_path, "3_trdepth", k2_kinect_loc);
                            DirectoryInfo k2_ti_dir = new DirectoryInfo(k2_ti_path);
                            if (!k2_ti_dir.Exists)
                            {
                                k2_ti_dir.Create();
                            }
                        }

                        // Set IR Path
                        if (IISwitch.IsOn)
                        {
                            k1_ii_path = System.IO.Path.Combine(trg_path, "4_ir", k1_kinect_loc);
                            DirectoryInfo k1_ii_dir = new DirectoryInfo(k1_ii_path);
                            if (!k1_ii_dir.Exists)
                            {
                                k1_ii_dir.Create();
                            }
                            k2_ii_path = System.IO.Path.Combine(trg_path, "4_ir", k2_kinect_loc);
                            DirectoryInfo k2_ii_dir = new DirectoryInfo(k2_ii_path);
                            if (!k2_ii_dir.Exists)
                            {
                                k2_ii_dir.Create();
                            }
                        }

                        // Set Joint Path
                        if (JDSwitch.IsOn)
                        {
                            k1_ji_path = System.IO.Path.Combine(trg_path, "5_joint", k1_kinect_loc);
                            DirectoryInfo k1_ji_dir = new DirectoryInfo(k1_ji_path);
                            if (!k1_ji_dir.Exists)
                            {
                                k1_ji_dir.Create();
                            }
                            k2_ji_path = System.IO.Path.Combine(trg_path, "5_joint", k2_kinect_loc);
                            DirectoryInfo k2_ji_dir = new DirectoryInfo(k2_ji_path);
                            if (!k2_ji_dir.Exists)
                            {
                                k2_ji_dir.Create();
                            }
                        }

                        // Set Video Path
                        if (CVSwitch.IsOn)
                        {
                            cv_path = System.IO.Path.Combine(trg_path, "6_video");
                            DirectoryInfo cv_dir = new DirectoryInfo(cv_path);
                            if (!cv_dir.Exists)
                            {
                                cv_dir.Create();
                            }
                        }

                        // Set Annotation Path
                        an_path = System.IO.Path.Combine(trg_path, "7_annotation");
                        DirectoryInfo an_dir = new DirectoryInfo(an_path);
                        if (!an_dir.Exists)
                        {
                            an_dir.Create();
                        }
                    }
                }));

                k1_kinect.StartCameras(k1s_config);
                k2_kinect.StartCameras(k2s_config);
                stop_watch.Start();
                int frame_cnt = 0;
                while (record_chk)
                {
                    TimeSpan time_span = stop_watch.Elapsed;
                    string elapsed_time = string.Format("{0:00}:{1:00}.{2:00}",
                        time_span.Minutes, time_span.Seconds, time_span.Milliseconds / 10);

                    string date_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentUICulture.DateTimeFormat);
                    capture_time_dict.Add(frame_cnt, date_time);

                    Microsoft.Azure.Kinect.Sensor.Capture k1_capture = null;
                    Microsoft.Azure.Kinect.Sensor.Capture k2_capture = null;
                    Parallel.Invoke(
                        () =>
                        {
                            k1_capture = k1_kinect.GetCapture();
                            _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                TimeText.Text = elapsed_time;
                                if (SCSwitch.IsOn)
                                {
                                    if (JDSwitch.IsOn)
                                    {
                                        k1_tracker.EnqueueCapture(k1_capture);
                                    }
                                }
                            }));
                        },
                        () =>
                        {
                            Thread.Sleep(2);
                            k2_capture = k2_kinect.GetCapture();
                            _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                if (SCSwitch.IsOn)
                                {
                                    if (JDSwitch.IsOn)
                                    {
                                        k2_tracker.EnqueueCapture(k2_capture);
                                    }
                                }
                            }));
                        }
                    );
                    if (k2_capture != null && k1_capture != null)
                    {
                        Parallel.Invoke(
                            () => {
                                Microsoft.Azure.Kinect.Sensor.Image k1_cimg = k1_capture.Color;
                                WriteableBitmap k1_color_wbitmap = AKImageConvert.ColorConvert(k1_cimg);
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    System.Windows.Controls.Image k1k_image = (System.Windows.Controls.Image)img_list[k1_idx];
                                    k1k_image.Source = k1_color_wbitmap;
                                    k1k_image.Visibility = Visibility.Visible;
                                    if (SCSwitch.IsOn)
                                    {
                                        if (CISwitch.IsOn)
                                        {
                                            object[] k1_ci_name_arr = { subject, date, k1_kinect_loc, game_info, "color", frame_cnt.ToString().PadLeft(9, '0') };
                                            string k1_ci_name = string.Join("_", k1_ci_name_arr);
                                            object[] k1_ci_file_arr = { k1_ci_name, "jpg" };
                                            string k1_ci_file = string.Join(".", k1_ci_file_arr);

                                            var k1_cimg_path = System.IO.Path.Combine(k1_ci_path, k1_ci_file);

                                            Task k1_save_color = Task.Run(() => AKDataSave.AKColorImage(k1_cimg_path, k1_color_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                Microsoft.Azure.Kinect.Sensor.Image k2_cimg = k2_capture.Color;
                                WriteableBitmap k2_color_wbitmap = AKImageConvert.ColorConvert(k2_cimg);
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    System.Windows.Controls.Image k2k_image = (System.Windows.Controls.Image)img_list[k2_idx];
                                    k2k_image.Source = k2_color_wbitmap;
                                    k2k_image.Visibility = Visibility.Visible;
                                    if (SCSwitch.IsOn)
                                    {
                                        if (CISwitch.IsOn)
                                        {
                                            object[] k2_ci_name_arr = { subject, date, k2_kinect_loc, game_info, "color", frame_cnt.ToString().PadLeft(9, '0') };
                                            string k2_ci_name = string.Join("_", k2_ci_name_arr);
                                            object[] k2_ci_file_arr = { k2_ci_name, "jpg" };
                                            string k2_ci_file = string.Join(".", k2_ci_file_arr);

                                            var k2_cimg_path = System.IO.Path.Combine(k2_ci_path, k2_ci_file);

                                            Task k2_save_color = Task.Run(() => AKDataSave.AKColorImage(k2_cimg_path, k2_color_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (DISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image k1_dimg = k1_capture.Depth;
                                            WriteableBitmap k1_depth_wbitmap = AKImageConvert.DepthConvert(k1_dimg);
                                            WriteableBitmap k1_tr_depth_wbitmap = AKImageConvert.TrDepthConvert(k1_transformation, k1_dimg);

                                            object[] k1_di_name_arr = { subject, date, k1_kinect_loc, game_info, "depth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string k1_di_name = string.Join("_", k1_di_name_arr);
                                            object[] k1_di_file_arr = { k1_di_name, "png" };
                                            string k1_di_file = string.Join(".", k1_di_file_arr);

                                            var k1_dimg_path = System.IO.Path.Combine(k1_di_path, k1_di_file);

                                            object[] k1_trdi_name_arr = { subject, date, k1_kinect_loc, game_info, "trdepth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string k1_trdi_name = string.Join("_", k1_trdi_name_arr);
                                            object[] k1_trdi_file_arr = { k1_trdi_name, "png" };
                                            string k1_trdi_file = string.Join(".", k1_trdi_file_arr);

                                            var k1_trdimg_path = System.IO.Path.Combine(k1_ti_path, k1_trdi_file);

                                            Task k1_save_depth = Task.Run(() => AKDataSave.AKPNGImage(k1_dimg_path, k1_depth_wbitmap));
                                            Task k1_save_trdepth = Task.Run(() => AKDataSave.AKPNGImage(k1_trdimg_path, k1_tr_depth_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (DISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image k2_dimg = k2_capture.Depth;
                                            WriteableBitmap k2_depth_wbitmap = AKImageConvert.DepthConvert(k2_dimg);
                                            WriteableBitmap k2_tr_depth_wbitmap = AKImageConvert.TrDepthConvert(k2_transformation, k2_dimg);

                                            object[] k2_di_name_arr = { subject, date, k2_kinect_loc, game_info, "depth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string k2_di_name = string.Join("_", k2_di_name_arr);
                                            object[] k2_di_file_arr = { k2_di_name, "png" };
                                            string k2_di_file = string.Join(".", k2_di_file_arr);

                                            var k2_dimg_path = System.IO.Path.Combine(k2_di_path, k2_di_file);

                                            object[] k2_trdi_name_arr = { subject, date, k2_kinect_loc, game_info, "trdepth", frame_cnt.ToString().PadLeft(9, '0') };
                                            string k2_trdi_name = string.Join("_", k2_trdi_name_arr);
                                            object[] k2_trdi_file_arr = { k2_trdi_name, "png" };
                                            string k2_trdi_file = string.Join(".", k2_trdi_file_arr);

                                            var k2_trdimg_path = System.IO.Path.Combine(k2_ti_path, k2_trdi_file);

                                            Task k2_save_depth = Task.Run(() => AKDataSave.AKPNGImage(k2_dimg_path, k2_depth_wbitmap));
                                            Task k2_save_trdepth = Task.Run(() => AKDataSave.AKPNGImage(k2_trdimg_path, k2_tr_depth_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (IISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image k1_iimg = k1_capture.IR;
                                            WriteableBitmap k1_ir_wbitmap = AKImageConvert.IRConvert(k1_iimg);

                                            object[] k1_ii_name_arr = { subject, date, k1_kinect_loc, game_info, "ir", frame_cnt.ToString().PadLeft(9, '0') };
                                            string k1_ii_name = string.Join("_", k1_ii_name_arr);
                                            object[] k1_ii_file_arr = { k1_ii_name, "png" };
                                            string k1_ii_file = string.Join(".", k1_ii_file_arr);

                                            var k1_iimg_path = System.IO.Path.Combine(k1_ii_path, k1_ii_file);
                                            Task k1_save_ir = Task.Run(() => AKDataSave.AKPNGImage(k1_iimg_path, k1_ir_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () => {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (IISwitch.IsOn)
                                        {
                                            Microsoft.Azure.Kinect.Sensor.Image k2_iimg = k2_capture.IR;
                                            WriteableBitmap k2_ir_wbitmap = AKImageConvert.IRConvert(k2_iimg);

                                            object[] k2_ii_name_arr = { subject, date, k2_kinect_loc, game_info, "ir", frame_cnt.ToString().PadLeft(9, '0') };
                                            string k2_ii_name = string.Join("_", k2_ii_name_arr);
                                            object[] k2_ii_file_arr = { k2_ii_name, "png" };
                                            string k2_ii_file = string.Join(".", k2_ii_file_arr);

                                            var k2_iimg_path = System.IO.Path.Combine(k2_ii_path, k2_ii_file);
                                            Task k2_save_ir = Task.Run(() => AKDataSave.AKPNGImage(k2_iimg_path, k2_ir_wbitmap));
                                        }
                                    }
                                }));
                            },
                            () =>
                            {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (JDSwitch.IsOn)
                                        {
                                            string k1_annotation_json = Task.Run(() => AKTracker.PopTracker(k1_camera_id, frame_cnt, date_time, k1_tracker, k1_calibration)).Result;

                                            object[] jd_name_arr = { subject, date, k1_kinect_loc, game_info, "joint", frame_cnt.ToString().PadLeft(9, '0') };
                                            string jd_name = string.Join("_", jd_name_arr);
                                            object[] jd_file_arr = { jd_name, "json" };
                                            string jd_file = string.Join(".", jd_file_arr);

                                            var jd_path = System.IO.Path.Combine(k1_ji_path, jd_file);
                                            Task save_jd = Task.Run(() => AKDataSave.AKJointData(k1_annotation_json, jd_path));
                                        }
                                    }
                                }));
                            },
                            () =>
                            {
                                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    if (SCSwitch.IsOn)
                                    {
                                        if (JDSwitch.IsOn)
                                        {
                                            string k2_annotation_json = Task.Run(() => AKTracker.PopTracker(k2_camera_id, frame_cnt, date_time, k2_tracker, k2_calibration)).Result;

                                            object[] jd_name_arr = { subject, date, k2_kinect_loc, game_info, "joint", frame_cnt.ToString().PadLeft(9, '0') };
                                            string jd_name = string.Join("_", jd_name_arr);
                                            object[] jd_file_arr = { jd_name, "json" };
                                            string jd_file = string.Join(".", jd_file_arr);

                                            var jd_path = System.IO.Path.Combine(k2_ji_path, jd_file);
                                            Task save_jd = Task.Run(() => AKDataSave.AKJointData(k2_annotation_json, jd_path));
                                        }
                                    }
                                }));
                            }
                        );
                    }

                    frame_cnt += 1;
                    last_frame_num = frame_cnt;

                    k1_capture.Dispose();
                    k2_capture.Dispose();
                }
            }
        }

        // Azure Kinect Record Stop Function //
        // Azure Kinect Capture 종료 함수
        public double real_fps = 0.0;
        public void KinectRecordStop()
        {
            int k_cnt = kinect_list.Count;

            for (int k_idx = 0; k_idx < k_cnt; k_idx++)
            {
                Dictionary<string, object> kinect_info = (Dictionary<string, object>)kinect_list[k_idx];

                Device kinect_device = (Device)kinect_info["kinect_device"];

                kinect_device.StopCameras();
            }

            stop_watch.Stop();
            TimeSpan time_span = stop_watch.Elapsed;

            int stop_min = time_span.Minutes;
            int stop_sec = time_span.Seconds;

            double total_sec = (stop_min * 60) + stop_sec;
            real_fps = last_frame_num / total_sec;

            Thread.Sleep(1000);
            _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                stop_watch.Reset();
                TimeText.Text = "00:00:00";

                if (SCSwitch.IsOn)
                {
                    if (CVSwitch.IsOn)
                    {
                        for (int k_idx = 0; k_idx < k_cnt; k_idx++)
                        {
                            Dictionary<string, object> k_info = (Dictionary<string, object>)kinect_list[k_idx];
                            int idx = (int)k_info["index"];
                            ComboBox loc_combox = (ComboBox)loctxt_list[idx];
                            int loc_idx = loc_combox.SelectedIndex;
                            string kinect_loc = "";
                            switch (loc_idx)
                            {
                                case 0:
                                    kinect_loc = "kf";
                                    break;
                                case 1:
                                    kinect_loc = "kl";
                                    break;
                                case 2:
                                    kinect_loc = "kr";
                                    break;
                            }

                            object[] ci_name_arr = { subject, date, kinect_loc, game_info, "color", "%09d" };
                            string ci_name = string.Join("_", ci_name_arr);
                            object[] ci_file_arr = { ci_name, "jpg" };
                            string ci_file = string.Join(".", ci_file_arr);

                            object[] cv_name_arr = { subject, date, kinect_loc, game_info, "video" };
                            string cv_name = string.Join("_", cv_name_arr);
                            object[] cv_file_arr = { cv_name, "mp4" };
                            string cv_file = string.Join(".", cv_file_arr);

                            var cimg_path = System.IO.Path.Combine(trg_path, "1_color", kinect_loc, ci_file);
                            var cv_path = System.IO.Path.Combine(trg_path, "6_video", cv_file);

                            AKDataSave.AKVideo(real_fps, cimg_path, cv_path);
                        }
                    }
                }
            }));

            for (int idx = 0; idx < 10; idx++)
            {
                Thread.Sleep(300);
                _ = Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    BitmapImage bg = new BitmapImage(new Uri("/Resource/bg_white.jpg", UriKind.RelativeOrAbsolute));
                    KImage_1.Source = bg;
                    KImage_2.Source = bg;
                }));
            }
        }
    }
}
