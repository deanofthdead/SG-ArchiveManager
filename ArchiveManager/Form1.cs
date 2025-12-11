using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArchiveManager
{
    public partial class Form1 : Form
    {
        // Change this if you ever rename the bucket
        private const string BucketName = "sticksandglassarchive";

        public Form1()
        {
            InitializeComponent();
        }

        // -----------------------
        // Archive tab
        // -----------------------

        private void btnBrowseSource_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select the project folder to archive";
            dialog.UseDescriptionForTitle = true;

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

            var sourcePath = txtSourcePath.Text.Trim();
            var archiveName = txtArchiveName.Text.Trim();

            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                AppendLog(txtArchiveLog, "ERROR: Source path does not exist.");
                return;
            }

            if (string.IsNullOrWhiteSpace(archiveName))
            {
                archiveName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar));
                txtArchiveName.Text = archiveName;
            }

            var bucketPath = $"s3://{BucketName}/{archiveName}/";

            AppendLog(txtArchiveLog, $"Source: {sourcePath}");
            AppendLog(txtArchiveLog, $"Destination: {bucketPath}");
            AppendLog(txtArchiveLog, "");

            btnStartUpload.Enabled = false;
            try
            {
                var args = $"s3 sync \"{sourcePath}\" \"{bucketPath}\"";
                var exitCode = await RunAwsCommandAsync(args, line => AppendLog(txtArchiveLog, line));

                AppendLog(txtArchiveLog, "");
                AppendLog(txtArchiveLog, $"aws s3 sync exit code: {exitCode}");
            }
            finally
            {
                btnStartUpload.Enabled = true;
            }
        }

        // -----------------------
        // Restore tab
        // -----------------------

        private void btnBrowseRestoreDest_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select the root folder for restore";
            dialog.UseDescriptionForTitle = true;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtRestoreDest.Text = dialog.SelectedPath;
            }
        }

        private async void btnRefreshArchives_Click(object sender, EventArgs e)
        {
            lstArchives.Items.Clear();
            txtRestoreLog.Clear();
            AppendLog(txtRestoreLog, "Listing archives...");

            var args = $"s3 ls s3://{BucketName}/";
            var sb = new StringBuilder();

            int exitCode = await RunAwsCommandAsync(args, line =>
            {
                if (line != null)
                {
                    sb.AppendLine(line);
                    AppendLog(txtRestoreLog, line);
                }
            });

            if (exitCode != 0)
            {
                AppendLog(txtRestoreLog, $"aws s3 ls exited with code {exitCode}");
                return;
            }

            var lines = sb.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("PRE "))
                {
                    var name = trimmed.Substring(4).TrimEnd('/');
                    lstArchives.Items.Add(name);
                }
                else
                {
                    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var last = parts[^1];
                    if (last.EndsWith("/"))
                    {
                        lstArchives.Items.Add(last.TrimEnd('/'));
                    }
                }
            }

            AppendLog(txtRestoreLog, $"Found {lstArchives.Items.Count} archive entries.");
        }

        private async void btnStartRestore_Click(object sender, EventArgs e)
        {
            txtRestoreLog.Clear();

            if (lstArchives.SelectedItem == null)
            {
                AppendLog(txtRestoreLog, "ERROR: No archive selected.");
                return;
            }

            var archiveName = lstArchives.SelectedItem.ToString();
            var destRoot = txtRestoreDest.Text.Trim();

            if (string.IsNullOrWhiteSpace(destRoot))
            {
                AppendLog(txtRestoreLog, "ERROR: No restore destination specified.");
                return;
            }

            if (!Directory.Exists(destRoot))
            {
                Directory.CreateDirectory(destRoot);
            }

            var localPath = Path.Combine(destRoot, archiveName);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            var bucketPath = $"s3://{BucketName}/{archiveName}/";

            AppendLog(txtRestoreLog, $"Restoring archive: {archiveName}");
            AppendLog(txtRestoreLog, $"From: {bucketPath}");
            AppendLog(txtRestoreLog, $"To:   {localPath}");
            AppendLog(txtRestoreLog, "");

            btnStartRestore.Enabled = false;
            try
            {
                var args = $"s3 sync \"{bucketPath}\" \"{localPath}\"";
                var exitCode = await RunAwsCommandAsync(args, line => AppendLog(txtRestoreLog, line));

                AppendLog(txtRestoreLog, "");
                AppendLog(txtRestoreLog, $"aws s3 sync exit code: {exitCode}");
                AppendLog(txtRestoreLog, "Note: If objects are in Glacier/Deep Archive and not restored,");
                AppendLog(txtRestoreLog, "      some files may fail until the S3 restore job completes.");
            }
            finally
            {
                btnStartRestore.Enabled = true;
            }
        }

        // -----------------------
        // Browse tab
        // -----------------------

        private async void btnBrowseRefresh_Click(object sender, EventArgs e)
        {
            txtBrowseOutput.Clear();
            AppendLog(txtBrowseOutput, "Listing archives with sizes...");
            AppendLog(txtBrowseOutput, "");

            var listBuilder = new StringBuilder();

            // First: list top-level prefixes in the bucket
            int exitCode = await RunAwsCommandAsync(
                $"s3 ls s3://{BucketName}/",
                line =>
                {
                    if (line != null)
                    {
                        listBuilder.AppendLine(line);
                    }
                });

            if (exitCode != 0)
            {
                AppendLog(txtBrowseOutput, $"aws s3 ls exited with code {exitCode}");
                return;
            }

            var lines = listBuilder
                .ToString()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var archives = new System.Collections.Generic.List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Typical prefix line: "                           PRE ProjectName/"
                if (trimmed.StartsWith("PRE "))
                {
                    var name = trimmed.Substring(4).TrimEnd('/');
                    archives.Add(name);
                }
                else
                {
                    // Fallback: last token ending with '/'
                    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var last = parts[^1];
                    if (last.EndsWith("/"))
                    {
                        archives.Add(last.TrimEnd('/'));
                    }
                }
            }

            if (archives.Count == 0)
            {
                AppendLog(txtBrowseOutput, "No archives found at top level of bucket.");
                return;
            }

            foreach (var archive in archives)
            {
                AppendLog(txtBrowseOutput, $"Archive: {archive} (calculating size...)");

                var (totalBytes, totalObjects) = await GetArchiveStatsAsync(archive);

                string sizeText = totalBytes > 0 ? FormatBytes(totalBytes) : "unknown";
                string countText = totalObjects >= 0 ? totalObjects.ToString() : "unknown";

                AppendLog(txtBrowseOutput, $"  Size: {sizeText}    Objects: {countText}");
                AppendLog(txtBrowseOutput, "");
            }

            AppendLog(txtBrowseOutput, "Done.");
        }


        // -----------------------
        // AWS helper
        // -----------------------

        private Task<int> RunAwsCommandAsync(string arguments, Action<string> onOutputLine)
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

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                        onOutputLine?.Invoke(e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                        onOutputLine?.Invoke("[ERR] " + e.Data);
                };

                process.Exited += (s, e) =>
                {
                    tcs.TrySetResult(process.ExitCode);
                    process.Dispose();
                };

                bool started = process.Start();
                if (!started)
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
        /// <summary>
        /// Ask S3 for the total size and object count of an archive by running
        /// "aws s3 ls ... --recursive --summarize" and parsing the summary lines.
        /// </summary>
        private async Task<(long totalBytes, long totalObjects)> GetArchiveStatsAsync(string archiveName)
        {
            long size = 0;
            long count = -1; // -1 = unknown

            string bucketPath = $"s3://{BucketName}/{archiveName}/";

            await RunAwsCommandAsync(
                $"s3 ls \"{bucketPath}\" --recursive --summarize",
                line =>
                {
                    if (line == null)
                        return;

                    if (line.Contains("Total Size:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && long.TryParse(parts[^1], out long parsedSize))
                        {
                            size = parsedSize;
                        }
                    }
                    else if (line.Contains("Total Objects:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && long.TryParse(parts[^1], out long parsedCount))
                        {
                            count = parsedCount;
                        }
                    }
                });

            return (size, count);
        }

        private string FormatBytes(long bytes)
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

        private void AppendLog(TextBox target, string line)
        {
            if (target == null || line == null) return;

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
    }
}
