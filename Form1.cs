using System.Diagnostics;
using System.Text.Json;

namespace UniversalModManager;

public partial class Form1 : Form
{
    // ========== DATA MODELS ==========
    private class Fix
    {
        public string Name { get; set; } = "";
        public string FolderPath { get; set; } = "";
    }

    private class Game
    {
        public string Name { get; set; } = "";
        public string GamePath { get; set; } = "";
        public string ExePath { get; set; } = "";
        public List<Fix> Fixes { get; set; } = new();
    }

    private class ConfigData
    {
        public List<Game> Games { get; set; } = new();
    }

    private ConfigData config = new();
    private readonly string configPath;

    private readonly Dictionary<string, string> createdSymlinks = new();
    private string? tempBackupPath;

    // ========== UI CONTROLS ==========
    private ListBox lstGames = null!;
    private ListBox lstFixes = null!;
    private TextBox txtGameName = null!;
    private TextBox txtGamePath = null!;
    private TextBox txtGameExe = null!;
    private TextBox txtFixName = null!;
    private TextBox txtFixPath = null!;
    private Button btnAddGame = null!;
    private Button btnEditGame = null!;
    private Button btnRemoveGame = null!;
    private Button btnAddFix = null!;
    private Button btnEditFix = null!;
    private Button btnRemoveFix = null!;
    private Button btnLaunch = null!;
    private Button btnRestoreBackups = null!;
    private Label lblStatus = null!;

    public Form1()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folder = Path.Combine(appData, "UniversalModManager");
        Directory.CreateDirectory(folder);
        configPath = Path.Combine(folder, "config.json");

        LoadConfig();
        InitializeComponent();
        RefreshGamesList();
    }

    private void LoadConfig()
    {
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
        }
        else
        {
            config = new ConfigData();
        }
    }

    private void SaveConfig()
    {
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    private void InitializeComponent()
    {
        this.Text = "Universal Mod Manager - Virtual Layer for Games";
        this.Size = new Size(900, 650);
        this.MinimumSize = new Size(800, 550);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Usamos un TableLayoutPanel principal de 2 columnas
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // fila inferior para botones

        // ========== COLUMNA IZQUIERDA: JUEGOS ==========
        Panel leftPanel = new Panel { Dock = DockStyle.Fill };
        Label lblGames = new Label { Text = "Configured Games:", Location = new Point(5, 5), AutoSize = true };
        lstGames = new ListBox
        {
            Location = new Point(5, 30),
            Width = leftPanel.Width - 10,
            Height = 180,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            DisplayMember = "Name"
        };
        lstGames.SelectedIndexChanged += LstGames_SelectedIndexChanged;

        GroupBox grpGame = new GroupBox
        {
            Text = "Game Details",
            Location = new Point(5, 220),
            Width = leftPanel.Width - 10,
            Height = 220,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Tabla interna para los campos del juego (3 columnas: label, textbox, button)
        TableLayoutPanel gameTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(5)
        };
        gameTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        gameTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        gameTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));

        // Name
        gameTable.Controls.Add(new Label { Text = "Name:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
        txtGameName = new TextBox { Dock = DockStyle.Fill };
        gameTable.Controls.Add(txtGameName, 1, 0);

        // Game folder
        gameTable.Controls.Add(new Label { Text = "Game folder:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
        txtGamePath = new TextBox { Dock = DockStyle.Fill };
        gameTable.Controls.Add(txtGamePath, 1, 1);
        Button btnBrowseGame = new Button { Text = "...", Dock = DockStyle.Fill };
        btnBrowseGame.Click += (s, e) => { using (var fbd = new FolderBrowserDialog()) if (fbd.ShowDialog() == DialogResult.OK) txtGamePath.Text = fbd.SelectedPath; };
        gameTable.Controls.Add(btnBrowseGame, 2, 1);

        // Executable
        gameTable.Controls.Add(new Label { Text = "Executable:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
        txtGameExe = new TextBox { Dock = DockStyle.Fill };
        gameTable.Controls.Add(txtGameExe, 1, 2);
        Button btnBrowseExe = new Button { Text = "...", Dock = DockStyle.Fill };
        btnBrowseExe.Click += BtnBrowseExe_Click;
        gameTable.Controls.Add(btnBrowseExe, 2, 2);

        // Botones Add/Edit/Remove
        FlowLayoutPanel gameButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        btnAddGame = new Button { Text = "Add", Width = 80 };
        btnEditGame = new Button { Text = "Edit", Width = 80 };
        btnRemoveGame = new Button { Text = "Remove", Width = 80 };
        btnAddGame.Click += BtnAddGame_Click;
        btnEditGame.Click += BtnEditGame_Click;
        btnRemoveGame.Click += BtnRemoveGame_Click;
        gameButtons.Controls.AddRange(new Control[] { btnAddGame, btnEditGame, btnRemoveGame });
        gameTable.SetColumnSpan(gameButtons, 2);
        gameTable.Controls.Add(gameButtons, 1, 3);

        grpGame.Controls.Add(gameTable);
        leftPanel.Controls.AddRange(new Control[] { lblGames, lstGames, grpGame });
        mainLayout.Controls.Add(leftPanel, 0, 0);

        // ========== COLUMNA DERECHA: FIXES ==========
        Panel rightPanel = new Panel { Dock = DockStyle.Fill };
        Label lblFixes = new Label { Text = "Fixes for selected game:", Location = new Point(5, 5), AutoSize = true };
        lstFixes = new ListBox
        {
            Location = new Point(5, 30),
            Width = rightPanel.Width - 10,
            Height = 180,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            DisplayMember = "Name"
        };
        lstFixes.SelectedIndexChanged += LstFixes_SelectedIndexChanged;

        GroupBox grpFix = new GroupBox
        {
            Text = "Fix Details",
            Location = new Point(5, 220),
            Width = rightPanel.Width - 10,
            Height = 220,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        TableLayoutPanel fixTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            Padding = new Padding(5)
        };
        fixTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        fixTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fixTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));

        // Name
        fixTable.Controls.Add(new Label { Text = "Name:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
        txtFixName = new TextBox { Dock = DockStyle.Fill };
        fixTable.Controls.Add(txtFixName, 1, 0);

        // Fix folder
        fixTable.Controls.Add(new Label { Text = "Fix folder:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
        txtFixPath = new TextBox { Dock = DockStyle.Fill };
        fixTable.Controls.Add(txtFixPath, 1, 1);
        Button btnBrowseFix = new Button { Text = "...", Dock = DockStyle.Fill };
        btnBrowseFix.Click += (s, e) => { using (var fbd = new FolderBrowserDialog()) if (fbd.ShowDialog() == DialogResult.OK) txtFixPath.Text = fbd.SelectedPath; };
        fixTable.Controls.Add(btnBrowseFix, 2, 1);

        // Botones Add/Edit/Remove
        FlowLayoutPanel fixButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        btnAddFix = new Button { Text = "Add", Width = 80 };
        btnEditFix = new Button { Text = "Edit", Width = 80 };
        btnRemoveFix = new Button { Text = "Remove", Width = 80 };
        btnAddFix.Click += BtnAddFix_Click;
        btnEditFix.Click += BtnEditFix_Click;
        btnRemoveFix.Click += BtnRemoveFix_Click;
        fixButtons.Controls.AddRange(new Control[] { btnAddFix, btnEditFix, btnRemoveFix });
        fixTable.SetColumnSpan(fixButtons, 2);
        fixTable.Controls.Add(fixButtons, 1, 2);

        grpFix.Controls.Add(fixTable);
        rightPanel.Controls.AddRange(new Control[] { lblFixes, lstFixes, grpFix });
        mainLayout.Controls.Add(rightPanel, 1, 0);

        // ========== FILA INFERIOR: BOTONES Y ESTADO ==========
        Panel bottomPanel = new Panel { Dock = DockStyle.Fill };
        TableLayoutPanel bottomTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0)
        };
        bottomTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bottomTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
        bottomTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        bottomTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

        btnLaunch = new Button
        {
            Text = "▶ Apply Virtual Layer & Launch",
            Dock = DockStyle.Fill,
            BackColor = Color.LightGreen,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnLaunch.Click += BtnLaunch_Click;

        btnRestoreBackups = new Button
        {
            Text = "♻ Restore Orphaned Backups",
            Dock = DockStyle.Fill,
            BackColor = Color.LightCoral
        };
        btnRestoreBackups.Click += BtnRestoreBackups_Click;

        lblStatus = new Label
        {
            Text = "Ready",
            ForeColor = Color.DarkBlue,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        bottomTable.Controls.Add(btnLaunch, 0, 0);
        bottomTable.Controls.Add(btnRestoreBackups, 1, 0);
        bottomTable.SetColumnSpan(lblStatus, 2);
        bottomTable.Controls.Add(lblStatus, 0, 1);

        bottomPanel.Controls.Add(bottomTable);
        mainLayout.Controls.Add(bottomPanel, 0, 1);
        mainLayout.SetColumnSpan(bottomPanel, 2);

        this.Controls.Add(mainLayout);
    }

    // ========== MÉTODOS AUXILIARES ==========
    private void RefreshGamesList()
    {
        lstGames.DataSource = null;
        lstGames.DataSource = config.Games;
        if (config.Games.Count > 0)
            lstGames.SelectedIndex = 0;
        else
            ClearGameDetails();
    }

    private void ClearGameDetails()
    {
        txtGameName.Text = "";
        txtGamePath.Text = "";
        txtGameExe.Text = "";
        lstFixes.DataSource = null;
    }

    private void RefreshFixesList(Game game)
    {
        lstFixes.DataSource = null;
        if (game != null)
            lstFixes.DataSource = game.Fixes;
    }

    private Game? CurrentGame => lstGames.SelectedItem as Game;
    private Fix? CurrentFix => lstFixes.SelectedItem as Fix;

    private void LstGames_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var game = CurrentGame;
        if (game != null)
        {
            txtGameName.Text = game.Name;
            txtGamePath.Text = game.GamePath;
            txtGameExe.Text = game.ExePath;
            RefreshFixesList(game);
        }
        else
        {
            ClearGameDetails();
        }
    }

    private void LstFixes_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var fix = CurrentFix;
        if (fix != null)
        {
            txtFixName.Text = fix.Name;
            txtFixPath.Text = fix.FolderPath;
        }
        else
        {
            txtFixName.Text = "";
            txtFixPath.Text = "";
        }
    }

    private void BtnBrowseExe_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
        ofd.Title = "Select the game executable";
        if (!string.IsNullOrEmpty(txtGamePath.Text) && Directory.Exists(txtGamePath.Text))
            ofd.InitialDirectory = txtGamePath.Text;
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            string exePath = ofd.FileName;
            if (!string.IsNullOrEmpty(txtGamePath.Text) && exePath.StartsWith(txtGamePath.Text, StringComparison.OrdinalIgnoreCase))
                txtGameExe.Text = Path.GetRelativePath(txtGamePath.Text, exePath);
            else
                txtGameExe.Text = exePath;
        }
    }

    // ========== OPERACIONES CON JUEGOS ==========
    private void BtnAddGame_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtGameName.Text) || string.IsNullOrWhiteSpace(txtGamePath.Text))
        {
            MessageBox.Show("Game name and folder are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!Directory.Exists(txtGamePath.Text))
        {
            MessageBox.Show("Game folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var game = new Game
        {
            Name = txtGameName.Text.Trim(),
            GamePath = txtGamePath.Text.Trim(),
            ExePath = txtGameExe.Text.Trim(),
            Fixes = new List<Fix>()
        };
        config.Games.Add(game);
        SaveConfig();
        RefreshGamesList();
        lstGames.SelectedItem = game;
    }

    private void BtnEditGame_Click(object? sender, EventArgs e)
    {
        var game = CurrentGame;
        if (game == null) return;
        if (string.IsNullOrWhiteSpace(txtGameName.Text) || string.IsNullOrWhiteSpace(txtGamePath.Text))
        {
            MessageBox.Show("Name and folder are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        game.Name = txtGameName.Text.Trim();
        game.GamePath = txtGamePath.Text.Trim();
        game.ExePath = txtGameExe.Text.Trim();
        SaveConfig();
        RefreshGamesList();
        lstGames.SelectedItem = game;
    }

    private void BtnRemoveGame_Click(object? sender, EventArgs e)
    {
        var game = CurrentGame;
        if (game == null) return;
        if (MessageBox.Show($"Delete game '{game.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            config.Games.Remove(game);
            SaveConfig();
            RefreshGamesList();
        }
    }

    // ========== OPERACIONES CON FIXES ==========
    private void BtnAddFix_Click(object? sender, EventArgs e)
    {
        var game = CurrentGame;
        if (game == null)
        {
            MessageBox.Show("Select a game first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(txtFixName.Text) || string.IsNullOrWhiteSpace(txtFixPath.Text))
        {
            MessageBox.Show("Fix name and folder are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!Directory.Exists(txtFixPath.Text))
        {
            MessageBox.Show("Fix folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var fix = new Fix { Name = txtFixName.Text.Trim(), FolderPath = txtFixPath.Text.Trim() };
        game.Fixes.Add(fix);
        SaveConfig();
        RefreshFixesList(game);
        lstFixes.SelectedItem = fix;
    }

    private void BtnEditFix_Click(object? sender, EventArgs e)
    {
        var game = CurrentGame;
        var fix = CurrentFix;
        if (game == null || fix == null) return;
        if (string.IsNullOrWhiteSpace(txtFixName.Text) || string.IsNullOrWhiteSpace(txtFixPath.Text))
        {
            MessageBox.Show("Name and folder are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        fix.Name = txtFixName.Text.Trim();
        fix.FolderPath = txtFixPath.Text.Trim();
        SaveConfig();
        RefreshFixesList(game);
        lstFixes.SelectedItem = fix;
    }

    private void BtnRemoveFix_Click(object? sender, EventArgs e)
    {
        var game = CurrentGame;
        var fix = CurrentFix;
        if (game == null || fix == null) return;
        if (MessageBox.Show($"Delete fix '{fix.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            game.Fixes.Remove(fix);
            SaveConfig();
            RefreshFixesList(game);
        }
    }

    // ========== LÓGICA PRINCIPAL ==========
    private async void BtnLaunch_Click(object? sender, EventArgs e)
    {
        var game = CurrentGame;
        var fix = CurrentFix;
        if (game == null || fix == null)
        {
            MessageBox.Show("Select a game and a fix first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(game.GamePath))
        {
            MessageBox.Show($"Game folder does not exist:\n{game.GamePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!Directory.Exists(fix.FolderPath))
        {
            MessageBox.Show($"Fix folder does not exist:\n{fix.FolderPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string exeToLaunch = game.ExePath;
        if (string.IsNullOrWhiteSpace(exeToLaunch))
        {
            var exeFiles = Directory.GetFiles(game.GamePath, "*.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length == 0)
            {
                MessageBox.Show("No .exe found in game folder. Please specify the executable manually.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            exeToLaunch = exeFiles[0];
        }
        else if (!Path.IsPathRooted(exeToLaunch))
        {
            exeToLaunch = Path.Combine(game.GamePath, exeToLaunch);
        }

        if (!File.Exists(exeToLaunch))
        {
            MessageBox.Show($"Executable does not exist:\n{exeToLaunch}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        btnLaunch.Enabled = false;
        lblStatus.Text = "Applying virtual layer (creating symlinks)...";
        lblStatus.ForeColor = Color.Orange;
        Application.DoEvents();

        try
        {
            bool success = await ApplyVirtualLayer(game.GamePath, fix.FolderPath);
            if (!success)
            {
                lblStatus.Text = "Failed to apply virtual layer. See error messages.";
                lblStatus.ForeColor = Color.Red;
                return;
            }

            lblStatus.Text = "Virtual layer active. Launching game...";
            Application.DoEvents();

            var process = Process.Start(new ProcessStartInfo(exeToLaunch)
            {
                WorkingDirectory = Path.GetDirectoryName(exeToLaunch),
                UseShellExecute = true
            });
            if (process != null)
                await process.WaitForExitAsync();

            lblStatus.Text = "Game closed. Restoring original files...";
            Application.DoEvents();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            CleanupSymlinksAndBackups(game.GamePath);
            lblStatus.Text = "Original files restored. Ready.";
            lblStatus.ForeColor = Color.Green;
            btnLaunch.Enabled = true;
        }
    }

    private Task<bool> ApplyVirtualLayer(string gamePath, string fixPath)
    {
        tempBackupPath = Path.Combine(Path.GetTempPath(), "UniversalModManager_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempBackupPath);
        createdSymlinks.Clear();
        File.WriteAllText(Path.Combine(tempBackupPath, ".gamepath"), gamePath);

        var fixFiles = Directory.GetFiles(fixPath, "*", SearchOption.AllDirectories);

        foreach (string fixFile in fixFiles)
        {
            string relative = Path.GetRelativePath(fixPath, fixFile);
            string target = Path.Combine(gamePath, relative);
            string backup = Path.Combine(tempBackupPath, relative);

            if (File.Exists(target))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                File.Move(target, backup);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            try
            {
                File.CreateSymbolicLink(target, fixFile);
                createdSymlinks[target] = backup;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot create symbolic link for:\n{relative}\nError: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanupSymlinksAndBackups(gamePath);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    private void CleanupSymlinksAndBackups(string gamePath)
    {
        foreach (string symlink in createdSymlinks.Keys)
        {
            if (File.Exists(symlink))
            {
                try { File.Delete(symlink); } catch { }
            }
        }

        if (tempBackupPath != null && Directory.Exists(tempBackupPath))
        {
            var backupFiles = Directory.GetFiles(tempBackupPath, "*", SearchOption.AllDirectories);
            foreach (string backupFile in backupFiles)
            {
                if (Path.GetFileName(backupFile) == ".gamepath") continue;
                string relative = Path.GetRelativePath(tempBackupPath, backupFile);
                string originalDest = Path.Combine(gamePath, relative);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalDest)!);
                    File.Move(backupFile, originalDest, true);
                }
                catch { }
            }

            try { Directory.Delete(tempBackupPath, true); } catch { }
            tempBackupPath = null;
        }

        createdSymlinks.Clear();
    }

    // ========== CRASH RECOVERY ==========
    private void BtnRestoreBackups_Click(object? sender, EventArgs e)
    {
        string tempFolder = Path.GetTempPath();
        var orphanedBackups = Directory.GetDirectories(tempFolder, "UniversalModManager_*")
                                       .Where(dir => !IsBackupFromCurrentSession(dir))
                                       .ToList();

        if (orphanedBackups.Count == 0)
        {
            MessageBox.Show("No orphaned backups found. All temporary layers are clean.", "Crash Recovery", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using (var form = new Form())
        {
            form.Text = "Crash Recovery - Restore missing original files";
            form.Size = new Size(600, 400);
            form.StartPosition = FormStartPosition.CenterParent;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;

            Label lbl = new Label() { Text = "Select a backup to restore (usually the most recent one):", Location = new Point(12, 12), AutoSize = true };
            ListBox list = new ListBox() { Location = new Point(12, 40), Size = new Size(560, 250) };
            foreach (var dir in orphanedBackups)
            {
                string gamePathFile = Path.Combine(dir, ".gamepath");
                string gamePath = File.Exists(gamePathFile) ? File.ReadAllText(gamePathFile) : "Unknown";
                string dirName = Path.GetFileName(dir);
                string creation = Directory.GetCreationTime(dir).ToString();
                list.Items.Add($"{dirName} | Game: {gamePath} | Created: {creation}");
            }
            list.SelectedIndex = 0;

            Button btnRestore = new Button() { Text = "Restore selected", Location = new Point(12, 310), Size = new Size(150, 40) };
            Button btnCancel = new Button() { Text = "Cancel", Location = new Point(180, 310), Size = new Size(100, 40) };
            btnCancel.DialogResult = DialogResult.Cancel;
            btnRestore.DialogResult = DialogResult.OK;
            form.Controls.AddRange(new Control[] { lbl, list, btnRestore, btnCancel });

            if (form.ShowDialog() == DialogResult.OK && list.SelectedIndex >= 0)
            {
                string selectedBackup = orphanedBackups[list.SelectedIndex];
                string backupGamePath = File.Exists(Path.Combine(selectedBackup, ".gamepath"))
                    ? File.ReadAllText(Path.Combine(selectedBackup, ".gamepath"))
                    : "";

                string restoreGamePath = backupGamePath;
                if (string.IsNullOrEmpty(restoreGamePath) || !Directory.Exists(restoreGamePath))
                {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.Description = "Select the game folder where files should be restored";
                        fbd.ShowNewFolderButton = false;
                        if (fbd.ShowDialog() != DialogResult.OK)
                            return;
                        restoreGamePath = fbd.SelectedPath;
                    }
                }

                try
                {
                    int restored = 0;
                    var backupFiles = Directory.GetFiles(selectedBackup, "*", SearchOption.AllDirectories);
                    foreach (string backupFile in backupFiles)
                    {
                        if (Path.GetFileName(backupFile) == ".gamepath") continue;
                        string relative = Path.GetRelativePath(selectedBackup, backupFile);
                        string target = Path.Combine(restoreGamePath, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                        File.Move(backupFile, target, true);
                        restored++;
                    }
                    Directory.Delete(selectedBackup, true);
                    MessageBox.Show($"Restored {restored} files to:\n{restoreGamePath}", "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    lblStatus.Text = $"Restored backup from {Path.GetFileName(selectedBackup)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during restore: {ex.Message}", "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    private bool IsBackupFromCurrentSession(string backupDir)
    {
        if (tempBackupPath != null && backupDir.Equals(tempBackupPath, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}