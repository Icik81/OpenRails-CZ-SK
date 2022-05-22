using System;
using System.Windows.Forms;

namespace ORTS
{
    public partial class SplashWindow : Form
    {
        public int Progress = 0;
        public string Message = "Aktualizace databáze Mirelu. Zabere to jen pár vteřin..";
        public SplashWindow()
        {
            InitializeComponent();
        }

        private void SplashWindow_Load(object sender, EventArgs e)
        {

        }

        public void UpdateProgress()
        {
            if (Progress > 100) Progress = 100;
            this.Refresh();
            progressBar.Refresh();
            progressBar.Value = Progress;
            progressBar.Update();
            this.Refresh();
            progressBar.Refresh();
            label1.Text = Message;
            label1.Refresh();
        }
    }
}
