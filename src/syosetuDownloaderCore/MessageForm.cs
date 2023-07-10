using System;
using System.IO;
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
            if (!Visible)
            {
                Show();
                BringToFront();
            }
            // add all lines
            int count = 0;
            using (var reader = new StringReader(message))
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    count++;
                    listboxLog.Items.Add(line);
                }
            // scroll to bottom
            listboxLog.SelectionMode = SelectionMode.One;
            listboxLog.TopIndex = listboxLog.Items.Count - 1;
            listboxLog.SelectedIndex = listboxLog.Items.Count - 1;
            listboxLog.SelectionMode = SelectionMode.MultiExtended;
            // select added lines
            for (int i = 0; i < listboxLog.Items.Count; i++)
                if (i >= listboxLog.Items.Count - count)
                    listboxLog.SetSelected(i, true);
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
                    string s = "";
                    foreach (var item in listboxLog.SelectedItems) s += item.ToString() + Environment.NewLine;
                    Clipboard.SetData(DataFormats.StringFormat, s.Trim(Environment.NewLine.ToCharArray()));
                }
            }
        }
    }
}
