using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace syosetuDownloader
{
    public partial class RenameForm : Form
    {
        public string DlFolder { get; set; }
        public string CurrNovelFolder { get => Path.Combine(DlFolder, textBox1.Text); }

        List<Char> _invalidChars = new List<Char>() { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public RenameForm(string dlfolder, string title)
        {
            InitializeComponent();

            DlFolder = dlfolder;
            textBox1.Text = title;
            textBox2.Text = title;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            ValidateInput();
        }

        public void ValidateInput()
        {
            textBox3.Text = Path.Combine(DlFolder, textBox2.Text);

            if (string.IsNullOrEmpty(textBox2.Text))
            {
                lblError.Text = "";
                btnOK.Enabled = false;
                return;
            }

            if (_invalidChars.Any(character => textBox2.Text.Contains(character)))
            {
                lblError.Text = "A folder name can't contain any of the following characters:  \\ / : * ? \" < > |";
                btnOK.Enabled = false;
                return;
            }

            if (Directory.Exists(textBox3.Text))
            {
                lblError.Text = "Folder already exists";
                btnOK.Enabled = false;
                return;
            }

            lblError.Text = "";
            btnOK.Enabled = true;
        }
    }
}
