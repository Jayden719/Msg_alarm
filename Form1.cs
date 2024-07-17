using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace Msg_alarm
{
    public partial class Form1 : Form
    {
        //실행됨과 동시에 최초 로그파일 경로 저장
        string LogPath = Application.StartupPath + "\\log\\log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
        string FolderPath = Application.StartupPath + "\\log";

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileInt(string section, string key, int val, string filepath);

        string iniDirectory = Application.StartupPath + @"\alarm_config.ini";

        public int ReadINI(string sec, string keyname, string fp)
        {
            int time;
            time = GetPrivateProfileInt(sec, keyname, 0, fp);
            return time;
        }

        System.Timers.Timer timer_start = new System.Timers.Timer();
        System.Timers.Timer timer_msg = new System.Timers.Timer();
        System.Timers.Timer timer_srv = new System.Timers.Timer();

        int hours = 0;
        int start_hour = 0;
        int start_min = 0;
        int scnt = 0;
        int mcnt = 0;
        List<string> S_splitSrv = new List<string>();
        List<string> M_splitSrv = new List<string>();
        List<string> res_splitSrv = new List<string>();
        List<string> rem_splitSrv = new List<string>();
        Dictionary<string, int> dics_splitSrv = new Dictionary<string, int>();
        Dictionary<string, int> dicm_splitSrv = new Dictionary<string, int>();
        int srvTimes = 0;
        int sresult = 0;
        int mresult = 0;
        bool newLog_timerStart = false;
        bool newLog_splitCheck = false;

        
        private void Logwriter()
        {
            // 로그 폴더(디렉토리) 존재 여부 판단하여 생성
            DirectoryInfo di = new DirectoryInfo(FolderPath);
            if (di.Exists == true)
            {

            }
            else
            {
                di.Create();
            }

            // 당일 로그 파일 존재 여부 판단하여 생성하기
            string LogPath_n = Application.StartupPath + "\\log\\log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            if (LogPath == LogPath_n)
            {
                // 존재여부
                System.IO.FileInfo fi = new System.IO.FileInfo(LogPath);
                if (fi.Exists == true)
                {

                }
                else
                {
                    System.IO.File.AppendAllText(LogPath_n, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " 로그 파일 최초 생성 ", Encoding.Default);
                }
            }
            else
            {
                // 프로그램 실행 중 day +1 상태
                System.IO.File.AppendAllText(LogPath_n, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " 로그 파일 최초 생성 ", Encoding.Default);
                LogPath = LogPath_n; // 전역변수에 최신화된 데이터 저장
              
                // 기존 타이머 인스턴스가 전역변수여서 interval 값 초기화가 필요함
                newLog_timerStart = true;
                newLog_splitCheck = true;
            }
        }

        public Form1()
        {
            InitializeComponent();
            Console.WriteLine("문자 서버 알림 프로그램 실행중...");
            Logwriter();
            this.ShowInTaskbar = false;
            this.Opacity = 0;

            start_hour = ReadINI("Timer", "start_h", iniDirectory);
            start_min = ReadINI("Timer", "start_m", iniDirectory);
            int end_hour = ReadINI("Timer", "stop_h", iniDirectory);
            int end_min = ReadINI("Timer", "stop_m", iniDirectory);

            if (start_hour < 10)
            {
                start_hour = Convert.ToInt32("0" + start_hour.ToString());
            }
            if (start_min < 10)
            {
                start_min = Convert.ToInt32("0" + start_min.ToString());
            }
            if (end_hour < 10)
            {
                end_hour = Convert.ToInt32("0" + end_hour.ToString());
            }
            if (end_min < 10)
            {
                end_min = Convert.ToInt32("0" + end_min.ToString());
            }                

            if (start_hour * 100 + start_min > Convert.ToInt32(DateTime.Now.ToString("HHmm")) || Convert.ToInt32(DateTime.Now.ToString("HHmm")) > end_hour*100 + end_min )
            {
                MessageBox.Show("시간 설정을 다시 해주세요");
                return;
            }          
            timer_start.Elapsed += new ElapsedEventHandler(Timer_start);
            timer_start.Start();           
        }

        private void Timer_start(object sender, ElapsedEventArgs e)
        {
            Logwriter();
            string LogPath_n = Application.StartupPath + "\\log\\log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " Timer_start 타이머 시작", Encoding.Default);
           
            //timer_start.Interval = ((hours * 60 * 60) + mins * 60) * 1000;
            // 프로그램 시작 메소드
            int end_hour = ReadINI("Timer", "stop_h", iniDirectory);
            int end_min = ReadINI("Timer", "stop_m", iniDirectory);
            int now = Convert.ToInt32(DateTime.Now.ToString("HHmm"));
            int now_h = Convert.ToInt32(DateTime.Now.ToString("HH"));
            int now_m = Convert.ToInt32(DateTime.Now.ToString("mm"));

            if (end_hour * 100 + end_min < Convert.ToInt32(DateTime.Now.ToString("HHmm")))
            {
                timer_start.Stop();
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " Timer_start 타이머 종료", Encoding.Default);
                return;
            }

            // 당일 시작 시간에 맞춰서 프로그램 실행된 경우
            if (now < start_hour * 100 + start_min + 1 && start_hour * 100 + start_min <= now)
            {
                // 여기서 24시간 후에 다시 반복 실행
                hours = 24;
                timer_start.Interval = hours * 60 * 60 * 1000;
            }
            else
            {
                // 09:00 ~ 16:00 사이에 실행되어 다음날 시작시간부터 시작 
                int interval = ((23 - now_h + start_hour) * 60 * 60 + (60 - now_m) * 60) * 1000;
                timer_start.Interval = interval;
            }

             if (newLog_timerStart)
             {
                newLog_timerStart = false;
                split_check(); //바로 시작
                timer_msg.Elapsed += new ElapsedEventHandler(split_check_timer);
                timer_msg.Start(); 
             }
             else
             {
                 timer_msg.Elapsed += new ElapsedEventHandler(split_check_timer);
                 timer_msg.Start();
             }
        }

        private void split_check_timer(object sender, ElapsedEventArgs e)
        {
            timer_msg.Interval = 60 * 60 * 1000;
            split_check();           
        }

        private void split_check()
        {
            int end_h = ReadINI("Timer", "stop_h", iniDirectory);
            int end_m = ReadINI("Timer", "stop_m", iniDirectory);
            string LogPath_n = Application.StartupPath + "\\log\\log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " split_check 타이머 시작", Encoding.Default);

            if (Convert.ToInt32(DateTime.Now.ToString("HHmm")) > end_h * 100 + end_m)
            {
                timer_msg.Stop();
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " split_check 타이머 종료", Encoding.Default);

                return;
            }
            dicm_splitSrv.Clear();
            dics_splitSrv.Clear();
            S_splitSrv.Clear();
            M_splitSrv.Clear();
            res_splitSrv.Clear();
            rem_splitSrv.Clear();


            string db_sql = "Server=222.231.58.71; database=SMS; uid=eshinan; pwd=!eshinan4600";
            int end_hour = ReadINI("Timer", "stop_h", iniDirectory);
            int end_min = ReadINI("Timer", "stop_m", iniDirectory);

            //SMS
            using (SqlConnection conn = new SqlConnection(db_sql))
            {
                conn.Open();
                string startDate = DateTime.Now.ToString("yyyy-MM-dd");

                string msg_sql = "select sSplitSrv from jobSmslog(nolock) " +
                    "where sSplitSrv !='' and sSplitSrv is not null and dtEndTime is null " +
                    "and dtStartTime between DATEADD(MINUTE, -60, GETDATE()) and GETDATE() group by sSplitSrv\r\n";

                SqlCommand comm = new SqlCommand(msg_sql, conn);
                SqlDataReader rdr = comm.ExecuteReader();

                while (rdr.Read())
                {
                    S_splitSrv.Add(rdr["sSplitSrv"].ToString());
                }
                rdr.Close();
                comm.Dispose();

                if (S_splitSrv.Count > 0)
                {
                    foreach (string srv in S_splitSrv)
                    {
                        if (srv.Length > 6)
                        {
                            string[] split_data = srv.Split(';');
                            //LINQ 실습
                            var datas = from data in split_data
                                        select data;

                            foreach (var s in datas)
                            {
                                res_splitSrv.Add(s.ToString());
                            }
                        }
                        else
                        {
                            res_splitSrv.Add(srv);
                        }
                    }
                    // 복수 스플릿 분리 및 중복 스플릿 제거된 리스트
                    res_splitSrv = res_splitSrv.Distinct().ToList();

                    foreach (string split in res_splitSrv)
                    {
                        string fail_sql = string.Format("Select top 1 tr_num from [{0}].dbo.SC_TRAN(nolock) where tr_sendstat=0" +
                            " and tr_senddate < getdate() order by tr_num asc \r\n", split);

                        SqlCommand com = new SqlCommand(fail_sql, conn);
                        SqlDataReader crdr = com.ExecuteReader();

                        while (crdr.Read())
                        {
                            if (dics_splitSrv.ContainsKey(split))
                            {
                                dics_splitSrv[split] = Convert.ToInt32(crdr["tr_num"]);
                            }
                            else
                            {
                                dics_splitSrv.Add(split, Convert.ToInt32(crdr["tr_num"]));
                            }
                        }
                        crdr.Close();
                        com.Dispose();
                    }
                    conn.Close();
                }
                else
                {

                }
            }

            //LMS MMS
            using (SqlConnection conn = new SqlConnection(db_sql))
            {
                conn.Open();
                string startDate = DateTime.Now.ToString("yyyy-MM-dd");

                string msg_sql = "select sSplitSrv from jobmmslog(nolock) " +
                    "where sSplitSrv !='' and sSplitSrv is not null and dtEndTime is null " +
                    "and dtStartTime between DATEADD(MINUTE, -60, GETDATE()) and GETDATE() group by sSplitSrv\r\n";

                SqlCommand comm = new SqlCommand(msg_sql, conn);
                SqlDataReader rdr = comm.ExecuteReader();

                while (rdr.Read())
                {
                    M_splitSrv.Add(rdr["sSplitSrv"].ToString());
                }
                rdr.Close();
                comm.Dispose();

                if (M_splitSrv.Count() <= 0)
                {

                }
                else
                {
                    foreach (string srv in M_splitSrv)
                    {
                        if (srv.Length > 6)
                        {
                            string[] split_data = srv.Split(';');
                            foreach (string s in split_data)
                            {
                                rem_splitSrv.Add(s);
                            }
                        }
                        else
                        {
                            rem_splitSrv.Add(srv);
                        }
                    }
                    // 복수 스플릿 분리 및 중복 스플릿 제거된 리스트
                    rem_splitSrv = rem_splitSrv.Distinct().ToList();

                    foreach (string split in rem_splitSrv)
                    {
                        string fail_sql = string.Format("Select top 1 MSGKEY from [{0}].dbo.MMS_MSG(nolock) where status=0 " +
                            "and reqdate < getdate() order by msgkey asc \r\n", split);

                        SqlCommand com = new SqlCommand(fail_sql, conn);
                        SqlDataReader crdr = com.ExecuteReader();

                        while (crdr.Read())
                        {
                            if (dicm_splitSrv.ContainsKey(split))
                            {
                                dicm_splitSrv[split] = Convert.ToInt32(crdr["MSGKEY"]);
                            }
                            else
                            {
                                dicm_splitSrv.Add(split, Convert.ToInt32(crdr["MSGKEY"]));
                            }
                        }
                        crdr.Close();
                        com.Dispose();
                    }
                    conn.Close();
                }

                if (newLog_splitCheck)
                {
                    // 타이머 전역변수로 인해 어제 설정된 interval 값을 오늘 처음 시작할 때 초기화 해야한다
                    newLog_splitCheck = false;
                    srv_recheck();
                    timer_srv.Elapsed += new ElapsedEventHandler(srv_recheck_timer);
                    timer_srv.Interval = 15 * 60 * 1000;
                    timer_srv.Start();
                }
                else
                {
                    timer_srv.Elapsed += new ElapsedEventHandler(srv_recheck_timer);
                    timer_srv.Interval = 15 * 60 * 1000;
                    timer_srv.Start();
                }
            }
        }

        private void srv_recheck_timer(object sender, ElapsedEventArgs e)
        {         
            srv_recheck();
        }

        private void srv_recheck()
        {
            string LogPath_n = Application.StartupPath + "\\log\\log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            Dictionary<string, int> dics_errsplitSrv = new Dictionary<string, int>();
            Dictionary<string, int> dicm_errsplitSrv = new Dictionary<string, int>();

            // 초기 실행 시 0에서부터 시작
            scnt += 1;
            mcnt += 1;
            srvTimes += 1;

            int end_h = ReadINI("Timer", "stop_h", iniDirectory);
            int end_m = ReadINI("Timer", "stop_m", iniDirectory);
            if (end_h < 10)
            {
                end_h = Convert.ToInt32("0" + end_h.ToString());
            }
            if (end_m < 10)
            {
                end_m = Convert.ToInt32("0" + end_m.ToString());
            }
            if (Convert.ToInt32(DateTime.Now.ToString("HHmm")) > end_h * 100 + end_m)
            {
                timer_srv.Stop();
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " srv_recheck 타이머 종료", Encoding.Default);

                timer_msg.Stop();
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " split_check 타이머 종료", Encoding.Default);

                dics_errsplitSrv.Clear();
                dicm_errsplitSrv.Clear();
                dicm_splitSrv.Clear();
                dics_splitSrv.Clear();

                // 체크 횟수 초과시 0으로 초기화
                srvTimes = 0;
                scnt = 0;
                mcnt = 0;

                return;
            }

            File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " srv_recheck 타이머 시작", Encoding.Default);

            string result_page_s = "";
            string result_page_m = "";
            string db_sql = "Server=222.231.58.71; database=SMS; uid=eshinan; pwd=!eshinan4600";

            // SMS
            using (SqlConnection conn = new SqlConnection(db_sql))
            {
                conn.Open();
                if (res_splitSrv.Count() <= 0)
                {

                }
                else
                {
                    foreach (string split in res_splitSrv)
                    {
                        string fail_sql = string.Format("Select top 1 tr_num from [{0}].dbo.SC_TRAN(nolock) where tr_sendstat=0 " +
                            "and tr_senddate < getdate() order by tr_num asc \r\n", split);

                        SqlCommand com = new SqlCommand(fail_sql, conn);
                        SqlDataReader crdr = com.ExecuteReader();

                        while (crdr.Read())
                        {
                            if (Convert.ToInt32(crdr["tr_num"]) > 0)
                            {
                                // 딕셔너리에 해당 스플릿이 존재 하는지 우선 체크
                                if (dics_errsplitSrv.ContainsKey(split))
                                {
                                    // 존재한다면 다시 조회한 실패건수와 기존의 실패건수가 동일여부 체크
                                    if (dics_splitSrv[split] == Convert.ToInt32(crdr["tr_num"]))
                                    {
                                        // 플레그 값 수정
                                        dics_errsplitSrv[split] = scnt;
                                        Console.WriteLine("tr_num" + Convert.ToInt32(crdr["tr_num"]));
                                    }
                                    else
                                    {
                                        dics_errsplitSrv.Add(split, scnt);
                                    }

                                }
                            }
                        }
                        crdr.Close();
                        com.Dispose();
                    }
                    conn.Close();
                }
            }

            // LMS MMS
            using (SqlConnection conn = new SqlConnection(db_sql))
            {
                conn.Open();
                if (rem_splitSrv.Count() <= 0)
                {

                }
                else
                {
                    foreach (string split in rem_splitSrv)
                    {
                        string fail_sql = string.Format("select top 1 MSGKEY from [{0}].dbo.MMS_MSG(nolock) where status=0" +
                            " and reqdate < getdate() order by msgkey asc \r\n", split);

                        SqlCommand com = new SqlCommand(fail_sql, conn);
                        SqlDataReader crdr = com.ExecuteReader();

                        while (crdr.Read())
                        {
                            if (Convert.ToInt32(crdr["MSGKEY"]) > 0)
                            {
                                if (dicm_errsplitSrv.ContainsKey(split))
                                {
                                    if (dicm_splitSrv[split] == Convert.ToInt32(crdr["MSGKEY"]))
                                    {
                                        dicm_errsplitSrv[split] = mcnt;
                                       
                                    }
                                }
                                else
                                {
                                    dicm_errsplitSrv.Add(split, mcnt);
                                }
                            }
                        }
                        crdr.Close();
                        com.Dispose();
                    }
                    conn.Close();
                }
            }
            List<string> srvs = new List<string>();
            List<string> srvm = new List<string>();

            // SMS 딕셔너리
            foreach (KeyValuePair<string, int> item in dics_errsplitSrv)
            {
                if (item.Value == 3)
                {
                    srvs.Add(item.Key);
                }
            }

            // LMS MMS 딕셔너리
            foreach (KeyValuePair<string, int> item in dicm_errsplitSrv)
            {
                if (item.Value == 3)
                {
                    srvm.Add(item.Key);
                }
            }

            // SMS 결과 
            if (srvs.Count == 0)
            {
                sresult += 1;
                if (sresult == 3)
                {
                    dics_errsplitSrv.Clear();
                    scnt = 0;
                    sresult = 0;
                }
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " SMS 문제 없습니다", Encoding.Default);

            }
            // 3번 모두 검사시 문제 있는경우
            else
            {
                foreach (string s in srvs)
                {
                    result_page_s += s + " 서버에 문제가 있습니다 \r\n";
                }
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " SMS 문제 이상 문자발송", Encoding.Default);
                dics_errsplitSrv.Clear();
                scnt = 0;
                sresult = 0;
                MessageBox.Show(result_page_s, "SMS 이상");
            }

            // LMS/MMS 결과
            if (srvm.Count == 0)
            {
                mresult += 1;
                if (mresult == 3)
                {
                    dics_errsplitSrv.Clear();
                    mcnt = 0;
                    mresult = 0;
                }
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " LMS MMS 문제 없습니다", Encoding.Default);
            }
            else
            {
                foreach (string s in srvm)
                {
                    result_page_m += s + " 서버에 문제가 있습니다 \r\n";
                }
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " LMS MMS 문제 이상 문자발송", Encoding.Default);
                dicm_errsplitSrv.Clear();
                mcnt = 0;
                mresult = 0;
                MessageBox.Show(result_page_m, "LMS/MMS 이상");
            }

            if (srvTimes >= 3)
            {
                timer_srv.Stop();
                timer_srv.Enabled = false;
                File.AppendAllText(LogPath_n, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " srv_recheck 타이머 종료", Encoding.Default);
                dics_errsplitSrv.Clear();
                dicm_errsplitSrv.Clear();
                dicm_splitSrv.Clear();
                dics_splitSrv.Clear();

                // 체크 횟수 초과시 0으로 초기화
                srvTimes = 0;
                scnt = 0;
                mcnt = 0;
                return;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = string.Format("문자 서버 알림 프로그램 ver_{0}", Application.ProductVersion);
        }
    }
}
