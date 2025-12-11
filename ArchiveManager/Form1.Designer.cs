namespace ArchiveManager
{
    partial class Form1
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
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabArchive = new System.Windows.Forms.TabPage();
            this.txtArchiveLog = new System.Windows.Forms.TextBox();
            this.btnStartUpload = new System.Windows.Forms.Button();
            this.txtArchiveName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnBrowseSource = new System.Windows.Forms.Button();
            this.txtSourcePath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabRestore = new System.Windows.Forms.TabPage();
            this.txtRestoreLog = new System.Windows.Forms.TextBox();
            this.btnStartRestore = new System.Windows.Forms.Button();
            this.btnBrowseRestoreDest = new System.Windows.Forms.Button();
            this.txtRestoreDest = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnRefreshArchives = new System.Windows.Forms.Button();
            this.lstArchives = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tabBrowse = new System.Windows.Forms.TabPage();
            this.txtBrowseOutput = new System.Windows.Forms.TextBox();
            this.btnBrowseRefresh = new System.Windows.Forms.Button();
            this.tabControlMain.SuspendLayout();
            this.tabArchive.SuspendLayout();
            this.tabRestore.SuspendLayout();
            this.tabBrowse.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabArchive);
            this.tabControlMain.Controls.Add(this.tabRestore);
            this.tabControlMain.Controls.Add(this.tabBrowse);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(984, 561);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabArchive
            // 
            this.tabArchive.Controls.Add(this.txtArchiveLog);
            this.tabArchive.Controls.Add(this.btnStartUpload);
            this.tabArchive.Controls.Add(this.txtArchiveName);
            this.tabArchive.Controls.Add(this.label2);
            this.tabArchive.Controls.Add(this.btnBrowseSource);
            this.tabArchive.Controls.Add(this.txtSourcePath);
            this.tabArchive.Controls.Add(this.label1);
            this.tabArchive.Location = new System.Drawing.Point(4, 24);
            this.tabArchive.Name = "tabArchive";
            this.tabArchive.Padding = new System.Windows.Forms.Padding(3);
            this.tabArchive.Size = new System.Drawing.Size(976, 533);
            this.tabArchive.TabIndex = 0;
            this.tabArchive.Text = "Archive";
            this.tabArchive.UseVisualStyleBackColor = true;
            // 
            // txtArchiveLog
            // 
            this.txtArchiveLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtArchiveLog.Location = new System.Drawing.Point(16, 132);
            this.txtArchiveLog.Multiline = true;
            this.txtArchiveLog.Name = "txtArchiveLog";
            this.txtArchiveLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtArchiveLog.Size = new System.Drawing.Size(944, 383);
            this.txtArchiveLog.TabIndex = 6;
            // 
            // btnStartUpload
            // 
            this.btnStartUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartUpload.Location = new System.Drawing.Point(855, 94);
            this.btnStartUpload.Name = "btnStartUpload";
            this.btnStartUpload.Size = new System.Drawing.Size(105, 27);
            this.btnStartUpload.TabIndex = 5;
            this.btnStartUpload.Text = "Start Upload";
            this.btnStartUpload.UseVisualStyleBackColor = true;
            this.btnStartUpload.Click += new System.EventHandler(this.btnStartUpload_Click);
            // 
            // txtArchiveName
            // 
            this.txtArchiveName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtArchiveName.Location = new System.Drawing.Point(126, 96);
            this.txtArchiveName.Name = "txtArchiveName";
            this.txtArchiveName.Size = new System.Drawing.Size(723, 23);
            this.txtArchiveName.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 99);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "Archive name:";
            // 
            // btnBrowseSource
            // 
            this.btnBrowseSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseSource.Location = new System.Drawing.Point(855, 43);
            this.btnBrowseSource.Name = "btnBrowseSource";
            this.btnBrowseSource.Size = new System.Drawing.Size(105, 27);
            this.btnBrowseSource.TabIndex = 2;
            this.btnBrowseSource.Text = "Browse...";
            this.btnBrowseSource.UseVisualStyleBackColor = true;
            this.btnBrowseSource.Click += new System.EventHandler(this.btnBrowseSource_Click);
            // 
            // txtSourcePath
            // 
            this.txtSourcePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSourcePath.Location = new System.Drawing.Point(126, 46);
            this.txtSourcePath.Name = "txtSourcePath";
            this.txtSourcePath.Size = new System.Drawing.Size(723, 23);
            this.txtSourcePath.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Source path:";
            // 
            // tabRestore
            // 
            this.tabRestore.Controls.Add(this.txtRestoreLog);
            this.tabRestore.Controls.Add(this.btnStartRestore);
            this.tabRestore.Controls.Add(this.btnBrowseRestoreDest);
            this.tabRestore.Controls.Add(this.txtRestoreDest);
            this.tabRestore.Controls.Add(this.label4);
            this.tabRestore.Controls.Add(this.btnRefreshArchives);
            this.tabRestore.Controls.Add(this.lstArchives);
            this.tabRestore.Controls.Add(this.label3);
            this.tabRestore.Location = new System.Drawing.Point(4, 24);
            this.tabRestore.Name = "tabRestore";
            this.tabRestore.Padding = new System.Windows.Forms.Padding(3);
            this.tabRestore.Size = new System.Drawing.Size(976, 533);
            this.tabRestore.TabIndex = 1;
            this.tabRestore.Text = "Restore";
            this.tabRestore.UseVisualStyleBackColor = true;
            // 
            // txtRestoreLog
            // 
            this.txtRestoreLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRestoreLog.Location = new System.Drawing.Point(283, 132);
            this.txtRestoreLog.Multiline = true;
            this.txtRestoreLog.Name = "txtRestoreLog";
            this.txtRestoreLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtRestoreLog.Size = new System.Drawing.Size(677, 383);
            this.txtRestoreLog.TabIndex = 7;
            // 
            // btnStartRestore
            // 
            this.btnStartRestore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartRestore.Location = new System.Drawing.Point(855, 94);
            this.btnStartRestore.Name = "btnStartRestore";
            this.btnStartRestore.Size = new System.Drawing.Size(105, 27);
            this.btnStartRestore.TabIndex = 6;
            this.btnStartRestore.Text = "Start Restore";
            this.btnStartRestore.UseVisualStyleBackColor = true;
            this.btnStartRestore.Click += new System.EventHandler(this.btnStartRestore_Click);
            // 
            // btnBrowseRestoreDest
            // 
            this.btnBrowseRestoreDest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseRestoreDest.Location = new System.Drawing.Point(855, 43);
            this.btnBrowseRestoreDest.Name = "btnBrowseRestoreDest";
            this.btnBrowseRestoreDest.Size = new System.Drawing.Size(105, 27);
            this.btnBrowseRestoreDest.TabIndex = 5;
            this.btnBrowseRestoreDest.Text = "Browse...";
            this.btnBrowseRestoreDest.UseVisualStyleBackColor = true;
            this.btnBrowseRestoreDest.Click += new System.EventHandler(this.btnBrowseRestoreDest_Click);
            // 
            // txtRestoreDest
            // 
            this.txtRestoreDest.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRestoreDest.Location = new System.Drawing.Point(283, 46);
            this.txtRestoreDest.Name = "txtRestoreDest";
            this.txtRestoreDest.Size = new System.Drawing.Size(566, 23);
            this.txtRestoreDest.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(283, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(127, 15);
            this.label4.TabIndex = 3;
            this.label4.Text = "Local restore location:";
            // 
            // btnRefreshArchives
            // 
            this.btnRefreshArchives.Location = new System.Drawing.Point(16, 94);
            this.btnRefreshArchives.Name = "btnRefreshArchives";
            this.btnRefreshArchives.Size = new System.Drawing.Size(105, 27);
            this.btnRefreshArchives.TabIndex = 2;
            this.btnRefreshArchives.Text = "Refresh";
            this.btnRefreshArchives.UseVisualStyleBackColor = true;
            this.btnRefreshArchives.Click += new System.EventHandler(this.btnRefreshArchives_Click);
            // 
            // lstArchives
            // 
            this.lstArchives.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstArchives.FormattingEnabled = true;
            this.lstArchives.IntegralHeight = false;
            this.lstArchives.ItemHeight = 15;
            this.lstArchives.Location = new System.Drawing.Point(16, 132);
            this.lstArchives.Name = "lstArchives";
            this.lstArchives.Size = new System.Drawing.Size(249, 383);
            this.lstArchives.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(129, 15);
            this.label3.TabIndex = 0;
            this.label3.Text = "Available archive sets:";
            // 
            // tabBrowse
            // 
            this.tabBrowse.Controls.Add(this.txtBrowseOutput);
            this.tabBrowse.Controls.Add(this.btnBrowseRefresh);
            this.tabBrowse.Location = new System.Drawing.Point(4, 24);
            this.tabBrowse.Name = "tabBrowse";
            this.tabBrowse.Padding = new System.Windows.Forms.Padding(3);
            this.tabBrowse.Size = new System.Drawing.Size(976, 533);
            this.tabBrowse.TabIndex = 2;
            this.tabBrowse.Text = "Browse";
            this.tabBrowse.UseVisualStyleBackColor = true;
            // 
            // txtBrowseOutput
            // 
            this.txtBrowseOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBrowseOutput.Location = new System.Drawing.Point(16, 61);
            this.txtBrowseOutput.Multiline = true;
            this.txtBrowseOutput.Name = "txtBrowseOutput";
            this.txtBrowseOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtBrowseOutput.Size = new System.Drawing.Size(944, 454);
            this.txtBrowseOutput.TabIndex = 1;
            // 
            // btnBrowseRefresh
            // 
            this.btnBrowseRefresh.Location = new System.Drawing.Point(16, 18);
            this.btnBrowseRefresh.Name = "btnBrowseRefresh";
            this.btnBrowseRefresh.Size = new System.Drawing.Size(105, 27);
            this.btnBrowseRefresh.TabIndex = 0;
            this.btnBrowseRefresh.Text = "List Archives";
            this.btnBrowseRefresh.UseVisualStyleBackColor = true;
            this.btnBrowseRefresh.Click += new System.EventHandler(this.btnBrowseRefresh_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 561);
            this.Controls.Add(this.tabControlMain);
            this.Name = "Form1";
            this.Text = "SG Archive Manager";
            this.tabControlMain.ResumeLayout(false);
            this.tabArchive.ResumeLayout(false);
            this.tabArchive.PerformLayout();
            this.tabRestore.ResumeLayout(false);
            this.tabRestore.PerformLayout();
            this.tabBrowse.ResumeLayout(false);
            this.tabBrowse.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabArchive;
        private System.Windows.Forms.TabPage tabRestore;
        private System.Windows.Forms.TabPage tabBrowse;
        private System.Windows.Forms.TextBox txtArchiveLog;
        private System.Windows.Forms.Button btnStartUpload;
        private System.Windows.Forms.TextBox txtArchiveName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnBrowseSource;
        private System.Windows.Forms.TextBox txtSourcePath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtRestoreLog;
        private System.Windows.Forms.Button btnStartRestore;
        private System.Windows.Forms.Button btnBrowseRestoreDest;
        private System.Windows.Forms.TextBox txtRestoreDest;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnRefreshArchives;
        private System.Windows.Forms.ListBox lstArchives;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtBrowseOutput;
        private System.Windows.Forms.Button btnBrowseRefresh;
    }
}
