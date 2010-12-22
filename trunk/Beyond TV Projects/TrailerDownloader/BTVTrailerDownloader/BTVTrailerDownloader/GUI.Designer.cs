namespace BTVTrailerDownloader
{
    partial class GUI
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.formatBox = new System.Windows.Forms.ComboBox();
            this.statusWindow = new System.Windows.Forms.TextBox();
            this.scanButton = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.verboseStatus = new System.Windows.Forms.CheckBox();
            this.eraseDB = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.numToGetCombo = new System.Windows.Forms.ComboBox();
            this.getFoldersButton = new System.Windows.Forms.Button();
            this.editDBButton = new System.Windows.Forms.Button();
            this.exitButton = new System.Windows.Forms.Button();
            this.runOnStart = new System.Windows.Forms.CheckBox();
            this.locationBox = new System.Windows.Forms.ComboBox();
            this.passwordBox = new System.Windows.Forms.TextBox();
            this.usernameBox = new System.Windows.Forms.TextBox();
            this.portBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Port";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Username";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 80);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Password";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 109);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(39, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Format";
            // 
            // formatBox
            // 
            this.formatBox.FormattingEnabled = true;
            this.formatBox.Items.AddRange(new object[] {
            "480p",
            "720p",
            "1080p"});
            this.formatBox.Location = new System.Drawing.Point(79, 109);
            this.formatBox.Name = "formatBox";
            this.formatBox.Size = new System.Drawing.Size(121, 21);
            this.formatBox.TabIndex = 7;
            this.formatBox.Text = "480p";
            // 
            // statusWindow
            // 
            this.statusWindow.Location = new System.Drawing.Point(18, 188);
            this.statusWindow.Multiline = true;
            this.statusWindow.Name = "statusWindow";
            this.statusWindow.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.statusWindow.Size = new System.Drawing.Size(336, 115);
            this.statusWindow.TabIndex = 11;
            this.statusWindow.Text = "Waiting for logon...";
            // 
            // scanButton
            // 
            this.scanButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.scanButton.AutoSize = true;
            this.scanButton.Enabled = false;
            this.scanButton.Location = new System.Drawing.Point(15, 161);
            this.scanButton.Name = "scanButton";
            this.scanButton.Size = new System.Drawing.Size(42, 23);
            this.scanButton.TabIndex = 12;
            this.scanButton.Text = "Scan";
            this.scanButton.UseVisualStyleBackColor = true;
            this.scanButton.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 139);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(48, 13);
            this.label5.TabIndex = 13;
            this.label5.Text = "Location";
            // 
            // verboseStatus
            // 
            this.verboseStatus.AutoSize = true;
            this.verboseStatus.Checked = global::BTVTrailerDownloader.Properties.Settings.Default.verbose;
            this.verboseStatus.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::BTVTrailerDownloader.Properties.Settings.Default, "verbose", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.verboseStatus.Location = new System.Drawing.Point(256, 165);
            this.verboseStatus.Name = "verboseStatus";
            this.verboseStatus.Size = new System.Drawing.Size(98, 17);
            this.verboseStatus.TabIndex = 16;
            this.verboseStatus.Text = "Verbose Status";
            this.verboseStatus.UseVisualStyleBackColor = true;
            this.verboseStatus.CheckedChanged += new System.EventHandler(this.verboseStatus_CheckedChanged);
            // 
            // eraseDB
            // 
            this.eraseDB.Location = new System.Drawing.Point(278, 134);
            this.eraseDB.Name = "eraseDB";
            this.eraseDB.Size = new System.Drawing.Size(75, 23);
            this.eraseDB.TabIndex = 17;
            this.eraseDB.Text = "Clear History";
            this.eraseDB.UseVisualStyleBackColor = true;
            this.eraseDB.Click += new System.EventHandler(this.eraseDB_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(241, 63);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(113, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Download most recent";
            // 
            // numToGetCombo
            // 
            this.numToGetCombo.FormattingEnabled = true;
            this.numToGetCombo.Items.AddRange(new object[] {
            "All",
            "1",
            "5",
            "10",
            "15"});
            this.numToGetCombo.Location = new System.Drawing.Point(279, 79);
            this.numToGetCombo.Name = "numToGetCombo";
            this.numToGetCombo.Size = new System.Drawing.Size(75, 21);
            this.numToGetCombo.TabIndex = 19;
            this.numToGetCombo.Text = "All";
            // 
            // getFoldersButton
            // 
            this.getFoldersButton.Location = new System.Drawing.Point(125, 163);
            this.getFoldersButton.Name = "getFoldersButton";
            this.getFoldersButton.Size = new System.Drawing.Size(75, 23);
            this.getFoldersButton.TabIndex = 20;
            this.getFoldersButton.Text = "Get Folders";
            this.getFoldersButton.UseVisualStyleBackColor = true;
            this.getFoldersButton.Click += new System.EventHandler(this.getFoldersButton_Click);
            // 
            // editDBButton
            // 
            this.editDBButton.Location = new System.Drawing.Point(278, 107);
            this.editDBButton.Name = "editDBButton";
            this.editDBButton.Size = new System.Drawing.Size(75, 23);
            this.editDBButton.TabIndex = 21;
            this.editDBButton.Text = "Edit History";
            this.editDBButton.UseVisualStyleBackColor = true;
            this.editDBButton.Click += new System.EventHandler(this.editDBButton_Click);
            // 
            // exitButton
            // 
            this.exitButton.Location = new System.Drawing.Point(279, 310);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(75, 23);
            this.exitButton.TabIndex = 23;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // runOnStart
            // 
            this.runOnStart.AutoSize = true;
            this.runOnStart.Checked = global::BTVTrailerDownloader.Properties.Settings.Default.runOnStart;
            this.runOnStart.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::BTVTrailerDownloader.Properties.Settings.Default, "runOnStart", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.runOnStart.Location = new System.Drawing.Point(257, 19);
            this.runOnStart.Name = "runOnStart";
            this.runOnStart.Size = new System.Drawing.Size(97, 17);
            this.runOnStart.TabIndex = 22;
            this.runOnStart.Text = "Scan on Start?";
            this.runOnStart.UseVisualStyleBackColor = true;
            // 
            // locationBox
            // 
            this.locationBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::BTVTrailerDownloader.Properties.Settings.Default, "folderIndex", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.locationBox.Enabled = false;
            this.locationBox.FormattingEnabled = true;
            this.locationBox.Location = new System.Drawing.Point(79, 136);
            this.locationBox.Name = "locationBox";
            this.locationBox.Size = new System.Drawing.Size(121, 21);
            this.locationBox.TabIndex = 14;
            this.locationBox.Text = global::BTVTrailerDownloader.Properties.Settings.Default.folderIndex;
            // 
            // passwordBox
            // 
            this.passwordBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::BTVTrailerDownloader.Properties.Settings.Default, "password", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.passwordBox.Location = new System.Drawing.Point(79, 80);
            this.passwordBox.Name = "passwordBox";
            this.passwordBox.Size = new System.Drawing.Size(121, 20);
            this.passwordBox.TabIndex = 10;
            this.passwordBox.Text = global::BTVTrailerDownloader.Properties.Settings.Default.password;
            this.passwordBox.UseSystemPasswordChar = true;
            // 
            // usernameBox
            // 
            this.usernameBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::BTVTrailerDownloader.Properties.Settings.Default, "username", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.usernameBox.Location = new System.Drawing.Point(79, 50);
            this.usernameBox.Name = "usernameBox";
            this.usernameBox.Size = new System.Drawing.Size(121, 20);
            this.usernameBox.TabIndex = 9;
            this.usernameBox.Text = global::BTVTrailerDownloader.Properties.Settings.Default.username;
            // 
            // portBox
            // 
            this.portBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::BTVTrailerDownloader.Properties.Settings.Default, "port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.portBox.Location = new System.Drawing.Point(79, 17);
            this.portBox.Name = "portBox";
            this.portBox.Size = new System.Drawing.Size(121, 20);
            this.portBox.TabIndex = 1;
            this.portBox.Text = global::BTVTrailerDownloader.Properties.Settings.Default.port;
            // 
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(366, 340);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.runOnStart);
            this.Controls.Add(this.editDBButton);
            this.Controls.Add(this.getFoldersButton);
            this.Controls.Add(this.numToGetCombo);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.eraseDB);
            this.Controls.Add(this.verboseStatus);
            this.Controls.Add(this.locationBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.scanButton);
            this.Controls.Add(this.statusWindow);
            this.Controls.Add(this.passwordBox);
            this.Controls.Add(this.usernameBox);
            this.Controls.Add(this.formatBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.portBox);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.Name = "GUI";
            this.Text = "Beyond TV Trailer Downloader";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox portBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox formatBox;
        private System.Windows.Forms.TextBox usernameBox;
        private System.Windows.Forms.TextBox passwordBox;
        private System.Windows.Forms.TextBox statusWindow;
        private System.Windows.Forms.CheckBox scanButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox locationBox;
        private System.Windows.Forms.CheckBox verboseStatus;
        private System.Windows.Forms.Button eraseDB;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox numToGetCombo;
        private System.Windows.Forms.Button getFoldersButton;
        private System.Windows.Forms.Button editDBButton;
        private System.Windows.Forms.CheckBox runOnStart;
        private System.Windows.Forms.Button exitButton;
    }
}

