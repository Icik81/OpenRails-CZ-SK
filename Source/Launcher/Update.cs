using Ionic.Zip;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;

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
            label1.Text = waiting ? state : state + dots;
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
            string conts = "";
            try
            {
                state = "Navazuji spojení";
                Ping ping = new Ping();
                PingReply pr = ping.Send("msts-rw.cz");
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
                versionPath = versionPath + "\\version16.ini";
                if (!File.Exists(versionPath))
                {
                    File.WriteAllText(versionPath, "1");
                }
                string version = File.ReadAllText(versionPath);

                string contentPath = Application.StartupPath + "\\content16.ini";
                if (!File.Exists(contentPath))
                {
                    File.WriteAllText(contentPath, "0");
                }
                string content = File.ReadAllText(contentPath);

                WebClient webClient = new WebClient();
                s = webClient.DownloadString("http://msts-rw.cz/ORIC/Updates/version16.txt");
                conts = webClient.DownloadString("http://msts-rw.cz/ORIC/Updates/Content16.txt");
                if (version != s || conts != content) // new version available
                {
                    if (version != s)
                    {
                        DialogResult dr = MessageBox.Show("Nalezena aktualizace. Chcete program aktualizovat?", "Aktualizace", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.No)
                            return;
                        state = "Stahuji novou verzi";
                        File.Delete(Application.StartupPath + "\\Update16.zip");
                        webClient.DownloadFile("http://msts-rw.cz/ORIC/Updates/update16.zip", Application.StartupPath + "\\Update16.zip");
                        state = "Rozbaluji archiv";
                        ZipFile zip = new ZipFile(Application.StartupPath + "\\Update16.zip");
                        zip.ExtractAll(Application.StartupPath, ExtractExistingFileAction.OverwriteSilently);
                        File.WriteAllText(versionPath, s);
                        waiting = true;
                        state = s;
                        System.Threading.Thread.Sleep(2500);
                    }
                    if (conts != content)
                    {
                        state = "Stahuji novou verzi obsahu";
                        File.Delete(Application.StartupPath + "\\Content16.zip");
                        webClient.DownloadFile("http://msts-rw.cz/ORIC/Updates/content16.zip", Application.StartupPath + "\\Content16.zip");
                        state = "Rozbaluji archiv";
                        ZipFile zip = new ZipFile(Application.StartupPath + "\\Content16.zip");
                        zip.ExtractAll(Application.StartupPath, ExtractExistingFileAction.OverwriteSilently);
                        File.WriteAllText(contentPath, conts);
                        waiting = true;
                        state = "Obsah byl aktualizován.";
                        System.Threading.Thread.Sleep(2500);
                    }
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
