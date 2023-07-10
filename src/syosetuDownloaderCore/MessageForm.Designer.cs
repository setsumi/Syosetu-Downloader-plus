namespace syosetuDownloaderCore
{
    partial class MessageForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MessageForm));
            this.listboxLog = new System.Windows.Forms.ListBox();
            this.timerBeep = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // listboxLog
            // 
            this.listboxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listboxLog.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listboxLog.FormattingEnabled = true;
            this.listboxLog.ItemHeight = 16;
            this.listboxLog.Items.AddRange(new object[] {
            "sample 1",
            "sample 2",
            "sample 3",
            "sample 4",
            "sample 5",
            "sample 6",
            "sample 7",
            "sample 8",
            "sample 9",
            "sample 10",
            "sample 11",
            "sample 12",
            "sample 13",
            "sample 14",
            "sample 15"});
            this.listboxLog.Location = new System.Drawing.Point(0, 0);
            this.listboxLog.Name = "listboxLog";
            this.listboxLog.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listboxLog.Size = new System.Drawing.Size(744, 246);
            this.listboxLog.TabIndex = 0;
            // 
            // timerBeep
            // 
            this.timerBeep.Interval = 500;
            this.timerBeep.Tick += new System.EventHandler(this.timerBeep_Tick);
            // 
            // MessageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(744, 246);
            this.Controls.Add(this.listboxLog);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "MessageForm";
            this.Text = "Messages   (Ctrl+C - copy)";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MessageForm_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MessageForm_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listboxLog;
        private System.Windows.Forms.Timer timerBeep;
    }
}