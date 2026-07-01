using System.Diagnostics;
using System.Text;

namespace OffsetDiffFinder.Gui;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox oldExeBox = new();
    private readonly TextBox newExeBox = new();
    private readonly TextBox oldIniBox = new();
    private readonly TextBox outputIniBox = new();
    private readonly TextBox reportBox = new();
    private readonly TextBox logBox = new();
    private readonly Button runButton = new();
    private readonly Button openOutputButton = new();
    private readonly Button openReportButton = new();

    public MainForm()
    {
        Text = "MySEQ Offset Diff Finder";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 560);
        Size = new Size(900, 640);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 52,
            Text = "Compare an old EQ executable and old MySEQ offsets against a newer EQ executable.",
            ForeColor = Color.FromArgb(34, 41, 51),
            Font = new Font("Segoe UI Semibold", 11F)
        };
        root.Controls.Add(header, 0, 0);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            Padding = new Padding(0, 0, 0, 12)
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        root.Controls.Add(fields, 0, 1);

        AddPathRow(fields, 0, "Old eqgame", oldExeBox, "Browse...", BrowseExe);
        AddPathRow(fields, 1, "New eqgame", newExeBox, "Browse...", BrowseExe);
        AddPathRow(fields, 2, "Old offsets", oldIniBox, "Browse...", BrowseIni);
        AddPathRow(fields, 3, "Candidate ini", outputIniBox, "Save as...", BrowseSaveIni);
        AddPathRow(fields, 4, "Report", reportBox, "Save as...", BrowseSaveReport);

        oldIniBox.TextChanged += (_, _) => UpdateDefaultOutputs();
        newExeBox.TextChanged += (_, _) => UpdateDefaultOutputs();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(126, 2, 0, 8)
        };
        runButton.Text = "Find Offsets";
        runButton.Width = 120;
        runButton.Height = 30;
        runButton.Click += async (_, _) => await RunFinderAsync();
        openOutputButton.Text = "Open Ini";
        openOutputButton.Width = 92;
        openOutputButton.Height = 30;
        openOutputButton.Enabled = false;
        openOutputButton.Click += (_, _) => OpenFile(outputIniBox.Text);
        openReportButton.Text = "Open Report";
        openReportButton.Width = 104;
        openReportButton.Height = 30;
        openReportButton.Enabled = false;
        openReportButton.Click += (_, _) => OpenFile(reportBox.Text);
        actions.Controls.AddRange(new Control[] { runButton, openOutputButton, openReportButton });
        fields.Controls.Add(actions, 1, 5);
        fields.SetColumnSpan(actions, 2);

        logBox.Dock = DockStyle.Fill;
        logBox.Multiline = true;
        logBox.ScrollBars = ScrollBars.Both;
        logBox.ReadOnly = true;
        logBox.BackColor = Color.FromArgb(31, 36, 43);
        logBox.ForeColor = Color.FromArgb(232, 238, 244);
        logBox.Font = new Font("Consolas", 9F);
        root.Controls.Add(logBox, 0, 2);
    }

    private static void AddPathRow(TableLayoutPanel table, int row, string label, TextBox box, string buttonText, Func<string?> browse)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var labelControl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(64, 72, 83)
        };

        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(0, 4, 8, 4);

        var button = new Button
        {
            Text = buttonText,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 0, 3)
        };
        button.Click += (_, _) =>
        {
            string? selected = browse();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                box.Text = selected;
            }
        };

        table.Controls.Add(labelControl, 0, row);
        table.Controls.Add(box, 1, row);
        table.Controls.Add(button, 2, row);
    }

    private static string? BrowseExe()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select eqgame.exe"
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    private static string? BrowseIni()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            Title = "Select old MySEQ server INI"
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    private static string? BrowseSaveIni()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            FileName = "myseqserver.candidates.ini",
            Title = "Save candidate INI"
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    private static string? BrowseSaveReport()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = "offset-diff-report.txt",
            Title = "Save report"
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    private void UpdateDefaultOutputs()
    {
        if (string.IsNullOrWhiteSpace(oldIniBox.Text))
        {
            return;
        }

        string directory = Path.GetDirectoryName(Path.GetFullPath(oldIniBox.Text)) ?? Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(newExeBox.Text))
        {
            string newExeDirectory = Path.GetDirectoryName(Path.GetFullPath(newExeBox.Text)) ?? "";
            if (Directory.Exists(newExeDirectory))
            {
                directory = newExeDirectory;
            }
        }

        if (string.IsNullOrWhiteSpace(outputIniBox.Text))
        {
            outputIniBox.Text = Path.Combine(directory, "myseqserver.candidates.ini");
        }

        if (string.IsNullOrWhiteSpace(reportBox.Text))
        {
            reportBox.Text = Path.Combine(directory, "offset-diff-report.txt");
        }
    }

    private async Task RunFinderAsync()
    {
        if (!ValidateInputs())
        {
            return;
        }

        string toolPath = Path.Combine(AppContext.BaseDirectory, "offset-diff-finder.exe");
        if (!File.Exists(toolPath))
        {
            MessageBox.Show(this, $"Could not find:\r\n{toolPath}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        runButton.Enabled = false;
        openOutputButton.Enabled = false;
        openReportButton.Enabled = false;
        logBox.Clear();
        AppendLog("Running offset comparison...\r\n");

        var args = new[]
        {
            "--old-exe", oldExeBox.Text,
            "--new-exe", newExeBox.Text,
            "--old-ini", oldIniBox.Text,
            "--out", outputIniBox.Text,
            "--report", reportBox.Text
        };

        try
        {
            var result = await Task.Run(() => RunProcess(toolPath, args));
            AppendLog(result.Output);
            AppendLog($"\r\nExit code: {result.ExitCode}\r\n");

            bool outputExists = File.Exists(outputIniBox.Text);
            bool reportExists = File.Exists(reportBox.Text);
            openOutputButton.Enabled = outputExists;
            openReportButton.Enabled = reportExists;

            MessageBox.Show(
                this,
                result.ExitCode == 0
                    ? "Offset comparison finished."
                    : "Offset comparison finished, but some offsets need review.",
                Text,
                MessageBoxButtons.OK,
                result.ExitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            AppendLog(ex.ToString());
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            runButton.Enabled = true;
        }
    }

    private bool ValidateInputs()
    {
        if (!File.Exists(oldExeBox.Text))
        {
            MessageBox.Show(this, "Select the old eqgame.exe.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!File.Exists(newExeBox.Text))
        {
            MessageBox.Show(this, "Select the new eqgame.exe.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!File.Exists(oldIniBox.Text))
        {
            MessageBox.Show(this, "Select the old MySEQ server INI.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputIniBox.Text) || string.IsNullOrWhiteSpace(reportBox.Text))
        {
            MessageBox.Show(this, "Choose where to save the candidate INI and report.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private void AppendLog(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(text));
            return;
        }

        logBox.AppendText(text);
    }

    private static (int ExitCode, string Output) RunProcess(string toolPath, string[] args)
    {
        var output = new StringBuilder();
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = toolPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return (process.ExitCode, output.ToString());
    }

    private static void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
