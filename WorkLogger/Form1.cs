using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
//using System.Data.SqlClient;
using System.Windows.Forms.DataVisualization.Charting;
//using System.Collections.Generic;
using System.Runtime.InteropServices;
//[DllImport("user32.dll")]
//static extern IntPtr GetActiveWindow();
using System.IO;


namespace WorkLogger
{
    // http://hongliang.seesaa.net/article/7539988.html
    ///<summary>メッセージコードを表す。</summary>
    internal enum KeyboardMessage
    {
        ///<summary>キーが押された。</summary>
        KeyDown = 0x100,
        ///<summary>キーが放された。</summary>
        KeyUp = 0x101,
        ///<summary>システムキーが押された。</summary>
        SysKeyDown = 0x104,
        ///<summary>システムキーが放された。</summary>
        SysKeyUp = 0x105,
    }
    ///<summary>キーボードの状態を表す。</summary>
    internal struct KeyboardState
    {
        ///<summary>仮想キーコード。</summary>
        public Keys KeyCode;
        ///<summary>スキャンコード。</summary>
        public int ScanCode;
        ///<summary>各種特殊フラグ。</summary>
        public KeyboardStateFlag Flag;
        ///<summary>このメッセージが送られたときの時間。</summary>
        public int Time;
        ///<summary>メッセージに関連づけられた拡張情報。</summary>
        public IntPtr ExtraInfo;
    }
    ///<summary>キーボードの状態を補足する。</summary>
    internal struct KeyboardStateFlag
    {
        private int flag;
        private bool IsFlagging(int value)
        {
            return (flag & value) != 0;
        }
        private void Flag(bool value, int digit)
        {
            flag = value ? (flag | digit) : (flag & ~digit);
        }
        ///<summary>キーがテンキー上のキーのような拡張キーかどうかを表す。</summary>
        public bool IsExtended { get { return IsFlagging(0x01); } set { Flag(value, 0x01); } }
        ///<summary>イベントがインジェクトされたかどうかを表す。</summary>
        public bool IsInjected { get { return IsFlagging(0x10); } set { Flag(value, 0x10); } }
        ///<summary>ALTキーが押されているかどうかを表す。</summary>
        public bool AltDown { get { return IsFlagging(0x20); } set { Flag(value, 0x20); } }
        ///<summary>キーが放されたどうかを表す。</summary>
        public bool IsUp { get { return IsFlagging(0x80); } set { Flag(value, 0x80); } }
    }

    public partial class Form1 : Form
    {
        private List<string[]> m_data = new List<string[]>();

        private Dictionary<string, int> m_dict = new Dictionary<string, int>();
        static int KeySelector(KeyValuePair<string, int> pair)
        {
            // 並べ替えの際のキーにValueの値を使用する
            return pair.Value;
        }

        private int m_totaltime = 0; // total time
        private int m_loggedtime = 0; // logged time (a part of total time)
        private int m_interval = 1000; // ms
        private Timer m_timer = new Timer();

        private bool m_bLogging = true; // logging or not

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m_timer.Tick += new EventHandler(MyCallback);
            m_timer.Interval = m_interval;
            m_timer.Enabled = true;

            listView1.View = View.Details;
            listView1.Columns.Add("Program", 200);
            listView1.Columns.Add("File/Title", 300);
        }

        [DllImport("user32.dll")]
        static extern int GetActiveWindow();
        [DllImport("user32.dll")]
        static extern int GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern int IsWindowVisible(int hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(int hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(int hWnd, out uint ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int hookType, KeyboardHookDelegate hookDelegate, IntPtr hInstance, uint threadId);
        //http://mycsharp.seesaa.net/article/132281109.html
        //private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int CallNextHookEx(IntPtr hook, int code, KeyboardMessage message, ref KeyboardState state);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        private delegate int KeyboardHookDelegate(int code, KeyboardMessage message, ref KeyboardState state);


        public void MyCallback(object sender, EventArgs e)
        {
            int hWnd;
            int duration; // (ms) last duration (period?)

            m_totaltime += m_interval;
            if (m_bLogging)
            {
                m_loggedtime += m_interval;
            }
            duration = m_interval; // ms

            {
                int sec = m_loggedtime / 1000;
                int hour = sec / 3600;
                int minute = (sec - 3600 * hour) / 60;
                int second = sec % 60;
                label1.Text = "Elapsed Time: " + hour.ToString() + ":" + minute.ToString("00") + ":" + second.ToString("00");// (m_loggedtime / 1000).ToString() + " sec";
            }
            {
                int sec = m_totaltime / 1000;
                int hour = sec / 3600;
                int minute = (sec - 3600 * hour) / 60;
                int second = sec % 60;
                label1.Text += System.Environment.NewLine;
                label1.Text += "Total Time: " + hour.ToString() + ":" + minute.ToString("00") + ":" + second.ToString("00");// (m_loggedtime / 1000).ToString() + " sec";
            }

            if (m_bLogging == false)
            {
                return;
            }


            //hWnd = GetActiveWindow();
            hWnd = GetForegroundWindow();
            StringBuilder sb = new StringBuilder(0x1024);
            if (/*IsWindowVisible(hWnd) != 0 && */GetWindowText(hWnd, sb, sb.Capacity) != 0)
            {
                string title = sb.ToString();
                //label1.Text = title;
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                Process p = Process.GetProcessById((int)pid);
                string programpath = p.MainModule.FileName.ToString();
                //label1.Text = programpath;

                string[] delimiter = { "\\" };
                string[] delimiter2 = { " - " };
                string[] sttmp = programpath.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                string[] sttmp2 = title.Split(delimiter2, StringSplitOptions.RemoveEmptyEntries);
                string programname = sttmp[sttmp.Length - 1];
                string filename = sttmp2[0];

                //textBox1.Text += programname + ": " + filename + System.Environment.NewLine;
                //textBox1.SelectionStart = textBox1.Text.Length;
                //textBox1.ScrollToCaret();


                string[] item = { programname, filename };
                listView1.Items.Add(new ListViewItem(item));
                listView1.EnsureVisible(listView1.Items.Count - 1);

                m_data.Add(item);

                if (m_dict.ContainsKey(programname))
                {
                    m_dict[programname] += duration;
                }
                else
                {
                    m_dict[programname] = duration;
                }

                // chart start
                Series series = new Series();
                series.ChartType = SeriesChartType.Pie;                
                series["PieStartAngle"] = "270";

                //foreach (string[] data in m_data)
                IOrderedEnumerable<KeyValuePair<string, int>> sorted = m_dict.OrderByDescending(pair => pair.Value);
                foreach (KeyValuePair<string, int> pair in sorted/*m_dict*/)
                {
                    DataPoint point = new DataPoint();
                    point.XValue = 0;
                    point.YValues = new double[] { pair.Value };
                    {
                        int sec = pair.Value / 1000;
                        int hour = sec / 3600;
                        int minute = (sec - 3600 * hour) / 60;
                        int second = sec % 60;
                        point.Label = pair.Key + " (" + hour.ToString() + ":" + minute.ToString("00") + ":" + second.ToString("00") + ")";
                    }
                    point["3DLabelLineSize"] = "10";
                    point.IsValueShownAsLabel = true;
                    series.Points.Add(point);
                }
                chart1.Series.Clear();
                chart1.Series.Add(series);
            }
            else
            {
                if (IsWindowVisible(hWnd) == 0)
                {
                    label1.Text = "not visible";
                }
                else
                {
                    label1.Text = "window text failed";
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            m_bLogging = !m_bLogging;
            if (m_bLogging)
            {
                button1.Text = "&Out of Desk";
            }
            else
            {
                button1.Text = "&Resume work";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            m_timer.Enabled = false;

            Form2 f = new Form2();
            f.SetInterval(m_interval);
            f.ShowDialog();

            if (f.m_closeok)
            {
                m_interval = f.GetInterval();
                m_timer.Interval = m_interval;
                m_timer.Enabled = true;
            }
                
            //f.Owner = this;
            //f.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Load
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = "Text file(*.txt)|*.txt|All files(*.*)|*.*";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Save
            m_timer.Enabled = false;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter = "Text file(*.txt)|*.txt|All files(*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                SaveData(dlg.FileName);
            }

            m_timer.Enabled = true;
        }

        private void SaveData(string filename)
        {
            //StreamReader sr = new StreamReader(filename, Encoding.GetEncoding("Shift_JIS"));
            StreamWriter sw = new StreamWriter(filename, false, Encoding.GetEncoding("Shift_JIS"));
            IOrderedEnumerable<KeyValuePair<string, int>> sorted = m_dict.OrderByDescending(pair => pair.Value);
            foreach (KeyValuePair<string, int> pair in sorted/*m_dict*/)
            {
                sw.WriteLine(pair.Key + "\t" + pair.Value/* + sw.NewLine*/);
            }
            sw.Close();
        }
    }
}
