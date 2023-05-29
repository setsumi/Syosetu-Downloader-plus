using System;
using System.Windows.Forms;

namespace syosetuDownloaderCore
{
    public partial class MessageForm : Form
    {
        public MessageForm()
        {
            InitializeComponent();

            listboxLog.Items.Clear();
        }

        public void Error(string message)
        {
            Message(message);

            if (!timerBeep.Enabled)
            {
                timerBeep.Start();
                System.Media.SystemSounds.Exclamation.Play();
            }
        }

        private void timerBeep_Tick(object sender, EventArgs e)
        {
            timerBeep.Stop();
        }

        private void Message(string message)
        {
            listboxLog.Items.Add(message);
            //listboxLog.TopIndex = listboxLog.Items.Count - 1;
            listboxLog.SelectedIndex = listboxLog.Items.Count - 1;

            if (!Visible)
            {
                Show();
                BringToFront();
            }
        }

        private void MessageForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true; // Cancel the closure
            Hide(); // Hide the window
        }

        private void MessageForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control == true && e.KeyCode == Keys.C)
            {
                if (listboxLog.SelectedItems.Count > 0)
                {
                    string s = listboxLog.SelectedItem.ToString();
                    Clipboard.SetData(DataFormats.StringFormat, s);
                }
            }
        }
    }
}
