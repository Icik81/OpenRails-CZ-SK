using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using Ionic.Zip;

namespace ORTS
{
    public partial class Update : Form
    {
        public Update()
        {
            InitializeComponent();
        }

        Timer timer = new Timer();
        private void Update_Load(object sender, EventArgs e)
        {
            timer.Tick += Timer_Tick;
            timer.Interval = 10;
            timer.Start();
        }
        private bool updateRunning = false;
        private string state = "Kontrola aktualizací";
        private string dots = "";
        private bool waiting = false;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!updateRunning)
            {
                updateRunning = true;
                DoUpdate();
            }
            dots += ".";
            if (dots == "....")
                dots = "";
            label1.Text = waiting? state : state + dots;
            label1.Refresh();
        }

        public async void DoUpdate()
        {
            await System.Threading.Tasks.Task.Run(() => ProcessUpdate());
            Close();
        }

        public void ProcessUpdate()
        {
            string s = "";
            try
            {
                state = "Navazuji spojení";
                Ping ping = new Ping();
                PingReply pr = ping.Send("lkpr.aspone.cz");
                if (pr.Status != IPStatus.Success)
                {
                    waiting = true;
                    state = "Spojení nelze navázat";
                    System.Threading.Thread.Sleep(1000);
                    Close();
                    return;
                }
            }
            catch { Close(); return; }
            try
            {
                state = "Zjišťuji aktuální verzi";
                string versionPath = Application.StartupPath;
                versionPath = versionPath + "\\version14.ini";
                if (!File.Exists(versionPath))
                {
                    File.WriteAllText(versionPath, "1");
                }
                string version = File.ReadAllText(versionPath);
                WebClient webClient = new WebClient();
                s = webClient.DownloadString("http://lkpr.aspone.cz/or/version14.txt");
                if (version != s) // new version available
                {
                    state = "Stahuji novou verzi";
                    File.Delete(Application.StartupPath + "\\Update14.zip");
                    webClient.DownloadFile("http://lkpr.aspone.cz/or/update14.zip", Application.StartupPath + "\\Update14.zip");
                    state = "Rozbaluji archiv";
                    ZipFile zip = new ZipFile(Application.StartupPath + "\\Update14.zip");
                    zip.ExtractAll(Application.StartupPath, ExtractExistingFileAction.OverwriteSilently);
                    File.WriteAllText(versionPath, s);
                    waiting = true;
                    state = s;
                    System.Threading.Thread.Sleep(2500);
                }
                else
                {
                    waiting = true;
                    state = "Verze je aktuální";
                    System.Threading.Thread.Sleep(2000);
                }
            }
            catch (Exception ex) { MessageBox.Show("Chyba aktualizace." + Environment.NewLine + ex.Message, "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); }
        }
    }
}
