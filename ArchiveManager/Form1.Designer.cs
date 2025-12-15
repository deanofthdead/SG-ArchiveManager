// Form1.Designer.cs
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ArchiveManager
{
    partial class Form1
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabArchive = new System.Windows.Forms.TabPage();
            this.progressArchive = new System.Windows.Forms.ProgressBar();
            this.lblArchiveEta = new System.Windows.Forms.Label();
            this.txtArchiveLog = new System.Windows.Forms.TextBox();
            this.btnStartUpload = new System.Windows.Forms.Button();
            this.txtArchiveName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnBrowseSource = new System.Windows.Forms.Button();
            this.txtSourcePath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabBrowse = new System.Windows.Forms.TabPage();
            this.progressRestore = new System.Windows.Forms.ProgressBar();
            this.lblRestoreEta = new System.Windows.Forms.Label();
            this.txtRestoreLog = new System.Windows.Forms.TextBox();
            this.splitBrowse = new System.Windows.Forms.SplitContainer();
            this.treeS3 = new System.Windows.Forms.TreeView();
            this.gridFiles = new System.Windows.Forms.DataGridView();
            this.lblBrowsePath = new System.Windows.Forms.Label();
            this.btnBrowseUp = new System.Windows.Forms.Button();
            this.btnRestoreFolder = new System.Windows.Forms.Button();
            this.btnRestoreSelectedFile = new System.Windows.Forms.Button();
            this.btnExportCsv = new System.Windows.Forms.Button();
            this.btnBrowseRefresh = new System.Windows.Forms.Button();
            this.gridArchives = new System.Windows.Forms.DataGridView();
            this.tabControlMain.SuspendLayout();
            this.tabArchive.SuspendLayout();
            this.tabBrowse.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitBrowse)).BeginInit();
            this.splitBrowse.Panel1.SuspendLayout();
            this.splitBrowse.Panel2.SuspendLayout();
            this.splitBrowse.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridFiles)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridArchives)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabArchive);
            this.tabControlMain.Controls.Add(this.tabBrowse);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1184, 661);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabArchive
            // 
            this.tabArchive.Controls.Add(this.progressArchive);
            this.tabArchive.Controls.Add(this.lblArchiveEta);
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
            this.tabArchive.Size = new System.Drawing.Size(1176, 633);
            this.tabArchive.TabIndex = 0;
            this.tabArchive.Text = "Archive";
            this.tabArchive.UseVisualStyleBackColor = true;
            // 
            // progressArchive
            // 
            this.progressArchive.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressArchive.Location = new System.Drawing.Point(16, 602);
            this.progressArchive.Name = "progressArchive";
            this.progressArchive.Size = new System.Drawing.Size(1144, 20);
            this.progressArchive.TabIndex = 8;
            // 
            // lblArchiveEta
            // 
            this.lblArchiveEta.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblArchiveEta.AutoSize = true;
            this.lblArchiveEta.Location = new System.Drawing.Point(16, 582);
            this.lblArchiveEta.Name = "lblArchiveEta";
            this.lblArchiveEta.Size = new System.Drawing.Size(0, 15);
            this.lblArchiveEta.TabIndex = 7;
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
            this.txtArchiveLog.Size = new System.Drawing.Size(1144, 440);
            this.txtArchiveLog.TabIndex = 6;
            // 
            // btnStartUpload
            // 
            this.btnStartUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartUpload.Location = new System.Drawing.Point(1055, 94);
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
            this.txtArchiveName.Size = new System.Drawing.Size(923, 23);
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
            this.btnBrowseSource.Location = new System.Drawing.Point(1055, 43);
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
            this.txtSourcePath.Size = new System.Drawing.Size(923, 23);
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
            // tabBrowse
            // 
            this.tabBrowse.Controls.Add(this.progressRestore);
            this.tabBrowse.Controls.Add(this.lblRestoreEta);
            this.tabBrowse.Controls.Add(this.txtRestoreLog);
            this.tabBrowse.Controls.Add(this.splitBrowse);
            this.tabBrowse.Controls.Add(this.lblBrowsePath);
            this.tabBrowse.Controls.Add(this.btnBrowseUp);
            this.tabBrowse.Controls.Add(this.btnRestoreFolder);
            this.tabBrowse.Controls.Add(this.btnRestoreSelectedFile);
            this.tabBrowse.Controls.Add(this.btnExportCsv);
            this.tabBrowse.Controls.Add(this.btnBrowseRefresh);
            this.tabBrowse.Controls.Add(this.gridArchives);
            this.tabBrowse.Location = new System.Drawing.Point(4, 24);
            this.tabBrowse.Name = "tabBrowse";
            this.tabBrowse.Padding = new System.Windows.Forms.Padding(3);
            this.tabBrowse.Size = new System.Drawing.Size(1176, 633);
            this.tabBrowse.TabIndex = 1;
            this.tabBrowse.Text = "Browse";
            this.tabBrowse.UseVisualStyleBackColor = true;
            // 
            // progressRestore
            // 
            this.progressRestore.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressRestore.Location = new System.Drawing.Point(16, 602);
            this.progressRestore.Name = "progressRestore";
            this.progressRestore.Size = new System.Drawing.Size(1144, 20);
            this.progressRestore.TabIndex = 10;
            // 
            // lblRestoreEta
            // 
            this.lblRestoreEta.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRestoreEta.AutoSize = true;
            this.lblRestoreEta.Location = new System.Drawing.Point(16, 582);
            this.lblRestoreEta.Name = "lblRestoreEta";
            this.lblRestoreEta.Size = new System.Drawing.Size(0, 15);
            this.lblRestoreEta.TabIndex = 9;
            // 
            // txtRestoreLog
            // 
            this.txtRestoreLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRestoreLog.Location = new System.Drawing.Point(16, 499);
            this.txtRestoreLog.Multiline = true;
            this.txtRestoreLog.Name = "txtRestoreLog";
            this.txtRestoreLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtRestoreLog.Size = new System.Drawing.Size(1144, 73);
            this.txtRestoreLog.TabIndex = 8;
            // 
            // splitBrowse
            // 
            this.splitBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitBrowse.Location = new System.Drawing.Point(16, 214);
            this.splitBrowse.Name = "splitBrowse";
            // 
            // splitBrowse.Panel1
            // 
            this.splitBrowse.Panel1.Controls.Add(this.treeS3);
            // 
            // splitBrowse.Panel2
            // 
            this.splitBrowse.Panel2.Controls.Add(this.gridFiles);
            this.splitBrowse.Size = new System.Drawing.Size(1144, 279);
            this.splitBrowse.SplitterDistance = 320;
            this.splitBrowse.TabIndex = 7;
            // 
            // treeS3
            // 
            this.treeS3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeS3.Location = new System.Drawing.Point(0, 0);
            this.treeS3.Name = "treeS3";
            this.treeS3.Size = new System.Drawing.Size(320, 279);
            this.treeS3.TabIndex = 0;
            this.treeS3.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeS3_BeforeExpand);
            this.treeS3.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeS3_AfterSelect);
            // 
            // gridFiles
            // 
            this.gridFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridFiles.Location = new System.Drawing.Point(0, 0);
            this.gridFiles.Name = "gridFiles";
            this.gridFiles.RowTemplate.Height = 25;
            this.gridFiles.Size = new System.Drawing.Size(820, 279);
            this.gridFiles.TabIndex = 0;
            // 
            // lblBrowsePath
            // 
            this.lblBrowsePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBrowsePath.AutoEllipsis = true;
            this.lblBrowsePath.Location = new System.Drawing.Point(16, 186);
            this.lblBrowsePath.Name = "lblBrowsePath";
            this.lblBrowsePath.Size = new System.Drawing.Size(1144, 20);
            this.lblBrowsePath.TabIndex = 6;
            this.lblBrowsePath.Text = "Select an archive to browse...";
            // 
            // btnBrowseUp
            // 
            this.btnBrowseUp.Location = new System.Drawing.Point(16, 149);
            this.btnBrowseUp.Name = "btnBrowseUp";
            this.btnBrowseUp.Size = new System.Drawing.Size(105, 27);
            this.btnBrowseUp.TabIndex = 5;
            this.btnBrowseUp.Text = "Up";
            this.btnBrowseUp.UseVisualStyleBackColor = true;
            this.btnBrowseUp.Click += new System.EventHandler(this.btnBrowseUp_Click);
            // 
            // btnRestoreFolder
            // 
            this.btnRestoreFolder.Location = new System.Drawing.Point(238, 149);
            this.btnRestoreFolder.Name = "btnRestoreFolder";
            this.btnRestoreFolder.Size = new System.Drawing.Size(140, 27);
            this.btnRestoreFolder.TabIndex = 4;
            this.btnRestoreFolder.Text = "Restore Folder...";
            this.btnRestoreFolder.UseVisualStyleBackColor = true;
            this.btnRestoreFolder.Click += new System.EventHandler(this.btnRestoreFolder_Click);
            // 
            // btnRestoreSelectedFile
            // 
            this.btnRestoreSelectedFile.Location = new System.Drawing.Point(384, 149);
            this.btnRestoreSelectedFile.Name = "btnRestoreSelectedFile";
            this.btnRestoreSelectedFile.Size = new System.Drawing.Size(170, 27);
            this.btnRestoreSelectedFile.TabIndex = 3;
            this.btnRestoreSelectedFile.Text = "Restore Selected File...";
            this.btnRestoreSelectedFile.UseVisualStyleBackColor = true;
            this.btnRestoreSelectedFile.Click += new System.EventHandler(this.btnRestoreSelectedFile_Click);
            // 
            // btnExportCsv
            // 
            this.btnExportCsv.Location = new System.Drawing.Point(127, 149);
            this.btnExportCsv.Name = "btnExportCsv";
            this.btnExportCsv.Size = new System.Drawing.Size(105, 27);
            this.btnExportCsv.TabIndex = 2;
            this.btnExportCsv.Text = "Export CSV";
            this.btnExportCsv.UseVisualStyleBackColor = true;
            this.btnExportCsv.Click += new System.EventHandler(this.btnExportCsv_Click);
            // 
            // btnBrowseRefresh
            // 
            this.btnBrowseRefresh.Location = new System.Drawing.Point(16, 116);
            this.btnBrowseRefresh.Name = "btnBrowseRefresh";
            this.btnBrowseRefresh.Size = new System.Drawing.Size(105, 27);
            this.btnBrowseRefresh.TabIndex = 1;
            this.btnBrowseRefresh.Text = "List Archives";
            this.btnBrowseRefresh.UseVisualStyleBackColor = true;
            this.btnBrowseRefresh.Click += new System.EventHandler(this.btnBrowseRefresh_Click);
            // 
            // gridArchives
            // 
            this.gridArchives.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridArchives.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridArchives.Location = new System.Drawing.Point(16, 16);
            this.gridArchives.Name = "gridArchives";
            this.gridArchives.RowTemplate.Height = 25;
            this.gridArchives.Size = new System.Drawing.Size(1144, 94);
            this.gridArchives.TabIndex = 0;
            this.gridArchives.SelectionChanged += new System.EventHandler(this.gridArchives_SelectionChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 661);
            this.Controls.Add(this.tabControlMain);
            this.Name = "Form1";
            this.Text = "SG Archive Manager";
            this.tabControlMain.ResumeLayout(false);
            this.tabArchive.ResumeLayout(false);
            this.tabArchive.PerformLayout();
            this.tabBrowse.ResumeLayout(false);
            this.tabBrowse.PerformLayout();
            this.splitBrowse.Panel1.ResumeLayout(false);
            this.splitBrowse.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitBrowse)).EndInit();
            this.splitBrowse.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridFiles)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridArchives)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TabControl tabControlMain;
        private TabPage tabArchive;
        private ProgressBar progressArchive;
        private Label lblArchiveEta;
        private TextBox txtArchiveLog;
        private Button btnStartUpload;
        private TextBox txtArchiveName;
        private Label label2;
        private Button btnBrowseSource;
        private TextBox txtSourcePath;
        private Label label1;

        private TabPage tabBrowse;
        private DataGridView gridArchives;
        private Button btnBrowseRefresh;
        private Button btnExportCsv;
        private Button btnRestoreSelectedFile;
        private Button btnRestoreFolder;
        private Button btnBrowseUp;
        private Label lblBrowsePath;
        private SplitContainer splitBrowse;
        private TreeView treeS3;
        private DataGridView gridFiles;

        private TextBox txtRestoreLog;
        private Label lblRestoreEta;
        private ProgressBar progressRestore;
    }
}