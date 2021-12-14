using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using REghZyFramework.ViewModels;
using REghZyFramework.Themes;
using REghZyFramework.Utilities;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Media;

namespace REghZyFramework
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {   
        // Clock
        System.Windows.Threading.DispatcherTimer Timer = new System.Windows.Threading.DispatcherTimer();
        // Site + Alarm + Break
        System.Windows.Threading.DispatcherTimer Timer2 = new System.Windows.Threading.DispatcherTimer();

        // For Pomodoro Label
        System.Windows.Threading.DispatcherTimer Timer4 = new System.Windows.Threading.DispatcherTimer();
        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr hWnd);
        bool Started = false;
        public App CurrentApplication { get; set; }
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        private bool BreakTime = true;
        private double PomoOfficeTime = 25*60;
        private double PomoBreakTime = 5*60;
        private double PomoWorktime = 0;
        private bool PomoMode = true; // ture = work mode; false = break mode
        private bool PomoModeStarted = false;
        public MainWindow()
        {
            InitializeComponent();

            // Init Clock
            Timer.Tick += new EventHandler(Timer_Click);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 3); // 0.3 sec for preventing alliasing
            Timer.Start();

            // Init Site check
            Timer2.Tick += new EventHandler(SiteCheck);
            // Init Alarm check
            Timer2.Tick += new EventHandler(AlarmCheck);
            // Inite Break check
            Timer2.Tick += new EventHandler(BreakCheck);

            Timer2.Interval = new TimeSpan(0, 0, 5); 
            Timer2.Start();


            Timer4.Interval = TimeSpan.FromSeconds(0.5);
            Timer4.Tick += new EventHandler(PomoLabelChange);
            PomoWorktime = PomoOfficeTime;
            PomoLabel.Content = TimeSpan.FromSeconds(PomoOfficeTime).ToString();
        }


        private void _PlaySOund(string soundFile)
        {
            SoundPlayer player = new SoundPlayer();
            player.SoundLocation = soundFile;
            player.Play();
        }
        private void _RaiseMessage(string message, bool PlaySound, string SoundFile)
        {
            // Top most mode
            Dispatcher.Invoke(() =>
            {
                this.Activate();
                this.WindowState = System.Windows.WindowState.Normal;
                this.Topmost = true;
                this.Focus();
                Xceed.Wpf.Toolkit.MessageBox mbox = new Xceed.Wpf.Toolkit.MessageBox();
                mbox.Style = (Style)Resources["MessageBoxStyle1"];
                mbox.Text = message;
                if (PlaySound)
                {
                    _PlaySOund(SoundFile);
                }
                mbox.ShowDialog();
                this.Topmost = false;
            });

        }

        private IEnumerable<string> _getCurrentTab(string Browser)
        {
            // Split to browser name
            string[] subs = Browser.Split(':');
            string BrowserTrim = subs[1].Trim();
            Process[] procsBrowser = Process.GetProcessesByName(BrowserTrim);
            if (procsBrowser.Any())
            {
                List<string> _tabs = new List<string>();
                foreach (Process singleProcess in procsBrowser)
                {
                    IntPtr hWnd = singleProcess.MainWindowHandle;
                    int length = GetWindowTextLength(hWnd);

                    StringBuilder text = new StringBuilder(length + 1);
                    GetWindowText(hWnd, text, text.Capacity);
                    _tabs.Add(text.ToString());
                }
                return _tabs;
            }
            return Enumerable.Empty<string>();
        }

        private void ChangeTheme(object sender, RoutedEventArgs e)
        {
            switch (int.Parse(((MenuItem)sender).Uid))
            {
                case 0: ThemesController.SetTheme(ThemesController.ThemeTypes.Light); break;
                case 1: ThemesController.SetTheme(ThemesController.ThemeTypes.ColourfulLight); break;
                case 2: ThemesController.SetTheme(ThemesController.ThemeTypes.Dark); break;
                case 3: ThemesController.SetTheme(ThemesController.ThemeTypes.ColourfulDark); break;
            }
            e.Handled = true;
        }

        private void Timer_Click(object sender, EventArgs e)
        {
            String d;
            d = DateTime.Now.ToString("hh:mm:ss tt");
            Clock.Content = d;
        }

        // This looks really messsy... Fix this if time allows
        private void SiteCheck(object sender, EventArgs e)
        {
            // Do not do checking if in break time
            if (!BreakTime)
            {
                foreach (var Browser in BrowserList.Items)
                {
                    IEnumerable<String> tabs = _getCurrentTab(Browser.ToString());
                    if (tabs != Enumerable.Empty<string>())
                    {
                        foreach (string tab in tabs)
                        {
                            foreach (var Site in SiteList.Items)
                            {
                                string[] subs = Site.ToString().Split(':');
                                string SiteTrim = subs[1].Trim();
                                if (tab.Contains(SiteTrim))
                                {
                                    _RaiseMessage("こら~~　作業に集中しなさい!!", true, @"koiduka2.wav");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AlarmCheck(object sender, EventArgs e)
        {
            if (Started == true) 
            {
                string ClockNow = DateTime.Now.ToString("HH:mm:00");
                foreach (var Alarm in AlarmList.Items)
                {
                    if (ClockNow == Alarm.ToString())
                    {
                        _RaiseMessage("こら~~  作業の時間ですよ~~~~", false, "");
                        BreakTime = false;
                        BreakStatus.Content = "現在のステイタス: 作業中";
                        
                    }
                }
                AlarmList.Items.Remove(ClockNow);
            }
        }

        private void BreakCheck(object sender, EventArgs e)
        {
            if (Started == true && BreakList.Items.Count != 0)
            {
                string ClockNow = DateTime.Now.ToString("HH:mm:00");
                foreach (var Break in BreakList.Items)
                {
                    
                    if (ClockNow == Break.ToString())
                    {
                        _RaiseMessage("こら~~ 休憩しなさい~~~~", false, "");
                        BreakTime = true;
                        BreakStatus.Content = "現在のステイタス: 休憩中";
                    }
                }
                BreakList.Items.Remove(ClockNow);
            }
        }

        private void BrowserAddOnClick(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(BrowserInput.Text))
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = BrowserInput.Text;
                BrowserList.Items.Add(itm);
                BrowserInput.Text = "";
            }
        }

        private void BrowserDelOnClick(object sender, RoutedEventArgs e)
        {
            if (BrowserList.SelectedIndex != -1)
            {
                BrowserList.Items.RemoveAt(BrowserList.SelectedIndex);
            }
        }

        private void SiteAddOnClick(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(SiteInput.Text))
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = SiteInput.Text;
                SiteList.Items.Add(itm);
                SiteInput.Text = "";
            }
        }

        private void SiteOnClick(object sender, RoutedEventArgs e)
        {
            if (SiteList.SelectedIndex != -1)
            {
                SiteList.Items.RemoveAt(SiteList.SelectedIndex);
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Started = true;
            BreakTime = false;
            StartStatus.Content = "アプリステイタス: スタート中";
            BreakStatus.Content = "現在のステイタス: 作業中";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Started = false;
            BreakTime = true;
            StartStatus.Content = "アプリステイタス: ストップ中";
            BreakStatus.Content = "現在のステイタス: 休憩中";
        }


        private void AlarmAdd_Click(object sender, RoutedEventArgs e)
        {
            DateTime time = AlarmClock.Time;
            string t = time.ToString("HH:mm:ss");
            AlarmList.Items.Add(t);
        }

        private void AlarmDel_Click(object sender, RoutedEventArgs e)
        {
            if (AlarmList.SelectedIndex != -1)
            {
                AlarmList.Items.RemoveAt(AlarmList.SelectedIndex);
            }
        }

        private void BreakAdd_Click(object sender, RoutedEventArgs e)
        {
            DateTime time = BreakClock.Time;
            string t = time.ToString("HH:mm:ss");
            BreakList.Items.Add(t);
        }

        private void BreakDel_Click(object sender, RoutedEventArgs e)
        {
            if (BreakList.SelectedIndex != -1)
            {
                BreakList.Items.RemoveAt(BreakList.SelectedIndex);
            }
        }
        private void PomoLabelChange(object sender, EventArgs e)
        {
            PomoWorktime = PomoWorktime - 0.5;
            double labelContent = Math.Floor(PomoWorktime);
            PomoLabel.Content = (TimeSpan.FromSeconds(labelContent)).ToString();
            if (PomoWorktime == 0)
            {
               
                Timer4.Stop();
                
                PomoModeStarted = false;
                PomoStart.Content = "Start";
                if (PomoMode)
                {
                    _RaiseMessage("こら~~ 休憩しなさい~~~~", false, "");
                    PomoWorktime = PomoBreakTime;
                    PomoLabel.Content = TimeSpan.FromSeconds(PomoBreakTime).ToString();
                    Stop_Click(null, null);
                }
                else
                {
                    _RaiseMessage("こら~~  作業の時間ですよ~~~~", false, "");
                    PomoWorktime = PomoOfficeTime;
                    PomoLabel.Content = TimeSpan.FromSeconds(PomoOfficeTime).ToString();
                }
                PomoMode = !PomoMode; // switch mode
            }
        }
        private void PomoStart_Click(object sender, RoutedEventArgs e)
        {
            // Stop timer if it has already been started
            if (PomoModeStarted)
            {
                Timer4.Stop();
                Stop_Click(null, null);
                PomoStart.Content = "Start";
                PomoModeStarted = false;
                return;
            }
           
            if (PomoMode){
                Timer4.Start();
                Start_Click(null, null);
                PomoModeStarted = true;
                PomoStart.Content = "Stop";
            }
            else
            {
                Timer4.Start();
                PomoModeStarted = true;
                PomoStart.Content = "Stop";
            }

           
        }


    }
}
