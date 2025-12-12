using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;

namespace ArchiveManager
{
    public partial class Form1 : Form
    {
        private const string BucketName = "sticksandglassarchive";

        // NOTE: You should rotate this webhook if it was ever shared outside your org.
        private const string GoogleChatWebhookUrl =
            "https://chat.googleapis.com/v1/spaces/AAAAPmYlk7E/messages?key=AIzaSyDdI0hCZtE6vySjMm-WEfRq3CPzqKqqsHI&token=nF_EfKaa76HFVfaLah_HXr6S4E_cnO-bvjxyITQ88rg";

        // Browse: archive table
        private readonly List<ArchiveInfo> _archiveStats = new();

        // Browse: current selection
        private string? _selectedArchiveName;
        private string _selectedRelativePrefix = ""; // within archive, e.g. "Media/CamA/"
        private readonly List<S3ObjectRow> _currentFiles = new();

        // Upload tracking
        private int _uploadTotalFiles;
        private int _uploadedFiles;
        private int _lastProgressLoggedAt;
        private DateTime? _uploadStartTime;

        public Form1()
        {
            InitializeComponent();
            ConfigureArchiveGrid();
            ConfigureFilesGrid();
            ResetBrowserUi();
        }

        // -----------------------
        // Archive tab (upload)
        // -----------------------

        private void btnBrowseSource_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the project folder to archive",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtSourcePath.Text = dialog.SelectedPath;

                if (string.IsNullOrWhiteSpace(txtArchiveName.Text))
                {
                    txtArchiveName.Text = Path.GetFileName(dialog.SelectedPath.TrimEnd(Path.DirectorySeparatorChar));
                }
            }
        }

        private async void btnStartUpload_Click(object sender, EventArgs e)
        {
            txtArchiveLog.Clear();
            lblArchiveEta.Text = "Preparing upload...";

            var sourcePath = txtSourcePath.Text.Trim();
            var archiveName = txtArchiveName.Text.Trim();

            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                AppendLog(txtArchiveLog, "ERROR: Source path does not exist.");
                lblArchiveEta.Text = "ERROR: Source path does not exist.";
                return;
            }

            if (string.IsNullOrWhiteSpace(archiveName))
            {
                archiveName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar));
                txtArchiveName.Text = archiveName;
            }

            var bucketPath = $"s3://{BucketName}/{archiveName}/";

            AppendLog(txtArchiveLog, $"Source:      {sourcePath}");
            AppendLog(txtArchiveLog, $"Destination: {bucketPath}");
            AppendLog(txtArchiveLog, "");

            AppendLog(txtArchiveLog, "Analysing source folder (counting files and total size)...");
            progressArchive.Style = ProgressBarStyle.Marquee;
            progressArchive.MarqueeAnimationSpeed = 30;

            (int fileCount, long totalBytes) analysis;
            try
            {
                analysis = await Task.Run(() => AnalyzeSourceFolder(sourcePath));
            }
            catch (Exception ex)
            {
                progressArchive.Style = ProgressBarStyle.Blocks;
                progressArchive.MarqueeAnimationSpeed = 0;
                AppendLog(txtArchiveLog, $"ERROR during analysis: {ex.Message}");
                lblArchiveEta.Text = $"ERROR during analysis: {ex.Message}";
                return;
            }

            _uploadTotalFiles = analysis.fileCount;
            _uploadedFiles = 0;
            _lastProgressLoggedAt = 0;
            _uploadStartTime = DateTime.UtcNow;

            progressArchive.Style = ProgressBarStyle.Blocks;
            progressArchive.MarqueeAnimationSpeed = 0;
            progressArchive.Minimum = 0;
            progressArchive.Maximum = _uploadTotalFiles == 0 ? 1 : _uploadTotalFiles;
            progressArchive.Value = 0;

            AppendLog(txtArchiveLog, $"Found {_uploadTotalFiles} files, total size {FormatBytes(analysis.totalBytes)}");
            AppendLog(txtArchiveLog, "");
            AppendLog(txtArchiveLog, "Starting upload...");
            AppendLog(txtArchiveLog, "");

            lblArchiveEta.Text = _uploadTotalFiles > 0
                ? $"Progress: 0/{_uploadTotalFiles} files (0.0%), ETA: calculating..."
                : "No files found to upload.";

            btnStartUpload.Enabled = false;

            try
            {
                var args = $"s3 sync \"{sourcePath}\" \"{bucketPath}\"";

                var exitCode = await RunAwsCommandAsync(args, line =>
                {
                    if (line != null)
                    {
                        AppendLog(txtArchiveLog, line);

                        var trimmed = line.TrimStart();
                        if (trimmed.StartsWith("upload:", StringComparison.OrdinalIgnoreCase))
                        {
                            NotifyFileUploaded();
                        }
                    }
                });

                AppendLog(txtArchiveLog, "");
                AppendLog(txtArchiveLog, $"aws s3 sync exit code: {exitCode}");

                if (_uploadTotalFiles > 0)
                {
                    progressArchive.Value = progressArchive.Maximum;
                    _uploadedFiles = _uploadTotalFiles;
                    NotifyFileUploaded(forceLog: true);
                }

                if (_uploadTotalFiles > 0 && _uploadStartTime.HasValue)
                {
                    var elapsed = DateTime.UtcNow - _uploadStartTime.Value;
                    lblArchiveEta.Text = exitCode == 0
                        ? $"Upload complete: {_uploadTotalFiles}/{_uploadTotalFiles} files (100%), elapsed {elapsed:hh\\:mm\\:ss}"
                        : $"Upload finished with errors (exit code {exitCode}). See log for details.";
                }

                // Google Chat notification
                if (exitCode == 0)
                {
                    var elapsed = _uploadStartTime.HasValue ? (DateTime.UtcNow - _uploadStartTime.Value) : (TimeSpan?)null;
                    var msg = $"✅ Archive '{archiveName}' is now complete in bucket '{BucketName}'.";
                    if (elapsed.HasValue) msg += $" Elapsed time: {elapsed.Value:hh\\:mm\\:ss}.";
                    _ = NotifyGoogleChatAsync(msg);
                }
                else
                {
                    _ = NotifyGoogleChatAsync($"⚠️ Archive '{archiveName}' upload finished with errors (exit code {exitCode}).");
                }
            }
            finally
            {
                btnStartUpload.Enabled = true;
                _uploadStartTime = null;
            }
        }

        private (int fileCount, long totalBytes) AnalyzeSourceFolder(string sourcePath)
        {
            int count = 0;
            long totalBytes = 0;

            foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                count++;
                try
                {
                    totalBytes += new FileInfo(file).Length;
                }
                catch { /* ignore */ }
            }

            return (count, totalBytes);
        }

        private void NotifyFileUploaded(bool forceLog = false)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(NotifyFileUploaded), forceLog);
                return;
            }

            if (_uploadTotalFiles <= 0) return;

            _uploadedFiles++;
            if (_uploadedFiles > progressArchive.Maximum) _uploadedFiles = progressArchive.Maximum;
            if (_uploadedFiles < 0) _uploadedFiles = 0;

            progressArchive.Value = _uploadedFiles;

            if (!_uploadStartTime.HasValue) return;

            double fraction = _uploadTotalFiles > 0 ? _uploadedFiles / (double)_uploadTotalFiles : 0d;
            if (fraction <= 0) return;

            var elapsed = DateTime.UtcNow - _uploadStartTime.Value;
            double totalSeconds = elapsed.TotalSeconds / fraction;
            var remaining = TimeSpan.FromSeconds(Math.Max(0, totalSeconds - elapsed.TotalSeconds));
            double percent = fraction * 100.0;

            lblArchiveEta.Text =
                $"Progress: {_uploadedFiles}/{_uploadTotalFiles} files ({percent:0.0}%), ETA {remaining:hh\\:mm\\:ss}";

            if (!forceLog && _uploadedFiles < _uploadTotalFiles && _uploadedFiles < _lastProgressLoggedAt + 10)
                return;

            _lastProgressLoggedAt = _uploadedFiles;

            AppendLog(txtArchiveLog,
                $"Progress: {_uploadedFiles}/{_uploadTotalFiles} files ({percent:0.0}%), ETA {remaining:hh\\:mm\\:ss}");
        }

        // -----------------------
        // Browse tab (Explorer)
        // -----------------------

        private void ConfigureArchiveGrid()
        {
            gridArchives.AllowUserToAddRows = false;
            gridArchives.AllowUserToDeleteRows = false;
            gridArchives.ReadOnly = true;
            gridArchives.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridArchives.MultiSelect = false;
            gridArchives.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            gridArchives.Columns.Clear();

            gridArchives.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ArchiveName",
                HeaderText = "Archive Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            gridArchives.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SizeHuman",
                HeaderText = "Size",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            gridArchives.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SizeBytes",
                HeaderText = "Size (bytes)",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            gridArchives.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ObjectCount",
                HeaderText = "Objects",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });
        }

        private void ConfigureFilesGrid()
        {
            gridFiles.AllowUserToAddRows = false;
            gridFiles.AllowUserToDeleteRows = false;
            gridFiles.ReadOnly = true;
            gridFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridFiles.MultiSelect = false;
            gridFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            gridFiles.Columns.Clear();

            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FileName",
                HeaderText = "Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SizeHuman",
                HeaderText = "Size",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LastModified",
                HeaderText = "Last Modified",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "S3Key",
                HeaderText = "S3 Key",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });
        }

        private void ResetBrowserUi()
        {
            lblBrowsePath.Text = "Select an archive to browse...";
            treeS3.Nodes.Clear();
            gridFiles.Rows.Clear();
            _currentFiles.Clear();
            _selectedArchiveName = null;
            _selectedRelativePrefix = "";
        }

        private async void btnBrowseRefresh_Click(object sender, EventArgs e)
        {
            _archiveStats.Clear();
            gridArchives.Rows.Clear();
            ResetBrowserUi();

            var listText = new StringBuilder();

            int exitCode = await RunAwsCommandAsync(
                $"s3 ls s3://{BucketName}/",
                line => { if (line != null) listText.AppendLine(line); });

            if (exitCode != 0)
            {
                MessageBox.Show(this, $"aws s3 ls exited with code {exitCode}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var lines = listText.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var archives = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("PRE "))
                {
                    archives.Add(trimmed.Substring(4).TrimEnd('/'));
                }
                else
                {
                    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var last = parts[^1];
                        if (last.EndsWith("/"))
                            archives.Add(last.TrimEnd('/'));
                    }
                }
            }

            foreach (var archive in archives)
            {
                var (totalBytes, totalObjects) = await GetArchiveStatsAsync(archive);

                var info = new ArchiveInfo
                {
                    Name = archive,
                    SizeBytes = totalBytes,
                    ObjectCount = totalObjects
                };
                _archiveStats.Add(info);

                gridArchives.Rows.Add(info.Name, FormatBytes(info.SizeBytes), info.SizeBytes, info.ObjectCount >= 0 ? info.ObjectCount : 0);
            }

            if (archives.Count == 0)
            {
                MessageBox.Show(this, "No archives found at top level of bucket.", "Browse",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (_archiveStats.Count == 0)
            {
                MessageBox.Show(this, "No archive data to export. Click 'List Archives' first.", "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Export archive list to CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"archive-list-{DateTime.Now:yyyy-MM-dd_HHmmss}.csv"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
                writer.WriteLine("ArchiveName,SizeBytes,ObjectCount,SizeHuman");

                foreach (var info in _archiveStats)
                {
                    string sizeHuman = FormatBytes(info.SizeBytes);
                    string nameEscaped = info.Name.Replace("\"", "\"\"");
                    writer.WriteLine($"\"{nameEscaped}\",{info.SizeBytes},{info.ObjectCount},{sizeHuman}");
                }

                MessageBox.Show(this, "CSV export complete.", "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to export CSV: {ex.Message}", "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void gridArchives_SelectionChanged(object sender, EventArgs e)
        {
            if (gridArchives.SelectedRows.Count == 0)
                return;

            var row = gridArchives.SelectedRows[0];
            var archiveObj = row.Cells["ArchiveName"].Value;
            var archiveName = archiveObj as string ?? archiveObj?.ToString();

            if (string.IsNullOrWhiteSpace(archiveName))
                return;

            _selectedArchiveName = archiveName;
            _selectedRelativePrefix = "";

            treeS3.Nodes.Clear();
            gridFiles.Rows.Clear();
            _currentFiles.Clear();

            // Root node = archive
            var root = new TreeNode(archiveName)
            {
                Tag = "" // relative prefix
            };
            root.Nodes.Add(new TreeNode("Loading...") { Tag = "__placeholder__" }); // lazy
            treeS3.Nodes.Add(root);
            root.Expand();

            lblBrowsePath.Text = $"s3://{BucketName}/{archiveName}/";

            // Also load root listing into files grid
            await LoadPrefixIntoFilesGridAsync(archiveName, "");
        }

        private async void treeS3_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (_selectedArchiveName == null)
                return;

            // If node has placeholder child, load actual children
            if (e.Node.Nodes.Count == 1 && (e.Node.Nodes[0].Tag as string) == "__placeholder__")
            {
                e.Node.Nodes.Clear();

                string relPrefix = e.Node.Tag as string ?? "";
                var folders = await ListFoldersAsync(_selectedArchiveName, relPrefix);

                foreach (var f in folders)
                {
                    var child = new TreeNode(f.DisplayName)
                    {
                        Tag = f.RelativePrefix
                    };

                    // Add placeholder to enable lazy expand
                    child.Nodes.Add(new TreeNode("Loading...") { Tag = "__placeholder__" });
                    e.Node.Nodes.Add(child);
                }
            }
        }

        private async void treeS3_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_selectedArchiveName == null)
                return;

            string relPrefix = e.Node.Tag as string ?? "";
            _selectedRelativePrefix = relPrefix;

            lblBrowsePath.Text = $"s3://{BucketName}/{_selectedArchiveName}/{relPrefix}";
            await LoadPrefixIntoFilesGridAsync(_selectedArchiveName, relPrefix);
        }

        private void btnBrowseUp_Click(object sender, EventArgs e)
        {
            if (treeS3.SelectedNode == null) return;
            var node = treeS3.SelectedNode;
            if (node.Parent != null)
            {
                treeS3.SelectedNode = node.Parent;
            }
        }

        private async Task LoadPrefixIntoFilesGridAsync(string archiveName, string relativePrefix)
        {
            gridFiles.Rows.Clear();
            _currentFiles.Clear();

            var objects = await ListFilesAsync(archiveName, relativePrefix);

            foreach (var o in objects)
            {
                _currentFiles.Add(o);

                gridFiles.Rows.Add(
                    o.FileName,
                    FormatBytes(o.SizeBytes),
                    o.LastModifiedText,
                    o.Key
                );
            }
        }

        private async Task<List<S3FolderRow>> ListFoldersAsync(string archiveName, string relativePrefix)
        {
            // We list one level deep under: prefix = $"{archiveName}/{relativePrefix}"
            string fullPrefix = CombineS3Prefix(archiveName, relativePrefix);

            JsonDocument doc = await S3ApiListObjectsV2Async(fullPrefix, delimiter: "/");

            var result = new List<S3FolderRow>();

            if (doc.RootElement.TryGetProperty("CommonPrefixes", out var cps) && cps.ValueKind == JsonValueKind.Array)
            {
                foreach (var cp in cps.EnumerateArray())
                {
                    if (!cp.TryGetProperty("Prefix", out var pEl)) continue;
                    var prefix = pEl.GetString() ?? "";
                    // prefix is full, like "ArchiveName/Media/CamA/"
                    var rel = StripArchivePrefix(archiveName, prefix);
                    var display = GetLastFolderName(rel);

                    if (!string.IsNullOrWhiteSpace(rel))
                    {
                        result.Add(new S3FolderRow
                        {
                            RelativePrefix = rel,
                            DisplayName = display
                        });
                    }
                }
            }

            return result;
        }

        private async Task<List<S3ObjectRow>> ListFilesAsync(string archiveName, string relativePrefix)
        {
            string fullPrefix = CombineS3Prefix(archiveName, relativePrefix);

            // paginate to collect all files under this prefix (non-recursive, with delimiter)
            var allContents = new List<JsonElement>();
            string? token = null;

            while (true)
            {
                JsonDocument doc = await S3ApiListObjectsV2Async(fullPrefix, delimiter: "/", continuationToken: token);

                if (doc.RootElement.TryGetProperty("Contents", out var contents) && contents.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in contents.EnumerateArray())
                        allContents.Add(item);
                }

                if (doc.RootElement.TryGetProperty("NextContinuationToken", out var nextTokEl)
                    && nextTokEl.ValueKind == JsonValueKind.String)
                {
                    token = nextTokEl.GetString();
                    if (string.IsNullOrWhiteSpace(token))
                        break;
                }
                else
                {
                    break;
                }
            }

            var result = new List<S3ObjectRow>();

            foreach (var item in allContents)
            {
                if (!item.TryGetProperty("Key", out var keyEl)) continue;
                string key = keyEl.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(key)) continue;

                // skip "folder marker" objects (keys ending with "/")
                if (key.EndsWith("/")) continue;

                long size = 0;
                if (item.TryGetProperty("Size", out var sizeEl) && sizeEl.ValueKind == JsonValueKind.Number)
                    size = sizeEl.GetInt64();

                string lastModifiedText = "";
                if (item.TryGetProperty("LastModified", out var lmEl) && lmEl.ValueKind == JsonValueKind.String)
                    lastModifiedText = lmEl.GetString() ?? "";

                // Display name: strip prefix path
                string fileName = Path.GetFileName(key.Replace('/', Path.DirectorySeparatorChar));

                result.Add(new S3ObjectRow
                {
                    Key = key,
                    FileName = fileName,
                    SizeBytes = size,
                    LastModifiedText = lastModifiedText
                });
            }

            return result;
        }

        private async void btnRestoreFolder_Click(object sender, EventArgs e)
        {
            if (_selectedArchiveName == null)
            {
                MessageBox.Show(this, "Select an archive first.", "Restore Folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string relPrefix = _selectedRelativePrefix; // could be ""
            string s3Prefix = CombineS3Prefix(_selectedArchiveName, relPrefix); // "Archive/Media/"

            using var dialog = new FolderBrowserDialog
            {
                Description = "Select local destination root (archive structure will be recreated under it)",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            string destRoot = dialog.SelectedPath;
            if (string.IsNullOrWhiteSpace(destRoot))
                return;

            // Local target is destRoot\ArchiveName\<relPrefix>
            string localPath = Path.Combine(destRoot, _selectedArchiveName, relPrefix.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(localPath);

            string sourceS3 = $"s3://{BucketName}/{s3Prefix}";
            if (!sourceS3.EndsWith("/")) sourceS3 += "/";

            var confirm = MessageBox.Show(this,
                $"Restore folder?\n\nS3:   {sourceS3}\nTo:   {localPath}",
                "Confirm Folder Restore",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.OK)
                return;

            int exitCode = await RunAwsCommandAsync(
                $"s3 sync \"{sourceS3}\" \"{localPath}\"",
                _ => { /* optional: hook to a log UI later */ });

            if (exitCode == 0)
            {
                MessageBox.Show(this, "Folder restore completed.", "Restore Folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, $"Folder restore failed. Exit code: {exitCode}", "Restore Folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnRestoreSelectedFile_Click(object sender, EventArgs e)
        {
            if (_selectedArchiveName == null)
            {
                MessageBox.Show(this, "Select an archive first.", "Restore File",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (gridFiles.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Select a file row first.", "Restore File",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = gridFiles.SelectedRows[0];
            var keyObj = row.Cells["S3Key"].Value;
            string key = keyObj as string ?? keyObj?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show(this, "Could not determine S3 key for selected file.", "Restore File",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var dialog = new FolderBrowserDialog
            {
                Description = "Select local destination root (archive structure will be recreated under it)",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            string destRoot = dialog.SelectedPath;
            if (string.IsNullOrWhiteSpace(destRoot))
                return;

            // Local path = destRoot\<full key path>
            string localFullPath = Path.Combine(destRoot, key.Replace('/', Path.DirectorySeparatorChar));
            string? dir = Path.GetDirectoryName(localFullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string sourceS3 = $"s3://{BucketName}/{key}";

            var confirm = MessageBox.Show(this,
                $"Restore file?\n\nS3:   {sourceS3}\nTo:   {localFullPath}",
                "Confirm File Restore",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.OK)
                return;

            int exitCode = await RunAwsCommandAsync(
                $"s3 cp \"{sourceS3}\" \"{localFullPath}\"",
                _ => { });

            if (exitCode == 0)
            {
                MessageBox.Show(this, "File restore completed.", "Restore File",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, $"File restore failed. Exit code: {exitCode}", "Restore File",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -----------------------
        // AWS helpers
        // -----------------------

        private Task<int> RunAwsCommandAsync(string arguments, Action<string?> onOutputLine)
        {
            var tcs = new TaskCompletionSource<int>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "aws",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                process.OutputDataReceived += (s, e) => { if (e.Data != null) onOutputLine?.Invoke(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) onOutputLine?.Invoke("[ERR] " + e.Data); };

                process.Exited += (s, e) =>
                {
                    tcs.TrySetResult(process.ExitCode);
                    process.Dispose();
                };

                if (!process.Start())
                {
                    tcs.TrySetResult(-1);
                }
                else
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }
            catch (Exception ex)
            {
                onOutputLine?.Invoke("[EXCEPTION] " + ex.Message);
                tcs.TrySetResult(-1);
            }

            return tcs.Task;
        }

        private async Task<string> RunAwsCommandCaptureAsync(string arguments)
        {
            var sb = new StringBuilder();
            int exit = await RunAwsCommandAsync(arguments, line =>
            {
                if (line != null) sb.AppendLine(line);
            });

            if (exit != 0)
                throw new Exception($"aws exited with code {exit}. Args: {arguments}");

            return sb.ToString();
        }

        private async Task<JsonDocument> S3ApiListObjectsV2Async(string fullPrefix, string delimiter, string? continuationToken = null)
        {
            // fullPrefix is "ArchiveName/Media/CamA/" (or "ArchiveName/")
            // We use s3api because it returns CommonPrefixes + Contents properly.
            var args = new StringBuilder();
            args.Append("s3api list-objects-v2 ");
            args.Append($"--bucket \"{BucketName}\" ");
            args.Append($"--prefix \"{fullPrefix}\" ");
            args.Append($"--delimiter \"{delimiter}\" ");
            args.Append("--max-keys 1000 ");

            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                args.Append($"--continuation-token \"{continuationToken}\" ");
            }

            // --output json is default, but explicit is fine
            args.Append("--output json");

            string json = await RunAwsCommandCaptureAsync(args.ToString());
            return JsonDocument.Parse(json);
        }

        private async Task<(long totalBytes, long totalObjects)> GetArchiveStatsAsync(string archiveName)
        {
            long size = 0;
            long count = -1;

            string bucketPath = $"s3://{BucketName}/{archiveName}/";

            await RunAwsCommandAsync(
                $"s3 ls \"{bucketPath}\" --recursive --summarize",
                line =>
                {
                    if (line == null) return;

                    if (line.Contains("Total Size:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && long.TryParse(parts[^1], out long parsed))
                            size = parsed;
                    }
                    else if (line.Contains("Total Objects:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && long.TryParse(parts[^1], out long parsed))
                            count = parsed;
                    }
                });

            return (size, count);
        }

        // -----------------------
        // Google Chat webhook
        // -----------------------

        private async Task NotifyGoogleChatAsync(string messageText)
        {
            if (string.IsNullOrWhiteSpace(GoogleChatWebhookUrl))
                return;

            try
            {
                using var client = new HttpClient();
                var payload = new { text = messageText };
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(GoogleChatWebhookUrl, content);
                // We don't throw; this shouldn't break uploads.
            }
            catch
            {
                // silent: don't block real work if chat is down
            }
        }

        // -----------------------
        // Misc helpers
        // -----------------------

        private static string CombineS3Prefix(string archiveName, string relativePrefix)
        {
            // relativePrefix is "" or "Media/CamA/"
            if (string.IsNullOrWhiteSpace(relativePrefix))
                return $"{archiveName}/";

            // ensure trailing slash
            string rel = relativePrefix.Replace('\\', '/');
            if (!rel.EndsWith("/")) rel += "/";

            return $"{archiveName}/{rel}";
        }

        private static string StripArchivePrefix(string archiveName, string fullPrefix)
        {
            // fullPrefix is "Archive/Media/CamA/"
            string basePrefix = archiveName.TrimEnd('/') + "/";
            if (fullPrefix.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
                return fullPrefix.Substring(basePrefix.Length);
            return fullPrefix;
        }

        private static string GetLastFolderName(string relativePrefix)
        {
            // "Media/CamA/" -> "CamA"
            string p = relativePrefix.TrimEnd('/');
            int idx = p.LastIndexOf('/');
            return idx >= 0 ? p.Substring(idx + 1) : p;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 0) return "unknown";

            string[] sizes = { "B", "KiB", "MiB", "GiB", "TiB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private static void AppendLog(TextBox target, string line)
        {
            if (target == null) return;

            if (target.InvokeRequired)
            {
                target.BeginInvoke(new Action<TextBox, string>(AppendLog), target, line);
                return;
            }

            if (target.TextLength == 0)
                target.AppendText(line);
            else
                target.AppendText(Environment.NewLine + line);
        }

        // -----------------------
        // Models
        // -----------------------

        private class ArchiveInfo
        {
            public string Name { get; set; } = "";
            public long SizeBytes { get; set; }
            public long ObjectCount { get; set; }
        }

        private class S3FolderRow
        {
            public string RelativePrefix { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }

        private class S3ObjectRow
        {
            public string Key { get; set; } = "";
            public string FileName { get; set; } = "";
            public long SizeBytes { get; set; }
            public string LastModifiedText { get; set; } = "";
        }
    }
}
