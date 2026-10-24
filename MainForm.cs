using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;

namespace InstaOrg;

public sealed class MainForm : Form
{
    private readonly TextBox sourceBox = new();
    private readonly TextBox destinationBox = new();
    private readonly Button chooseSource = new();
    private readonly Button chooseDestination = new();
    private readonly Button organizeButton = new();
    private readonly ProgressBar progress = new();
    private readonly Label status = new();
    private readonly ListBox log = new();

    public MainForm()
    {
        Text = "InstaOrg";
        MinimumSize = new Size(760, 600);
        Size = new Size(900, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 22, 39);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        BuildUi();
    }

    private void BuildUi()
    {
        var heading = new Label {
            Text = "InstaOrg", AutoSize = true, Font = new Font("Segoe UI", 27, FontStyle.Bold),
            ForeColor = Color.FromArgb(101, 214, 255), Location = new Point(28, 20)
        };
        Controls.Add(heading);
        Controls.Add(new Label {
            Text = "Your files. Organized instantly.", AutoSize = true,
            ForeColor = Color.FromArgb(190, 207, 230), Location = new Point(32, 72)
        });

        AddLabel("1. Choose a folder to scan", 32, 125);
        sourceBox.SetBounds(32, 155, 660, 34);
        StyleTextBox(sourceBox);
        chooseSource.Text = "Browse…";
        chooseSource.SetBounds(704, 155, 150, 34);
        StyleButton(chooseSource, false);
        chooseSource.Click += (_, _) => BrowseForSource();
        Controls.Add(sourceBox);
        Controls.Add(chooseSource);

        AddLabel("2. Choose where the Organized Files folder should be created", 32, 215);
        destinationBox.SetBounds(32, 245, 660, 34);
        StyleTextBox(destinationBox);
        chooseDestination.Text = "Browse…";
        chooseDestination.SetBounds(704, 245, 150, 34);
        StyleButton(chooseDestination, false);
        chooseDestination.Click += (_, _) => BrowseForDestination();
        Controls.Add(destinationBox);
        Controls.Add(chooseDestination);

        var safety = new Label {
            Text = "SAFE MODE: InstaOrg copies files and leaves your originals untouched.",
            AutoSize = true, ForeColor = Color.FromArgb(111, 239, 202),
            Location = new Point(32, 300)
        };
        Controls.Add(safety);

        organizeButton.Text = "Organize My Files";
        organizeButton.SetBounds(32, 340, 220, 46);
        StyleButton(organizeButton, true);
        organizeButton.Click += async (_, _) => await OrganizeAsync();
        Controls.Add(organizeButton);

        progress.SetBounds(32, 405, 822, 18);
        progress.Style = ProgressBarStyle.Continuous;
        Controls.Add(progress);

        status.Text = "Ready. Choose a source folder to begin.";
        status.AutoSize = true;
        status.Location = new Point(32, 435);
        status.ForeColor = Color.FromArgb(190, 207, 230);
        Controls.Add(status);

        log.SetBounds(32, 465, 822, 145);
        log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        log.BackColor = Color.FromArgb(22, 35, 56);
        log.ForeColor = Color.White;
        log.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(log);
    }

    private void AddLabel(string text, int x, int y) =>
        Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = Color.White });

    private static void StyleTextBox(TextBox box)
    {
        box.BackColor = Color.FromArgb(22, 35, 56);
        box.ForeColor = Color.White;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = primary ? Color.FromArgb(61, 126, 255) : Color.FromArgb(37, 56, 83);
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
    }

    private void BrowseForSource()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose the folder InstaOrg should scan" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            sourceBox.Text = dialog.SelectedPath;
    }

    private void BrowseForDestination()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose where Organized Files should be created" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            destinationBox.Text = dialog.SelectedPath;
    }

    private async Task OrganizeAsync()
    {
        var source = sourceBox.Text.Trim();
        var destinationParent = destinationBox.Text.Trim();

        if (!Directory.Exists(source))
        {
            MessageBox.Show(this, "Please choose an existing source folder.", "InstaOrg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!Directory.Exists(destinationParent))
        {
            MessageBox.Show(this, "Please choose an existing destination folder.", "InstaOrg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (IsSameOrChild(destinationParent, source))
        {
            MessageBox.Show(this, "Choose a destination outside the source folder so InstaOrg does not scan its own output.", "Choose another destination", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var output = Path.Combine(destinationParent, "Organized Files");
        if (Directory.Exists(output) && IsSameOrChild(source, output))
        {
            MessageBox.Show(this, "The output folder is inside the source folder. Choose a different destination.", "Choose another destination", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        organizeButton.Enabled = chooseSource.Enabled = chooseDestination.Enabled = false;
        progress.Value = 0;
        log.Items.Clear();
        try
        {
            var files = await Task.Run(() => Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
                .Where(f => !IsInsideDirectory(f, output))
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToList());

            if (files.Count == 0)
            {
                status.Text = "No files found in that folder.";
                return;
            }

            Directory.CreateDirectory(output);
            progress.Maximum = files.Count;
            int copied = 0, failed = 0;

            foreach (var file in files)
            {
                try
                {
                    var category = GetCategory(Path.GetExtension(file));
                    var targetFolder = Path.Combine(output, category);
                    Directory.CreateDirectory(targetFolder);
                    var target = GetUniquePath(targetFolder, Path.GetFileName(file));
                    await Task.Run(() => File.Copy(file, target, false));
                    copied++;
                    log.Items.Add("✓ " + Path.GetFileName(file) + "  →  " + category);
                }
                catch (Exception ex)
                {
                    failed++;
                    log.Items.Add("Could not copy: " + Path.GetFileName(file) + " (" + ex.Message + ")");
                }
                progress.Value = Math.Min(progress.Maximum, copied + failed);
                status.Text = $"Processed {copied + failed} of {files.Count} files…";
                if (log.Items.Count > 0) log.TopIndex = log.Items.Count - 1;
                await Task.Yield();
            }

            status.Text = $"Finished! Copied {copied} files; {failed} could not be copied.";
            log.Items.Add("Output: " + output);
            MessageBox.Show(this, $"Finished!\nCopied: {copied}\nCould not copy: {failed}\n\nYour original files were not moved or deleted.", "InstaOrg complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            status.Text = "Something went wrong.";
            MessageBox.Show(this, ex.Message, "InstaOrg error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            organizeButton.Enabled = chooseSource.Enabled = chooseDestination.Enabled = true;
        }
    }

    private static string GetCategory(string ext)
    {
        ext = ext.ToLowerInvariant();
        if (new[] { ".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg", ".wma" }.Contains(ext)) return "Music";
        if (new[] { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".webm" }.Contains(ext)) return "Videos";
        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".heic" }.Contains(ext)) return "Pictures";
        if (new[] { ".txt", ".md", ".rtf", ".note", ".doc", ".docx", ".odt" }.Contains(ext)) return "Notes & Documents";
        if (new[] { ".pdf", ".ppt", ".pptx", ".xls", ".xlsx", ".csv" }.Contains(ext)) return "Office & PDF";
        if (new[] { ".zip", ".7z", ".rar", ".tar", ".gz" }.Contains(ext)) return "Archives";
        if (new[] { ".exe", ".msi", ".bat", ".cmd", ".ps1" }.Contains(ext)) return "Programs";
        return "Other Files";
    }

    private static string GetUniquePath(string folder, string fileName)
    {
        var path = Path.Combine(folder, fileName);
        if (!File.Exists(path)) return path;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        int i = 2;
        do { path = Path.Combine(folder, $"{name} ({i++}){ext}"); } while (File.Exists(path));
        return path;
    }

    private static bool IsInsideDirectory(string file, string directory)
    {
        if (!Directory.Exists(directory)) return false;
        var fullFile = Path.GetFullPath(file);
        var fullDir = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullFile.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameOrChild(string candidate, string parent)
    {
        var c = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar);
        var p = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar);
        return c.Equals(p, StringComparison.OrdinalIgnoreCase) ||
               c.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
