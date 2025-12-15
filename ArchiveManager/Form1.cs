// Form1.cs
// SG Archive Manager (WinForms .NET 8)
//
// Features:
// - Upload: prep scan (files + bytes) → confirm → upload file-by-file with progress + ETA + cancel + Google Chat notifications
// - Browse: list archives (top-level prefixes) with size + object count, export CSV
// - Browse tree: prefix explorer + files grid
// - Restore: restore selected file or current folder with progress + ETA + cancel + Google Chat notifications
//   * During large single-file restores, uses Marquee (activity) until each file completes, then updates % between files.
//
// Requirements:
// - AWS CLI in PATH (aws.exe)
// - AWS credentials configured on machine (profile or env)
// - appsettings.local.json next to EXE (AppContext.BaseDirectory)
//   {
//     "BucketName": "sticksandglassarchive",
//     "GoogleChatWebhookUrl": "https://chat.googleapis.com/v1/spaces/.../messages?key=...&token=..."
//   }

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArchiveManager
{
    public partial class Form1 : Form
    {
        private readonly HttpClient _http = new HttpClient();

        private string _bucketName = "sticksandglassarchive";
        private string? _googleChatWebhookUrl;

        private readonly BindingList<ArchiveInfo> _archiveStats = new BindingList<ArchiveInfo>();
        private readonly BindingList<S3FileRow> _files = new BindingList<S3FileRow>();

        private string? _selectedArchive;
        private string _currentPrefix = ""; // within archive, e.g. "Assets/001_RUSHES/"

        private CancellationTokenSource? _uploadCts;
        private CancellationTokenSource? _restoreCts;

        public Form1()
        {
            InitializeComponent();

            gridArchives.AutoGenerateColumns = true;
            gridArchives.DataSource = _archiveStats;

            gridFiles.AutoGenerateColumns = true;
            gridFiles.DataSource = _files;

            progressArchive.Minimum = 0;
            progressArchive.Maximum = 100;
            progressArchive.Value = 0;
            lblArchiveEta.Text = "";

            progressRestore.Minimum = 0;
            progressRestore.Maximum = 100;
            progressRestore.Value = 0;
            lblRestoreEta.Text = "";
            txtRestoreLog.Text = "";

            txtArchiveLog.Text = "";

            LoadLocalSettings();
        }

        // -----------------------------
        // Settings
        // -----------------------------
        private void LoadLocalSettings()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
                if (!File.Exists(path))
                {
                    AppendArchiveLog($"Settings not found: {path} (OK)");
                    return;
                }

                string json = File.ReadAllText(path, Encoding.UTF8);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("BucketName", out var bucketProp))
                {
                    var b = bucketProp.GetString();
                    if (!string.IsNullOrWhiteSpace(b))
                        _bucketName = b.Trim();
                }

                if (doc.RootElement.TryGetProperty("GoogleChatWebhookUrl", out var hookProp))
                {
                    var url = hookProp.GetString();
                    if (!string.IsNullOrWhiteSpace(url))
                        _googleChatWebhookUrl = url.Trim();
                }

                AppendArchiveLog($"Loaded settings. Bucket={_bucketName}");
            }
            catch (Exception ex)
            {
                AppendArchiveLog($"Failed to load appsettings.local.json: {ex.Message}");
            }
        }

        // -----------------------------
        // Archive tab: browse source
        // -----------------------------
        private void btnBrowseSource_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select the source folder to upload";
            dialog.ShowNewFolderButton = false;

            if (dialog.ShowDialog(this) == DialogResult.OK)
                txtSourcePath.Text = dialog.SelectedPath;
        }

        // -----------------------------
        // Upload (prep, progress, cancel, chat)
        // -----------------------------
        private async void btnStartUpload_Click(object sender, EventArgs e)
        {
            if (_uploadCts != null)
            {
                _uploadCts.Cancel();
                return;
            }

            string source = (txtSourcePath.Text ?? "").Trim();
            string archiveName = (txtArchiveName.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(source) || !Directory.Exists(source))
            {
                MessageBox.Show(this, "Pick a valid source folder.", "Upload", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(archiveName))
            {
                MessageBox.Show(this, "Enter an archive name (this becomes the folder in S3).", "Upload", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var prep = await Task.Run(() => PrepScan(source));
            string sizeHuman = FormatBytes(prep.TotalBytes);

            var confirm = MessageBox.Show(
                this,
                $"You are about to upload:\n\nSource:\n{source}\n\nArchive folder:\n{archiveName}\n\nFiles: {prep.FileCount:n0}\nTotal size: {sizeHuman}\n\nProceed?",
                "Confirm Upload",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes)
                return;

            _uploadCts = new CancellationTokenSource();
            btnStartUpload.Text = "Cancel Upload";
            btnStartUpload.Enabled = true;

            progressArchive.Style = ProgressBarStyle.Blocks;
            progressArchive.Value = 0;
            lblArchiveEta.Text = "";

            AppendArchiveLog($"Starting upload to s3://{_bucketName}/{archiveName}/");
            await SendChatSafeAsync($"📦 Upload started: `{archiveName}`\nFiles: {prep.FileCount:n0}\nSize: {sizeHuman}");

            var sw = Stopwatch.StartNew();
            long bytesDone = 0;
            int filesDone = 0;

            try
            {
                var files = prep.Files;

                foreach (var file in files)
                {
                    _uploadCts.Token.ThrowIfCancellationRequested();

                    string rel = Path.GetRelativePath(source, file).Replace('\\', '/');
                    string key = $"{archiveName}/{rel}";
                    long len = new FileInfo(file).Length;

                    AppendArchiveLog($"UPLOAD: {rel}");

                    await RunAwsAsync(
                        $"s3 cp \"{file}\" \"s3://{_bucketName}/{key}\" --only-show-errors",
                        _uploadCts.Token,
                        onStdErrLine: (line) =>
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                AppendArchiveLog(line);
                        }
                    );

                    filesDone++;
                    bytesDone += len;

                    UpdateProgressWithEta(
                        progressArchive,
                        lblArchiveEta,
                        bytesDone,
                        prep.TotalBytes,
                        sw.Elapsed,
                        filesDone,
                        prep.FileCount
                    );
                }

                sw.Stop();
                progressArchive.Value = 100;
                lblArchiveEta.Text = $"Complete. {prep.FileCount:n0} files, {FormatBytes(prep.TotalBytes)} in {FormatDuration(sw.Elapsed)}.";

                AppendArchiveLog("Upload finished.");
                await SendChatSafeAsync($"✅ Upload finished: `{archiveName}`\nSize: {sizeHuman}\nDuration: {FormatDuration(sw.Elapsed)}");
            }
            catch (OperationCanceledException)
            {
                AppendArchiveLog("Upload cancelled.");
                await SendChatSafeAsync($"🛑 Upload cancelled: `{archiveName}`");
            }
            catch (Exception ex)
            {
                AppendArchiveLog($"Upload failed: {ex.Message}");
                await SendChatSafeAsync($"❌ Upload failed: `{archiveName}`\nError: {ex.Message}");
                MessageBox.Show(this, $"Upload failed:\n{ex.Message}", "Upload", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _uploadCts?.Dispose();
                _uploadCts = null;
                btnStartUpload.Text = "Start Upload";
            }
        }

        private PrepResult PrepScan(string root)
        {
            var list = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToList();
            long total = 0;

            foreach (var f in list)
            {
                try { total += new FileInfo(f).Length; }
                catch { }
            }

            return new PrepResult
            {
                FileCount = list.Count,
                TotalBytes = total,
                Files = list
            };
        }

        // -----------------------------
        // Browse tab: List Archives + Export CSV
        // -----------------------------
        private async void btnBrowseRefresh_Click(object sender, EventArgs e)
        {
            btnBrowseRefresh.Enabled = false;
            btnExportCsv.Enabled = false;

            try
            {
                _archiveStats.Clear();
                _selectedArchive = null;
                _currentPrefix = "";
                treeS3.Nodes.Clear();
                _files.Clear();
                lblBrowsePath.Text = "Loading archives...";

                var archives = await ListTopLevelPrefixesAsync(CancellationToken.None);

                foreach (var a in archives)
                {
                    lblBrowsePath.Text = $"Calculating stats: {a} ...";
                    var stats = await GetPrefixStatsAsync(a + "/", CancellationToken.None);
                    _archiveStats.Add(new ArchiveInfo
                    {
                        Name = a,
                        SizeBytes = stats.TotalBytes,
                        ObjectCount = stats.ObjectCount
                    });
                }

                lblBrowsePath.Text = "Select an archive to browse...";
                btnExportCsv.Enabled = _archiveStats.Count > 0;
            }
            catch (Exception ex)
            {
                lblBrowsePath.Text = "Failed to list archives.";
                MessageBox.Show(this, $"List Archives failed:\n{ex.Message}", "Browse", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBrowseRefresh.Enabled = true;
            }
        }

        private void gridArchives_SelectionChanged(object sender, EventArgs e)
        {
            if (gridArchives.CurrentRow == null)
                return;

            if (gridArchives.CurrentRow.DataBoundItem is not ArchiveInfo ai)
                return;

            if (string.IsNullOrWhiteSpace(ai.Name))
                return;

            _selectedArchive = ai.Name;
            _currentPrefix = "";
            lblBrowsePath.Text = $"s3://{_bucketName}/{_selectedArchive}/";

            treeS3.Nodes.Clear();
            var root = new TreeNode(_selectedArchive) { Tag = "" };
            root.Nodes.Add(new TreeNode("loading...") { Tag = "__DUMMY__" });
            treeS3.Nodes.Add(root);
            root.Expand();

            _ = LoadPrefixIntoFilesGridAsync(_selectedArchive, "");
        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (_archiveStats.Count == 0)
            {
                MessageBox.Show(this, "No archive data to export. Click 'List Archives' first.", "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog();
            dialog.Title = "Export archive list to CSV";
            dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            dialog.FileName = $"archive-list-{DateTime.Now:yyyy-MM-dd_HHmmss}.csv";

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
                writer.WriteLine("ArchiveName,SizeBytes,ObjectCount,SizeHuman");

                foreach (var info in _archiveStats)
                {
                    string sizeHuman = FormatBytes(info.SizeBytes);
                    string safeName = info.Name.Replace("\"", "\"\"");
                    writer.WriteLine($"\"{safeName}\",{info.SizeBytes},{info.ObjectCount},{sizeHuman}");
                }

                MessageBox.Show(this, "CSV export complete.", "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to export CSV: {ex.Message}",
                    "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -----------------------------
        // Tree explorer (prefix browsing)
        // -----------------------------
        private async void treeS3_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (_selectedArchive == null) return;

            if (e.Node.Nodes.Count == 1 && (string?)e.Node.Nodes[0].Tag == "__DUMMY__")
                e.Node.Nodes.Clear();

            string prefixWithinArchive = (string?)e.Node.Tag ?? "";

            try
            {
                var childPrefixes = await ListChildPrefixesAsync(_selectedArchive, prefixWithinArchive, CancellationToken.None);
                foreach (var cp in childPrefixes)
                {
                    var node = new TreeNode(cp) { Tag = JoinPrefix(prefixWithinArchive, cp) };
                    node.Nodes.Add(new TreeNode("loading...") { Tag = "__DUMMY__" });
                    e.Node.Nodes.Add(node);
                }
            }
            catch
            {
            }
        }

        private async void treeS3_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_selectedArchive == null) return;

            string prefixWithinArchive = (string?)e.Node.Tag ?? "";
            _currentPrefix = prefixWithinArchive;

            lblBrowsePath.Text = $"s3://{_bucketName}/{_selectedArchive}/{prefixWithinArchive}";
            await LoadPrefixIntoFilesGridAsync(_selectedArchive, prefixWithinArchive);
        }

        private async Task LoadPrefixIntoFilesGridAsync(string archiveName, string relativePrefix)
        {
            try
            {
                _files.Clear();
                var rows = await ListFilesAsync(archiveName, relativePrefix);
                foreach (var r in rows)
                    _files.Add(r);
            }
            catch (Exception ex)
            {
                _files.Clear();
                _files.Add(new S3FileRow { Key = $"(error) {ex.Message}", SizeBytes = 0, LastModified = "" });
            }
        }

        private void btnBrowseUp_Click(object sender, EventArgs e)
        {
            if (_selectedArchive == null) return;
            if (string.IsNullOrEmpty(_currentPrefix)) return;

            var parts = _currentPrefix.TrimEnd('/').Split('/');
            if (parts.Length <= 1)
                _currentPrefix = "";
            else
                _currentPrefix = string.Join("/", parts.Take(parts.Length - 1)) + "/";

            lblBrowsePath.Text = $"s3://{_bucketName}/{_selectedArchive}/{_currentPrefix}";
            _ = LoadPrefixIntoFilesGridAsync(_selectedArchive, _currentPrefix);
        }

        private static string JoinPrefix(string parentWithinArchive, string childName)
        {
            var p = parentWithinArchive ?? "";
            if (!string.IsNullOrEmpty(p) && !p.EndsWith("/")) p += "/";
            return p + childName + "/";
        }

        // -----------------------------
        // Restore with progress + chat confirmations
        // -----------------------------
        private async void btnRestoreSelectedFile_Click(object sender, EventArgs e)
        {
            if (_selectedArchive == null)
            {
                MessageBox.Show(this, "Select an archive first.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_restoreCts != null)
            {
                _restoreCts.Cancel();
                return;
            }

            if (gridFiles.CurrentRow == null || gridFiles.CurrentRow.DataBoundItem is not S3FileRow selected)
            {
                MessageBox.Show(this, "Select a file row first.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(selected.Key) || selected.Key.EndsWith("/"))
            {
                MessageBox.Show(this, "Select a file (not a folder marker).", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select local folder to restore into";
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            string destRoot = dialog.SelectedPath;

            await RestoreObjectsWithUiAsync(
                archiveName: _selectedArchive,
                objects: new List<S3ObjectRef> {
                    new S3ObjectRef {
                        Key = selected.Key,
                        SizeBytes = selected.SizeBytes
                    }
                },
                destRoot: destRoot,
                displayName: $"file {Path.GetFileName(selected.Key)}"
            );
        }

        private async void btnRestoreFolder_Click(object sender, EventArgs e)
        {
            if (_selectedArchive == null)
            {
                MessageBox.Show(this, "Select an archive first.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_restoreCts != null)
            {
                _restoreCts.Cancel();
                return;
            }

            string fullPrefix = $"{_selectedArchive}/{_currentPrefix}".TrimEnd('/') + "/";

            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select local folder to restore into";
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            string destRoot = dialog.SelectedPath;

            txtRestoreLog.Text = "";
            AppendRestoreLog($"Preparing restore listing for: s3://{_bucketName}/{fullPrefix}");

            List<S3ObjectRef> objects;
            try
            {
                objects = await ListAllObjectsUnderPrefixAsync(fullPrefix, CancellationToken.None);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to enumerate folder:\n{ex.Message}", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (objects.Count == 0)
            {
                MessageBox.Show(this, "No files found under this folder.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            long totalBytes = objects.Sum(o => o.SizeBytes);
            var confirm = MessageBox.Show(
                this,
                $"Restore folder:\n\ns3://{_bucketName}/{fullPrefix}\n\nFiles: {objects.Count:n0}\nSize: {FormatBytes(totalBytes)}\n\nProceed?",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes)
                return;

            await RestoreObjectsWithUiAsync(
                archiveName: _selectedArchive,
                objects: objects,
                destRoot: destRoot,
                displayName: $"folder {_currentPrefix}"
            );
        }

        private async Task RestoreObjectsWithUiAsync(string archiveName, List<S3ObjectRef> objects, string destRoot, string displayName)
        {
            _restoreCts = new CancellationTokenSource();

            btnRestoreSelectedFile.Text = "Cancel Restore";
            btnRestoreFolder.Text = "Cancel Restore";
            btnRestoreSelectedFile.Enabled = true;
            btnRestoreFolder.Enabled = true;

            progressRestore.Value = 0;
            progressRestore.Style = ProgressBarStyle.Marquee;
            progressRestore.MarqueeAnimationSpeed = 25;

            lblRestoreEta.Text = "Starting restore...";
            txtRestoreLog.Text = "";

            long totalBytes = objects.Sum(o => o.SizeBytes);
            string totalHuman = FormatBytes(totalBytes);

            AppendRestoreLog($"Starting restore ({displayName})");
            AppendRestoreLog($"Files: {objects.Count:n0}  Size: {totalHuman}");
            await SendChatSafeAsync($"⬇️ Restore started: `{archiveName}`\nTarget: {displayName}\nFiles: {objects.Count:n0}\nSize: {totalHuman}");

            var sw = Stopwatch.StartNew();
            long bytesDone = 0;
            int filesDone = 0;

            try
            {
                foreach (var obj in objects)
                {
                    _restoreCts.Token.ThrowIfCancellationRequested();

                    string relative = obj.Key.Replace('\\', '/');
                    if (relative.StartsWith(archiveName + "/", StringComparison.OrdinalIgnoreCase))
                        relative = relative.Substring(archiveName.Length + 1);

                    string localPath = Path.Combine(destRoot, archiveName, relative.Replace('/', Path.DirectorySeparatorChar));
                    string localDir = Path.GetDirectoryName(localPath) ?? Path.Combine(destRoot, archiveName);
                    Directory.CreateDirectory(localDir);

                    AppendRestoreLog($"RESTORE: {relative}");

                    SafeUi(() =>
                    {
                        // activity indicator during single large file downloads
                        progressRestore.Style = ProgressBarStyle.Marquee;
                        progressRestore.MarqueeAnimationSpeed = 25;
                        lblRestoreEta.Text = $"Downloading: {Path.GetFileName(obj.Key)} ...";
                    });

                    await RunAwsAsync(
                        $"s3 cp \"s3://{_bucketName}/{obj.Key}\" \"{localPath}\" --only-show-errors",
                        _restoreCts.Token,
                        onStdErrLine: (line) =>
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                AppendRestoreLog(line);
                        }
                    );

                    filesDone++;
                    bytesDone += obj.SizeBytes;

                    // switch to blocks to show percentage between files
                    SafeUi(() =>
                    {
                        progressRestore.Style = ProgressBarStyle.Blocks;
                        progressRestore.MarqueeAnimationSpeed = 0;
                    });

                    UpdateProgressWithEta(
                        progressRestore,
                        lblRestoreEta,
                        bytesDone,
                        totalBytes,
                        sw.Elapsed,
                        filesDone,
                        objects.Count
                    );
                }

                sw.Stop();
                SafeUi(() =>
                {
                    progressRestore.Style = ProgressBarStyle.Blocks;
                    progressRestore.MarqueeAnimationSpeed = 0;
                    progressRestore.Value = 100;
                    lblRestoreEta.Text = $"Complete. {objects.Count:n0} files, {totalHuman} in {FormatDuration(sw.Elapsed)}.";
                });

                AppendRestoreLog("Restore finished.");
                await SendChatSafeAsync($"✅ Restore finished: `{archiveName}`\nTarget: {displayName}\nSize: {totalHuman}\nDuration: {FormatDuration(sw.Elapsed)}");
            }
            catch (OperationCanceledException)
            {
                AppendRestoreLog("Restore cancelled.");
                await SendChatSafeAsync($"🛑 Restore cancelled: `{archiveName}`\nTarget: {displayName}");
            }
            catch (Exception ex)
            {
                AppendRestoreLog($"Restore failed: {ex.Message}");
                await SendChatSafeAsync($"❌ Restore failed: `{archiveName}`\nTarget: {displayName}\nError: {ex.Message}");
                MessageBox.Show(this, $"Restore failed:\n{ex.Message}", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                sw.Stop();

                _restoreCts?.Dispose();
                _restoreCts = null;

                SafeUi(() =>
                {
                    progressRestore.Style = ProgressBarStyle.Blocks;
                    progressRestore.MarqueeAnimationSpeed = 0;
                });

                btnRestoreSelectedFile.Text = "Restore Selected File...";
                btnRestoreFolder.Text = "Restore Folder...";
            }
        }

        // -----------------------------
        // S3 Listing helpers (AWS CLI + JSON)
        // -----------------------------
        private async Task<List<string>> ListTopLevelPrefixesAsync(CancellationToken ct)
        {
            string args = $"s3api list-objects-v2 --bucket \"{_bucketName}\" --delimiter \"/\" --output json";
            string json = await RunAwsCaptureStdOutAsync(args, ct);

            using var doc = JsonDocument.Parse(json);
            var list = new List<string>();

            if (doc.RootElement.TryGetProperty("CommonPrefixes", out var cps) && cps.ValueKind == JsonValueKind.Array)
            {
                foreach (var cp in cps.EnumerateArray())
                {
                    if (cp.TryGetProperty("Prefix", out var p))
                    {
                        var prefix = p.GetString() ?? "";
                        prefix = prefix.TrimEnd('/');
                        if (!string.IsNullOrWhiteSpace(prefix))
                            list.Add(prefix);
                    }
                }
            }

            return list.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<(long TotalBytes, long ObjectCount)> GetPrefixStatsAsync(string prefix, CancellationToken ct)
        {
            long totalBytes = 0;
            long totalCount = 0;
            string? token = null;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                string args = $"s3api list-objects-v2 --bucket \"{_bucketName}\" --prefix \"{prefix}\" --max-items 1000 --output json";
                if (!string.IsNullOrWhiteSpace(token))
                    args += $" --starting-token \"{token}\"";

                string json = await RunAwsCaptureStdOutAsync(args, ct);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("Contents", out var contents) && contents.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in contents.EnumerateArray())
                    {
                        if (item.TryGetProperty("Size", out var sizeProp))
                            totalBytes += sizeProp.GetInt64();
                        totalCount++;
                    }
                }

                if (doc.RootElement.TryGetProperty("NextToken", out var nt) && nt.ValueKind == JsonValueKind.String)
                {
                    token = nt.GetString();
                    if (!string.IsNullOrWhiteSpace(token))
                        continue;
                }

                break;
            }

            return (totalBytes, totalCount);
        }

        private async Task<List<string>> ListChildPrefixesAsync(string archiveName, string relativePrefixWithinArchive, CancellationToken ct)
        {
            string prefix = $"{archiveName}/{relativePrefixWithinArchive}".TrimEnd('/');
            if (!prefix.EndsWith("/")) prefix += "/";

            string args = $"s3api list-objects-v2 --bucket \"{_bucketName}\" --prefix \"{prefix}\" --delimiter \"/\" --output json";
            string json = await RunAwsCaptureStdOutAsync(args, ct);

            using var doc = JsonDocument.Parse(json);
            var list = new List<string>();

            if (doc.RootElement.TryGetProperty("CommonPrefixes", out var cps) && cps.ValueKind == JsonValueKind.Array)
            {
                foreach (var cp in cps.EnumerateArray())
                {
                    if (cp.TryGetProperty("Prefix", out var p))
                    {
                        var full = p.GetString() ?? "";
                        full = full.TrimEnd('/');
                        var name = full.Split('/').LastOrDefault() ?? "";
                        if (!string.IsNullOrWhiteSpace(name))
                            list.Add(name);
                    }
                }
            }

            return list.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<List<S3FileRow>> ListFilesAsync(string archiveName, string relativePrefixWithinArchive)
        {
            string prefix = $"{archiveName}/{relativePrefixWithinArchive}".TrimEnd('/');
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/")) prefix += "/";

            string args = $"s3api list-objects-v2 --bucket \"{_bucketName}\" --prefix \"{prefix}\" --delimiter \"/\" --output json";
            string json = await RunAwsCaptureStdOutAsync(args, CancellationToken.None);

            using var doc = JsonDocument.Parse(json);
            var rows = new List<S3FileRow>();

            if (doc.RootElement.TryGetProperty("CommonPrefixes", out var cps) && cps.ValueKind == JsonValueKind.Array)
            {
                foreach (var cp in cps.EnumerateArray())
                {
                    if (cp.TryGetProperty("Prefix", out var p))
                    {
                        var full = p.GetString() ?? "";
                        rows.Add(new S3FileRow
                        {
                            Key = full,
                            SizeBytes = 0,
                            LastModified = "(folder)"
                        });
                    }
                }
            }

            if (doc.RootElement.TryGetProperty("Contents", out var contents) && contents.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in contents.EnumerateArray())
                {
                    string key = item.GetProperty("Key").GetString() ?? "";
                    long size = item.TryGetProperty("Size", out var sz) ? sz.GetInt64() : 0;
                    string lm = item.TryGetProperty("LastModified", out var lmProp) ? (lmProp.GetString() ?? "") : "";

                    if (key.EndsWith("/") && size <= 1) continue;

                    rows.Add(new S3FileRow
                    {
                        Key = key,
                        SizeBytes = size,
                        LastModified = lm
                    });
                }
            }

            return rows
                .OrderByDescending(r => r.LastModified == "(folder)")
                .ThenBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<List<S3ObjectRef>> ListAllObjectsUnderPrefixAsync(string fullPrefix, CancellationToken ct)
        {
            var results = new List<S3ObjectRef>();
            string? token = null;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                string args = $"s3api list-objects-v2 --bucket \"{_bucketName}\" --prefix \"{fullPrefix}\" --max-items 1000 --output json";
                if (!string.IsNullOrWhiteSpace(token))
                    args += $" --starting-token \"{token}\"";

                string json = await RunAwsCaptureStdOutAsync(args, ct);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("Contents", out var contents) && contents.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in contents.EnumerateArray())
                    {
                        string key = item.GetProperty("Key").GetString() ?? "";
                        long size = item.TryGetProperty("Size", out var sz) ? sz.GetInt64() : 0;

                        if (key.EndsWith("/") && size <= 1) continue;

                        results.Add(new S3ObjectRef { Key = key, SizeBytes = size });
                    }
                }

                if (doc.RootElement.TryGetProperty("NextToken", out var nt) && nt.ValueKind == JsonValueKind.String)
                {
                    token = nt.GetString();
                    if (!string.IsNullOrWhiteSpace(token))
                        continue;
                }

                break;
            }

            return results;
        }

        // -----------------------------
        // AWS Process helpers
        // -----------------------------
        private async Task<string> RunAwsCaptureStdOutAsync(string args, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "aws",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            p.OutputDataReceived += (_, e) => { if (e.Data != null) sbOut.AppendLine(e.Data); };
            p.ErrorDataReceived += (_, e) => { if (e.Data != null) sbErr.AppendLine(e.Data); };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            using var reg = ct.Register(() =>
            {
                try { if (!p.HasExited) p.Kill(true); } catch { }
            });

            await p.WaitForExitAsync(ct);

            if (p.ExitCode != 0)
            {
                var err = sbErr.ToString().Trim();
                if (string.IsNullOrWhiteSpace(err)) err = "(no error output)";
                throw new Exception($"aws {args}\n\n{err}");
            }

            return sbOut.ToString();
        }

        private async Task RunAwsAsync(string args, CancellationToken ct, Action<string>? onStdErrLine = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "aws",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

            p.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null && onStdErrLine != null)
                    SafeUi(() => onStdErrLine(e.Data));
            };

            p.Start();
            p.BeginErrorReadLine();

            using var reg = ct.Register(() =>
            {
                try { if (!p.HasExited) p.Kill(true); } catch { }
            });

            await p.WaitForExitAsync(ct);

            if (p.ExitCode != 0)
            {
                string err = await p.StandardError.ReadToEndAsync();
                err = (err ?? "").Trim();
                if (string.IsNullOrWhiteSpace(err)) err = "(no error output)";
                throw new Exception($"aws {args}\n\n{err}");
            }
        }

        // -----------------------------
        // Google Chat webhook
        // -----------------------------
        private async Task SendChatSafeAsync(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_googleChatWebhookUrl))
                    return;

                var payload = new { text = message };
                string json = JsonSerializer.Serialize(payload);

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync(_googleChatWebhookUrl, content);

                if (!resp.IsSuccessStatusCode)
                {
                    AppendArchiveLog($"Google Chat webhook failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                AppendArchiveLog($"Google Chat webhook error: {ex.Message}");
            }
        }

        // -----------------------------
        // UI helpers
        // -----------------------------
        private void AppendArchiveLog(string line)
        {
            SafeUi(() =>
            {
                txtArchiveLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
            });
        }

        private void AppendRestoreLog(string line)
        {
            SafeUi(() =>
            {
                txtRestoreLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
            });
        }

        private void SafeUi(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        private void UpdateProgressWithEta(
            ProgressBar bar,
            Label label,
            long bytesDone,
            long totalBytes,
            TimeSpan elapsed,
            int filesDone,
            int totalFiles
        )
        {
            SafeUi(() =>
            {
                int pct = 0;
                if (totalBytes > 0)
                    pct = (int)Math.Clamp((bytesDone * 100.0) / totalBytes, 0, 100);

                // If bar is in marquee, switch back to blocks to show percent
                if (bar.Style != ProgressBarStyle.Blocks)
                {
                    bar.Style = ProgressBarStyle.Blocks;
                    bar.MarqueeAnimationSpeed = 0;
                }

                bar.Value = Math.Clamp(pct, 0, 100);

                double seconds = Math.Max(0.001, elapsed.TotalSeconds);
                double bps = bytesDone / seconds;

                string etaStr = "ETA: unknown";
                if (bps > 1 && bytesDone > 0 && totalBytes > bytesDone)
                {
                    double remaining = (totalBytes - bytesDone) / bps;
                    etaStr = $"ETA: {FormatDuration(TimeSpan.FromSeconds(remaining))}";
                }

                label.Text =
                    $"Progress: {pct}%  |  {filesDone:n0}/{totalFiles:n0} files  |  " +
                    $"{FormatBytes(bytesDone)} / {FormatBytes(totalBytes)}  |  " +
                    $"Elapsed: {FormatDuration(elapsed)}  |  {etaStr}";
            });
        }

        private static string FormatBytes(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB" };
            double b = bytes;
            int i = 0;
            while (b >= 1024 && i < suf.Length - 1)
            {
                b /= 1024;
                i++;
            }
            return $"{b:0.##} {suf[i]}";
        }

        private static string FormatDuration(TimeSpan t)
        {
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m {t.Seconds}s";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }

        // -----------------------------
        // Models
        // -----------------------------
        private class PrepResult
        {
            public int FileCount { get; set; }
            public long TotalBytes { get; set; }
            public List<string> Files { get; set; } = new List<string>();
        }

        private class ArchiveInfo
        {
            public string Name { get; set; } = "";
            public long SizeBytes { get; set; }
            public long ObjectCount { get; set; }
            public string SizeHuman => FormatBytes(SizeBytes);
        }

        private class S3FileRow
        {
            public string Key { get; set; } = "";
            public long SizeBytes { get; set; }
            public string SizeHuman => FormatBytes(SizeBytes);
            public string LastModified { get; set; } = "";
        }

        private class S3ObjectRef
        {
            public string Key { get; set; } = "";
            public long SizeBytes { get; set; }
        }
    }
}