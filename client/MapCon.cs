// Class Files

using myseq.Properties;
using Structures;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace myseq
{
    public class MapCon : UserControl
    {
        // Events
        public delegate void SelectPointHandler(Spawninfo playerinfo, double selectedX, double selectedY);

        private Timer tooltipTimer;
        private Timer clockTimer;

        public event SelectPointHandler SelectPoint; // Fires when the user clicks the map (without a mob)

        protected void OnSelectPoint(Spawninfo playerinfo, double selectedX, double selectedY) => SelectPoint?.Invoke(playerinfo, selectedX, selectedY);

        private readonly System.ComponentModel.Container components = null;

        private Label MobInfoLabel;
        private Panel selectionInfoCard;
        private Label selectionClockLabel;
        private Label selectionBodyLabel;
        private Panel mapIconLegend;
        private FlowLayoutPanel mapControlRail;
        private FlowLayoutPanel railNpcRow;
        private FlowLayoutPanel railPcRow;
        private Button btnRailNpc;
        private Button btnRailPlayers;
        private Button btnRailNpcCorpses;
        private Button btnRailPcCorpses;
        private Button btnRailGrid;
        private Button btnRailLabels;
        private Button btnRailFollow;
        private Button btnRailTarget;
        private Button btnRailLegend;
        private Button btnRailReset;
        private ToolTip railToolTip;
        private int lastInfoSpawnId = 99999;
        private string lastInfoText = "";
        private int lastPolledListSpawnId = 99999;

        private Font drawFont = Settings.Default.MapLabel;
        private Font drawFont1 = new Font(Settings.Default.MapLabel.Name, Settings.Default.MapLabel.Size * 0.9f, Settings.Default.MapLabel.Style);
        private Font drawFont3 = new Font(Settings.Default.MapLabel.Name, Settings.Default.MapLabel.Size * 1.1f, Settings.Default.MapLabel.Style);

        // Hand relocation variables

        private Cursor CursorHand;

        private bool MouseDragging;

        private bool m_rangechange;

        private float m_dragStartX;

        private float m_dragStartY;

        private float m_dragStartPanX;

        private float m_dragStartPanY;

        private bool canShowTooltip = true;

        private int skittle;

        private int flash_count;

        // m_mapCenter - centre point of screen in Map Units.
        private PointF mapCenter;

        // screenCenter - centre point of screen in Screen Units.
        private PointF screenCenter;

        private PointF adjustment;
        private PointF gamerPos;

        // Spawn Sizes
        private int SettingsSpawnSize = 3;

        private float SpawnSize = 5.0f;

        private float SpawnSizeOffset = 2.5f;

        private float SpawnPlusSize = 7.0f;

        private float PlusSzOZ = 3.5f;

        private float SelectSize = 9.0f;

        private float SelectSizeOffset = 4.5f;

        public float scale = 1.0f;

        public float MinmapX { get; set; } = -1000;
        public float MaxMapX { get; set; } = 1000;
        public float MinMapY { get; set; } = -1000;
        public float MaxMapY { get; set; } = 1000;

        private int filterpos;
        private int filterneg;
        private int fpsCount;

        private PointF selectedPoint = new PointF(-1, -1);// [42!] Mark an arbitrary spot on the map

        private string curTarget = "";

        private readonly BufferedGraphicsContext gfxManager;

        private BufferedGraphics bkgBuffer;

        private ToolTip toolTip;

        private bool flash; // used for flashing warning lights

        private readonly SolidBrush textBrush = new SolidBrush(Color.LightGray);
        private readonly SolidBrush WhiteBrush = new SolidBrush(Color.White);
        private readonly SolidBrush MarkerShadowBrush = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
        private readonly Pen WhitePen = new Pen(new SolidBrush(Color.White));
        private readonly Pen GreenPen = new Pen(new SolidBrush(Color.Green));
        private readonly Pen RedPen = new Pen(new SolidBrush(Color.Red));
        private readonly Pen BluePen = new Pen(new SolidBrush(Color.Blue));
        private readonly Pen YellowPen = new Pen(new SolidBrush(Color.Yellow));
        private readonly Pen PurplePen = new Pen(new SolidBrush(Color.Purple));
        private readonly Pen PinkPen = new Pen(new SolidBrush(Color.Fuchsia));
        private readonly Pen HuntAlertPen = new Pen(Color.LimeGreen, 2.2f);
        private readonly Pen CautionAlertPen = new Pen(Color.Orange, 2.2f);
        private readonly Pen DangerAlertPen = new Pen(Color.Red, 2.2f);
        private readonly Pen RareAlertPen = new Pen(Color.Fuchsia, 2.2f);
        private readonly Pen LookupPen = new Pen(Color.Red, 2.4f);
        private readonly Pen SelectionPen = new Pen(ModernTheme.AccentWarm, 2.4f);
        private readonly Pen PCBorder = new Pen(new SolidBrush(Settings.Default.PCBorderColor));

        private MainForm f1;          // Caution: this may be null
        private MapPane mapPane;     // Caution: this may be null
        private EQData eq;
        private EQMap map;
        private MapData mapData;
        private SpawnColors conColor;
        private TableLayoutPanel tableLayoutPanel1;
        private bool ShowPCName = Settings.Default.ShowPCNames;

        private float[] xSin = new float[512];
        private float[] xCos = new float[512];

        public Label lblGameClock;
        public int UpdateSteps { get; set; } = 5;
        public int UpdateTicks { get; set; } = 1;
        public float PanOffsetX { get; set; }
        public float PanOffsetY { get; set; }
        public float Ratio { get; set; } = 1.0f;

        private Stopwatch fpsStopwatch = new Stopwatch();
        private double fpsValue;

        public MapCon()
        {
            InitializeComponent();
            InitializeVariables();
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            gfxManager = BufferedGraphicsManager.Current;
        }

        public void SetComponents(MainForm f1, MapPane mapPane, EQData eq, EQMap map, MapData mapData)
        {
            this.f1 = f1;
            this.mapPane = mapPane;
            this.eq = eq;
            conColor = eq.spawnColor;
            this.map = map;
            this.mapData = mapData;
            map.EnterMap += MapChanged;

            Invalidate();
        }

        public void OnResize()
        {
            if (Width > 0 && Height > 0)
            {
                bkgBuffer?.Dispose();

                gfxManager.MaximumBuffer = new Size(Width + 1, Height + 1);

                bkgBuffer = gfxManager.Allocate(CreateGraphics(), new Rectangle(0, 0, Width + 1, Height + 1));

                bkgBuffer.Graphics.SmoothingMode = SmoothingMode.None;

                PositionMapOverlays();
                Refresh();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Override Method intentionally left empty.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (bkgBuffer != null)
            {
                bkgBuffer.Render(e.Graphics);

                base.OnPaint(e);

                CalculateFPS();
                f1.toolStripFPS.Text = $"FPS: {fpsValue}";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tooltipTimer?.Dispose();
                clockTimer?.Dispose();
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        public void Tick()
        {
            UpdateInfoClock();
            PollSelectedSpawnList();
            RefreshSelectedSpawnInfoCard();

            // Increment animation and flash counters
            skittle++;
            flash_count++;

            // Re-adjust the screen/map view (optional: call less frequently if performance is an issue)
            ReAdjust();

            // Reset skittle animation step if it exceeds the number of update steps
            if (skittle >= UpdateSteps) // Consider using >= for clarity and to avoid off-by-one issues
            {
                skittle = 0;
            }

            // Toggle flash status when the flash count reaches the update tick threshold
            if (flash_count >= UpdateTicks)
            {
                flash_count = 0;  // Reset flash count
                flash = !flash;   // Toggle the flash boolean
            }
        }

        private void PollSelectedSpawnList()
        {
            if (f1?.SpawnList == null)
            {
                return;
            }

            if (!f1.SpawnList.TryGetSelectedSpawnId(out int spawnId) || spawnId == 99999 || spawnId == lastPolledListSpawnId)
            {
                return;
            }

            lastPolledListSpawnId = spawnId;
            if (f1.SpawnList.TryGetSelectedSpawnSummary(out string summary))
            {
                UpdateSelectedSpawnInfoFromList(spawnId, summary);
            }
            else
            {
                UpdateSelectedSpawnInfo();
            }
        }

        public void ClearPan()
        {
            PanOffsetX = 0;
            PanOffsetY = 0;
            ReAdjust();
        }

        private void CalculateFPS()
        {
            if (!fpsStopwatch.IsRunning)
            {
                // Start the stopwatch if it's not already running
                fpsStopwatch.Start();
            }

            // Check if the elapsed time exceeds the threshold (0.5 seconds)
            if (fpsStopwatch.Elapsed.TotalSeconds > 0.5)
            {
                // Calculate FPS
                fpsValue = Math.Round(fpsCount / fpsStopwatch.Elapsed.TotalSeconds, 2);

                // Reset for the next interval
                fpsStopwatch.Restart();
                fpsCount = 0;
            }
            else
            {
                // Increment frame count
                fpsCount++;
            }
        }

        private static string FormatInfoClock() => DateTime.Now.ToString("MMM d, yyyy h:mm tt");

        private void UpdateInfoClock()
        {
            if (lblGameClock == null && selectionClockLabel == null)
            {
                return;
            }

            string currentTime = FormatInfoClock();
            if (lblGameClock != null && !string.Equals(lblGameClock.Text, currentTime, StringComparison.Ordinal))
            {
                lblGameClock.Text = currentTime;
            }

            if (selectionClockLabel != null && !string.Equals(selectionClockLabel.Text, currentTime, StringComparison.Ordinal))
            {
                selectionClockLabel.Text = currentTime;
            }
        }

        #region Component Designer generated code

        private void InitializeComponent()

        {
            MobInfoLabel = new Label();
            selectionInfoCard = new Panel();
            selectionClockLabel = new Label();
            selectionBodyLabel = new Label();
            mapIconLegend = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            lblGameClock = new Label();
            mapControlRail = new FlowLayoutPanel();
            railNpcRow = new FlowLayoutPanel();
            railPcRow = new FlowLayoutPanel();
            btnRailNpc = new Button();
            btnRailPlayers = new Button();
            btnRailNpcCorpses = new Button();
            btnRailPcCorpses = new Button();
            btnRailGrid = new Button();
            btnRailLabels = new Button();
            btnRailFollow = new Button();
            btnRailTarget = new Button();
            btnRailLegend = new Button();
            btnRailReset = new Button();
            railToolTip = new ToolTip();
            tableLayoutPanel1.SuspendLayout();
            selectionInfoCard.SuspendLayout();
            mapIconLegend.SuspendLayout();
            mapControlRail.SuspendLayout();
            railNpcRow.SuspendLayout();
            railPcRow.SuspendLayout();
            SuspendLayout();
            tooltipTimer = new System.Windows.Forms.Timer
            {
                Interval = 250 // 250 milliseconds = 0.25 seconds
            };
            tooltipTimer.Tick += TooltipTimer_Tick;
            tooltipTimer.Start();
            clockTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            clockTimer.Tick += (_, _) => UpdateInfoClock();
            clockTimer.Start();

            //
            // lblMobInfo
            //
            MobInfoLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
            | AnchorStyles.Left
            | AnchorStyles.Right;
            MobInfoLabel.BackColor = Color.FromArgb(46, 53, 63);
            MobInfoLabel.BorderStyle = BorderStyle.None;
            MobInfoLabel.Font = Settings.Default.TargetInfoFont;
            MobInfoLabel.ForeColor = Color.FromArgb(232, 238, 244);
            MobInfoLabel.Location = new Point(0, 24);
            MobInfoLabel.Margin = new Padding(0, 2, 0, 0);
            MobInfoLabel.Name = "lblMobInfo";
            MobInfoLabel.Padding = new Padding(8, 6, 8, 6);
            MobInfoLabel.Size = new Size(220, 92);
            MobInfoLabel.TabIndex = 0;
            MobInfoLabel.Text = "Spawn Information Window";
            //
            // tableLayoutPanel1
            //
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel1.BackColor = Color.FromArgb(46, 53, 63);
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(lblGameClock, 0, 0);
            tableLayoutPanel1.Controls.Add(MobInfoLabel, 0, 1);
            tableLayoutPanel1.Location = new Point(10, 10);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(220, 118);
            tableLayoutPanel1.TabIndex = 2;
            //
            // lblGameClock
            //
            lblGameClock.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
            | AnchorStyles.Left
            | AnchorStyles.Right;
            lblGameClock.BackColor = ModernTheme.Accent;
            lblGameClock.BorderStyle = BorderStyle.None;
            lblGameClock.Font = Settings.Default.TargetInfoFont;/// new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold);
            lblGameClock.ForeColor = Color.White;
            lblGameClock.Location = new Point(0, 0);
            lblGameClock.Margin = new Padding(0);
            lblGameClock.Name = "lblGameClock";
            lblGameClock.Size = new Size(220, 24);
            lblGameClock.TabIndex = 2;
            lblGameClock.Text = FormatInfoClock();
            lblGameClock.TextAlign = ContentAlignment.MiddleCenter;
            //
            // selectionInfoCard
            //
            selectionInfoCard.BackColor = Color.FromArgb(46, 53, 63);
            selectionInfoCard.Controls.Add(selectionClockLabel);
            selectionInfoCard.Controls.Add(selectionBodyLabel);
            selectionInfoCard.Location = new Point(10, 10);
            selectionInfoCard.Name = "selectionInfoCard";
            selectionInfoCard.Padding = new Padding(0);
            selectionInfoCard.Size = new Size(260, 116);
            selectionInfoCard.TabIndex = 6;
            //
            // selectionClockLabel
            //
            selectionClockLabel.BackColor = ModernTheme.Accent;
            selectionClockLabel.Font = Settings.Default.TargetInfoFont;
            selectionClockLabel.ForeColor = Color.White;
            selectionClockLabel.Location = new Point(0, 0);
            selectionClockLabel.Name = "selectionClockLabel";
            selectionClockLabel.Size = new Size(260, 24);
            selectionClockLabel.TabIndex = 0;
            selectionClockLabel.Text = FormatInfoClock();
            selectionClockLabel.TextAlign = ContentAlignment.MiddleCenter;
            //
            // selectionBodyLabel
            //
            selectionBodyLabel.BackColor = Color.FromArgb(46, 53, 63);
            selectionBodyLabel.Font = Settings.Default.TargetInfoFont;
            selectionBodyLabel.ForeColor = Color.FromArgb(232, 238, 244);
            selectionBodyLabel.Location = new Point(0, 24);
            selectionBodyLabel.Name = "selectionBodyLabel";
            selectionBodyLabel.Padding = new Padding(8, 6, 8, 6);
            selectionBodyLabel.Size = new Size(260, 92);
            selectionBodyLabel.TabIndex = 1;
            selectionBodyLabel.Text = "Select a spawn";
            //
            // mapIconLegend
            //
            mapIconLegend.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            mapIconLegend.BackColor = Color.FromArgb(32, 36, 43);
            mapIconLegend.Location = new Point(10, 10);
            mapIconLegend.Name = "mapIconLegend";
            mapIconLegend.Padding = new Padding(10, 8, 10, 8);
            mapIconLegend.Size = new Size(360, 174);
            mapIconLegend.TabIndex = 7;
            mapIconLegend.Paint += MapIconLegend_Paint;
            //
            // mapControlRail
            //
            mapControlRail.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            mapControlRail.AutoSize = false;
            mapControlRail.AutoSizeMode = AutoSizeMode.GrowOnly;
            mapControlRail.BackColor = Color.FromArgb(32, 36, 43);
            railNpcRow.Controls.Add(btnRailNpc);
            railNpcRow.Controls.Add(btnRailNpcCorpses);
            railPcRow.Controls.Add(btnRailPlayers);
            railPcRow.Controls.Add(btnRailPcCorpses);
            mapControlRail.Controls.Add(railNpcRow);
            mapControlRail.Controls.Add(railPcRow);
            mapControlRail.Controls.Add(btnRailGrid);
            mapControlRail.Controls.Add(btnRailLabels);
            mapControlRail.Controls.Add(btnRailFollow);
            mapControlRail.Controls.Add(btnRailTarget);
            mapControlRail.Controls.Add(btnRailLegend);
            mapControlRail.Controls.Add(btnRailReset);
            mapControlRail.FlowDirection = FlowDirection.LeftToRight;
            mapControlRail.Location = new Point(10, 10);
            mapControlRail.Name = "mapControlRail";
            mapControlRail.Padding = new Padding(8);
            mapControlRail.MinimumSize = new Size(400, 0);
            mapControlRail.Size = new Size(400, 86);
            mapControlRail.TabIndex = 3;
            mapControlRail.WrapContents = false;
            ConfigureRailRow(railNpcRow);
            ConfigureRailRow(railPcRow);
            ConfigureRailButton(btnRailNpc, "NPC", "Toggle NPCs", RailNpc_Click);
            ConfigureRailButton(btnRailPlayers, "PC", "Toggle players", RailPlayers_Click);
            ConfigureRailButton(btnRailNpcCorpses, "CRP", "Toggle NPC corpses", RailNpcCorpses_Click);
            ConfigureRailButton(btnRailPcCorpses, "CRP", "Toggle player corpses", RailPcCorpses_Click);
            ConfigureRailButton(btnRailGrid, "GRID", "Toggle map grid", RailGrid_Click);
            ConfigureRailButton(btnRailLabels, "TXT", "Toggle zone text labels", RailLabels_Click);
            ConfigureRailButton(btnRailFollow, "FOL", "Cycle follow mode", RailFollow_Click);
            ConfigureRailButton(btnRailTarget, "TGT", "Toggle target info", RailTarget_Click);
            ConfigureRailButton(btnRailLegend, "LEG", "Toggle map legend", RailLegend_Click);
            ConfigureRailButton(btnRailReset, "RST", "Reset map view", RailReset_Click);
            // MapCon
            //
            AutoScroll = true;
            BackColor = SystemColors.Control;
            Controls.Add(mapIconLegend);
            Controls.Add(mapControlRail);
            Controls.Add(selectionInfoCard);
            Controls.Add(tableLayoutPanel1);
            Location = new Point(3, 3);
            Name = "MapCon";
            Size = new Size(227, 154);
            Paint += new PaintEventHandler(MapCon_Paint);
            Resize += (_, _) => PositionMapOverlays();
            KeyPress += new KeyPressEventHandler(MapCon_KeyPress);
            MouseDoubleClick += new MouseEventHandler(MapCon_MouseDoubleClick);
            MouseDown += new MouseEventHandler(MapCon_MouseDown);
            MouseMove += new MouseEventHandler(MapCon_MouseMove);
            MouseUp += new MouseEventHandler(MapCon_MouseUp);
            MouseWheel += new MouseEventHandler(MapCon_MouseScroll);
            tableLayoutPanel1.ResumeLayout(false);
            selectionInfoCard.ResumeLayout(false);
            mapIconLegend.ResumeLayout(false);
            railNpcRow.ResumeLayout(false);
            railPcRow.ResumeLayout(false);
            mapControlRail.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
            ModernTheme.ApplyMapSurface(this);
            tableLayoutPanel1.Visible = false;
            PositionMapOverlays();
            ApplyMapOverlayVisibility();
            ApplyTargetInfoVisibility();
            mapIconLegend.BringToFront();
            selectionInfoCard.BringToFront();
            UpdateRailState();
        }

        private static void ConfigureRailRow(FlowLayoutPanel row)
        {
            row.AutoSize = false;
            row.BackColor = Color.Transparent;
            row.FlowDirection = FlowDirection.TopDown;
            row.Margin = new Padding(0, 0, 8, 0);
            row.MinimumSize = new Size(42, 70);
            row.Padding = new Padding(0);
            row.Size = new Size(42, 70);
            row.WrapContents = false;
        }

        private void ConfigureRailButton(Button button, string text, string tooltip, EventHandler handler, int width = 42)
        {
            button.Name = $"btnRail{text}";
            button.Text = text;
            button.Click += handler;
            railToolTip.SetToolTip(button, tooltip);
            ModernTheme.StyleRailButton(button, false);
            button.Width = width;
            button.MinimumSize = new Size(width, 34);
            button.Margin = new Padding(0, 0, 6, 0);
        }

        private void PositionMapOverlays()
        {
            PositionMapRail();
            PositionMapIconLegend();
        }

        private void PositionMapIconLegend()
        {
            if (mapIconLegend == null)
            {
                return;
            }

            int y = Math.Max(10, Height - mapIconLegend.Height - 12);
            if (mapControlRail != null && Settings.Default.ShowMapRail && Width < mapIconLegend.Width + mapControlRail.Width + 34)
            {
                y = Math.Max(10, Height - mapIconLegend.Height - mapControlRail.Height - 22);
            }

            mapIconLegend.Location = new Point(10, y);
        }

        private void PositionMapRail()
        {
            if (mapControlRail == null)
            {
                return;
            }

            int width = Math.Min(400, Math.Max(260, Width - 20));
            mapControlRail.Size = new Size(width, 86);
            mapControlRail.MinimumSize = new Size(width, 0);
            int x = Math.Max(10, Width - mapControlRail.Width - 12);
            int y = Math.Max(10, Height - mapControlRail.Height - 12);
            mapControlRail.Location = new Point(x, y);
        }

        private void MapIconLegend_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(mapIconLegend.BackColor);

            using (var titleFont = new Font(ModernTheme.UiFont.FontFamily, 9.0f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.FromArgb(232, 238, 244)))
            {
                g.DrawString("Map Legend", titleFont, textBrush, 10, 8);
                g.DrawString("Alert Rings", titleFont, textBrush, 188, 8);
            }

            DrawLegendRow(g, 30, "NPC", DrawLegendNpc);
            DrawLegendRow(g, 52, "Player", DrawLegendPlayer);
            DrawLegendRow(g, 74, "NPC corpse", graphics => DrawLegendCorpse(graphics, Color.Cyan));
            DrawLegendRow(g, 96, "PC corpse", graphics => DrawLegendCorpse(graphics, Color.Yellow));
            DrawLegendRow(g, 118, "Ground spawn", DrawLegendGroundSpawn);
            DrawLegendRow(g, 140, "Selected", DrawLegendSelected);
            DrawLegendRow(g, 30, "Hunt", graphics => DrawLegendAlertRing(graphics, Color.LimeGreen), 196, 216);
            DrawLegendRow(g, 52, "Caution", graphics => DrawLegendAlertRing(graphics, Color.Orange), 196, 216);
            DrawLegendRow(g, 74, "Danger", graphics => DrawLegendAlertRing(graphics, Color.Red), 196, 216);
            DrawLegendRow(g, 96, "Rare", graphics => DrawLegendAlertRing(graphics, Color.Fuchsia), 196, 216);
        }

        private static void DrawLegendRow(Graphics g, int y, string label, Action<Graphics> drawIcon, int iconX = 18, int textX = 38)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(iconX, y + 9);
            drawIcon(g);
            g.Restore(state);

            using (var textBrush = new SolidBrush(Color.FromArgb(232, 238, 244)))
            {
                g.DrawString(label, ModernTheme.UiFont, textBrush, textX, y);
            }
        }

        private static void DrawLegendNpc(Graphics g)
        {
            using (var shadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            using (var fill = new SolidBrush(Color.Cyan))
            using (var border = new Pen(Color.FromArgb(235, 255, 255, 255), 1.2f))
            {
                g.FillEllipse(shadow, -5, -3, 12, 12);
                g.FillEllipse(fill, -6, -5, 12, 12);
                g.DrawEllipse(border, -6, -5, 12, 12);
            }
        }

        private static void DrawLegendPlayer(Graphics g)
        {
            PointF[] diamond =
            {
                new PointF(0, -7),
                new PointF(7, 0),
                new PointF(0, 7),
                new PointF(-7, 0)
            };

            using (var shadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            using (var fill = new SolidBrush(Color.Lime))
            using (var border = new Pen(Settings.Default.PCBorderColor, 1.3f))
            {
                g.TranslateTransform(1, 2);
                g.FillPolygon(shadow, diamond);
                g.TranslateTransform(-1, -2);
                g.FillPolygon(fill, diamond);
                g.DrawPolygon(border, diamond);
            }
        }

        private static void DrawLegendCorpse(Graphics g, Color color)
        {
            RectangleF skull = new RectangleF(-7, -8, 14, 12);
            RectangleF jaw = new RectangleF(-4, 0, 8, 6);

            using (var skullBrush = new SolidBrush(Color.FromArgb(238, color)))
            using (var jawBrush = new SolidBrush(Color.FromArgb(220, color)))
            using (var outline = new Pen(Color.FromArgb(235, 30, 34, 40), 1.1f))
            using (var eyeBrush = new SolidBrush(Color.FromArgb(235, 30, 34, 40)))
            {
                g.FillEllipse(skullBrush, skull);
                g.DrawEllipse(outline, skull);
                g.FillRectangle(jawBrush, jaw);
                g.DrawRectangle(outline, jaw.X, jaw.Y, jaw.Width, jaw.Height);
                g.FillEllipse(eyeBrush, -4.3f, -4.3f, 3.2f, 3.2f);
                g.FillEllipse(eyeBrush, 1.1f, -4.3f, 3.2f, 3.2f);
                g.FillPolygon(eyeBrush, new[] { new PointF(0, -1), new PointF(-1.4f, 1.8f), new PointF(1.4f, 1.8f) });
            }
        }

        private static void DrawLegendGroundSpawn(Graphics g)
        {
            PointF[] diamond =
            {
                new PointF(0, -7),
                new PointF(7, 0),
                new PointF(0, 7),
                new PointF(-7, 0)
            };

            using (var fill = new SolidBrush(Color.FromArgb(224, 202, 137, 44)))
            using (var border = new Pen(Color.FromArgb(245, 255, 236, 178), 1.3f))
            {
                g.FillPolygon(fill, diamond);
                g.DrawPolygon(border, diamond);
            }
        }

        private static void DrawLegendSelected(Graphics g)
        {
            using (var fill = new SolidBrush(Color.Cyan))
            using (var border = new Pen(Color.FromArgb(235, 255, 255, 255), 1.2f))
            using (var selected = new Pen(ModernTheme.AccentWarm, 2.4f))
            {
                g.FillEllipse(fill, -5, -5, 10, 10);
                g.DrawEllipse(border, -5, -5, 10, 10);
                g.DrawEllipse(selected, -10, -10, 20, 20);
            }
        }

        private static void DrawLegendAlertRing(Graphics g, Color color)
        {
            using (var pen = new Pen(color, 2.2f))
            using (var fill = new SolidBrush(Color.FromArgb(70, color)))
            {
                g.FillEllipse(fill, -8, -8, 16, 16);
                g.DrawEllipse(pen, -8, -8, 16, 16);
            }
        }

        public void UpdateRailState()
        {
            ModernTheme.StyleRailButton(btnRailNpc, Settings.Default.ShowNPCs);
            ModernTheme.StyleRailButton(btnRailPlayers, Settings.Default.ShowPlayers);
            ModernTheme.StyleRailButton(btnRailNpcCorpses, Settings.Default.ShowCorpses);
            ModernTheme.StyleRailButton(btnRailPcCorpses, Settings.Default.ShowPCCorpses);
            SetRailButtonLayout(btnRailNpc, 42, RailButtonPlacement.StackTop);
            SetRailButtonLayout(btnRailPlayers, 42, RailButtonPlacement.StackTop);
            SetRailButtonLayout(btnRailNpcCorpses, 42, RailButtonPlacement.StackBottom);
            SetRailButtonLayout(btnRailPcCorpses, 42, RailButtonPlacement.StackBottom);
            SetRailRowLayout(railNpcRow);
            SetRailRowLayout(railPcRow);
            ModernTheme.StyleRailButton(btnRailGrid, (Settings.Default.DrawOptions & DrawOptions.GridLines) != DrawOptions.None);
            ModernTheme.StyleRailButton(btnRailLabels, (Settings.Default.DrawOptions & DrawOptions.ZoneText) != DrawOptions.None);
            ModernTheme.StyleRailButton(btnRailFollow, Settings.Default.FollowOption != FollowOption.None);
            ModernTheme.StyleRailButton(btnRailTarget, Settings.Default.ShowTargetInfo);
            ModernTheme.StyleRailButton(btnRailLegend, Settings.Default.ShowMapLegend);
            ModernTheme.StyleRailButton(btnRailReset, false);
            SetRailButtonLayout(btnRailGrid, 42, RailButtonPlacement.Row);
            SetRailButtonLayout(btnRailLabels, 42, RailButtonPlacement.Row);
            SetRailButtonLayout(btnRailFollow, 42, RailButtonPlacement.Row);
            SetRailButtonLayout(btnRailTarget, 42, RailButtonPlacement.Row);
            SetRailButtonLayout(btnRailLegend, 42, RailButtonPlacement.Row);
            SetRailButtonLayout(btnRailReset, 42, RailButtonPlacement.Row);
            ApplyMapOverlayVisibility();

        }

        private enum RailButtonPlacement
        {
            Row,
            StackTop,
            StackBottom
        }

        private static void SetRailButtonLayout(Button button, int width, RailButtonPlacement placement)
        {
            button.Width = width;
            button.MinimumSize = new Size(width, 34);
            button.Margin = placement switch
            {
                RailButtonPlacement.StackTop => new Padding(0, 0, 0, 2),
                RailButtonPlacement.StackBottom => Padding.Empty,
                _ => new Padding(0, 0, 8, 0)
            };
        }

        private static void SetRailRowLayout(FlowLayoutPanel row)
        {
            row.AutoSize = false;
            row.FlowDirection = FlowDirection.TopDown;
            row.MinimumSize = new Size(42, 70);
            row.Size = new Size(42, 70);
            row.Margin = new Padding(0, 0, 8, 0);
        }

        private void RailNpc_Click(object sender, EventArgs e) => f1?.ToggleNPCsFromRail();
        private void RailPlayers_Click(object sender, EventArgs e) => f1?.TogglePlayersFromRail();
        private void RailNpcCorpses_Click(object sender, EventArgs e) => f1?.ToggleNPCCorpsesFromRail();
        private void RailPcCorpses_Click(object sender, EventArgs e) => f1?.TogglePCCorpsesFromRail();
        private void RailGrid_Click(object sender, EventArgs e) => f1?.ToggleGridFromRail();
        private void RailLabels_Click(object sender, EventArgs e) => f1?.ToggleZoneTextFromRail();
        private void RailFollow_Click(object sender, EventArgs e) => f1?.CycleFollowFromRail();
        private void RailTarget_Click(object sender, EventArgs e) => f1?.ToggleTargetInfoFromRail();
        private void RailLegend_Click(object sender, EventArgs e) => ToggleMapLegend();
        private void RailReset_Click(object sender, EventArgs e) => f1?.ResetMapFromRail();

        public void ToggleMapLegend()
        {
            Settings.Default.ShowMapLegend = !Settings.Default.ShowMapLegend;
            ApplyMapOverlayVisibility();
            UpdateRailState();
            Invalidate();
        }

        public void ApplyMapOverlayVisibility()
        {
            if (mapIconLegend != null)
            {
                mapIconLegend.Visible = Settings.Default.ShowMapLegend;
            }

            if (mapControlRail != null)
            {
                mapControlRail.Visible = Settings.Default.ShowMapRail;
            }
        }

        public void ApplyTargetInfoVisibility()
        {
            if (tableLayoutPanel1 != null)
            {
                tableLayoutPanel1.Visible = false;
            }

            if (selectionInfoCard == null)
            {
                return;
            }

            selectionInfoCard.Visible = Settings.Default.ShowTargetInfo;
            if (selectionInfoCard.Visible)
            {
                selectionInfoCard.BringToFront();
            }
        }

        private void SetSelectionCardText(string text, Color? headerColor = null)
        {
            if (selectionInfoCard == null || selectionBodyLabel == null)
            {
                return;
            }

            selectionClockLabel.BackColor = headerColor ?? ModernTheme.Accent;
            selectionBodyLabel.Text = string.IsNullOrWhiteSpace(text) ? "Select a spawn" : text;

            using (Graphics graphics = selectionBodyLabel.CreateGraphics())
            {
                Size proposed = new Size(360, int.MaxValue);
                Size textSize = TextRenderer.MeasureText(
                    graphics,
                    selectionBodyLabel.Text,
                    selectionBodyLabel.Font,
                    proposed,
                    TextFormatFlags.WordBreak);

                int width = Math.Max(260, Math.Min(380, textSize.Width + 28));
                int bodyHeight = Math.Max(92, textSize.Height + 22);
                selectionInfoCard.Size = new Size(width, bodyHeight + 24);
                selectionClockLabel.Size = new Size(width, 24);
                selectionBodyLabel.Size = new Size(width, bodyHeight);
            }

            ApplyTargetInfoVisibility();
        }

        private void SetSelectionCardSpawn(Spawninfo sp)
        {
            if (sp == null)
            {
                return;
            }

            string info = SpawnInfoWindow(sp).ToString();
            Color header = HeaderColorForSpawn(sp);
            SetSelectionCardText(info, header);
            CacheSelectedInfo(sp.SpawnID, info);
        }

        public void UpdateSelectionCardGroundItem(GroundItem gi)
        {
            if (gi == null)
            {
                return;
            }

            SetSelectionCardText(BuildGroundItemInfo(gi), ModernTheme.Accent);
        }

        public void UpdateSelectionCardGroundItem(float x, float y)
        {
            GroundItem gi = eq?.GetItemSnapshot()
                .FirstOrDefault(item => Math.Abs(item.ItemLocation.X - x) < 0.75f && Math.Abs(item.ItemLocation.Y - y) < 0.75f);

            UpdateSelectionCardGroundItem(gi);
        }

        private static string BuildGroundItemInfo(GroundItem gi)
        {
            return $"Ground Item: {gi.Desc}\nActorDef: {gi.Name}\n{gi.ItemLocation}";
        }

        private Color HeaderColorForSpawn(Spawninfo sp)
        {
            InfoSetColor(sp);
            return lblGameClock.BackColor;
        }

        public void UpdateSelectedSpawnInfo()
        {
            if (eq == null)
            {
                SetSelectionCardText("Select a spawn");
                return;
            }

            if (eq.SelectedID == 99999)
            {
                SetSelectionCardText(SelectedListInfo());
                return;
            }

            bool found = eq.TryGetMobBySpawnId(eq.SelectedID, out Spawninfo selected);
            if (found)
            {
                SetSelectionCardSpawn(selected);
            }
            else
            {
                SetSelectionCardText(SelectedListInfo());
            }
        }

        public void UpdateSelectedSpawnInfoFromList(int spawnId, string listSummary)
        {
            lastPolledListSpawnId = spawnId;
            lastInfoSpawnId = 99999;
            lastInfoText = "";

            if (eq != null)
            {
                eq.SetSelectedID(spawnId);
            }

            SetSelectionCardText(listSummary);
            CacheSelectedInfo(spawnId, listSummary);

            if (eq != null && eq.TryGetMobBySpawnId(spawnId, out Spawninfo selected))
            {
                SetSelectionCardSpawn(selected);
                return;
            }
        }

        private void CacheSelectedInfo(int spawnId, string info)
        {
            if (spawnId == 99999 || string.IsNullOrWhiteSpace(info) || info == "No spawn selected")
            {
                return;
            }

            lastInfoSpawnId = spawnId;
            lastInfoText = info;
        }

        private string SelectedListInfo()
        {
            return TryBuildSelectedListInfo(out string summary)
                ? summary
                : MobInfo(null, true, true);
        }

        private bool TryBuildSelectedListInfo(out string info)
        {
            info = "";

            if (f1?.SpawnList?.TryGetSelectedSpawnSummary(out string summary) == true)
            {
                MobInfoLabel.BackColor = Color.FromArgb(46, 53, 63);
                MobInfoLabel.ForeColor = Color.FromArgb(232, 238, 244);
                lblGameClock.BackColor = ModernTheme.Accent;
                info = MobshowInfo(new StringBuilder(summary));
                if (f1.SpawnList.TryGetSelectedSpawnId(out int selectedSpawnId))
                {
                    CacheSelectedInfo(selectedSpawnId, info);
                }
                return true;
            }

            return false;
        }

        private bool RestoreSelectedSpawnFromList()
        {
            if (eq == null || f1?.SpawnList == null)
            {
                return eq != null && eq.SelectedID != 99999;
            }

            if (f1.SpawnList.TryGetSelectedSpawnId(out int selectedSpawnId) && selectedSpawnId != eq.SelectedID)
            {
                eq.SetSelectedID(selectedSpawnId);
                return true;
            }

            return eq.SelectedID != 99999;
        }

        private void MapCon_KeyPress(object sender, KeyPressEventArgs e) => mapPane.MapCon_KeyPress(sender, e);

        #endregion Component Designer generated code

        private void MapCon_Paint(object sender, PaintEventArgs pe)
        {
            if (mapPane == null || f1 == null) return;

            try
            {
                // Skip rendering if the window is minimized
                if (f1.WindowState == FormWindowState.Minimized) return;

                DrawOptions drawOpts = f1.DrawOpts;

                // Clear the map and update the info header clock
                bkgBuffer.Graphics.Clear(Settings.Default.BackColor);
                UpdateInfoClock();

                // Update spawn sizes if settings changed
                if (SettingsSpawnSize != Settings.Default.SpawnDrawSize)
                {
                    SetSpawnSizes();
                }

                // Get player coordinates for reference in rendering
                float pX = eq.GamerInfo.X;
                float pY = eq.GamerInfo.Y;
                float pZ = eq.GamerInfo.Z;
                PointF playerF = new PointF(CalcScreenCoordX(pX), CalcScreenCoordY(pY));

                // Calculate translation offsets for the map
                float dx = ((PanOffsetX + screenCenter.X) / -Ratio) - mapCenter.X;
                float dy = ((PanOffsetY + screenCenter.Y) / -Ratio) - mapCenter.Y;

                // Save graphics state, set transformations for drawing
                GraphicsState originalState = bkgBuffer.Graphics.Save();
                bkgBuffer.Graphics.ScaleTransform(-Ratio, -Ratio);
                bkgBuffer.Graphics.TranslateTransform(dx, dy);

                // Draw static map elements
                DrawMapLines(drawOpts);
                bkgBuffer.Graphics.Restore(originalState);

                // Dynamic map elements
                DrawMap(drawOpts);

                // Additional elements like trails, spawns, and items
                if ((drawOpts & DrawOptions.SpawnTrails) != DrawOptions.None) DrawSpawnTrails();

                if (!eq.Zoning)
                {
                    if ((drawOpts & DrawOptions.Player) != DrawOptions.None)
                    {
                        DrawGamer(playerF, SpawnSize, SpawnSizeOffset, drawOpts);
                    }
                    DrawSpawns(drawOpts, pX, pY, pZ, playerF);
                    DrawGroundItems(drawOpts, pZ);
                    DrawSpawntimers(drawOpts);
                    SmoothMode();
                    DrawCorpses(drawOpts, pZ);
                    RefreshSelectedSpawnInfoCard();
                }
                else
                {
                    DrawGamer(playerF, SpawnSize, SpawnSizeOffset, drawOpts);
                }

                // Restore default smoothing mode after drawing
                bkgBuffer.Graphics.SmoothingMode = SmoothingMode.Default;

                // Draw a debug line for reference
                DrawDashLine(drawOpts, playerF);

                // Render everything to the actual graphics context
                bkgBuffer.Render(pe.Graphics);
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in MapCon_Paint(): ", ex);
            }
        }

        private void RefreshSelectedSpawnInfoCard()
        {
            if (eq == null || eq.SelectedID == 99999)
            {
                return;
            }

            Spawninfo selected = eq.GetMobSnapshot().FirstOrDefault(sp => sp.SpawnID == eq.SelectedID);
            if (selected != null)
            {
                SetSelectionCardSpawn(selected);
            }
        }

        private void SmoothMode()
        {
            if (Settings.Default.SpawnDrawSize > 1)
            {
                bkgBuffer.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            }
        }

        private void InitializeVariables()
        {
            CursorHand = Cursors.Hand;

            // Initialize DragVariables to 0,0 and set semiphore false
            ResetDragState();
            PanOffsetX = PanOffsetY = 0;
            SetSelectedPoint(-1, -1);

            toolTip = new ToolTip
            {
                AutomaticDelay = 500
            };
            toolTip.SetToolTip(this, "ABCD\nEFGH");
            toolTip.Active = true;

            // Set sine and cosine values to use with headings
            for (var p = 0; p < 512; p++)
            {
                xCos[p] = (float)Math.Cos(p / 512.0f * 2.0f * Math.PI);
                xSin[p] = (float)Math.Sin(p / 512.0f * 2.0f * Math.PI);
            }

            //            textBrush = new SolidBrush(Color.LightGray);
        }

        private void ResetDragState()
        {
            MouseDragging = false;
            m_rangechange = false;
            m_dragStartX = m_dragStartY = 0;
        }

        private void SetSelectedPoint(float x, float y)
        {
            selectedPoint.X = x;
            selectedPoint.Y = y;

            if (eq != null)
            {
                SelectPoint?.Invoke(eq.GamerInfo, x, y);
            }
        }

        public void Offset_X_ValueChanged(NumericUpDown offsetx)
        {
            PanOffsetX = -(int)offsetx.Value;
            ReAdjust();
            Invalidate();
        }

        public void Offset_Y_ValueChanged(NumericUpDown offsety)
        {
            PanOffsetY = -(int)offsety.Value;

            ReAdjust();
            Invalidate();
        }

        #region MouseOps

        private void MapCon_MouseScroll(object sender, MouseEventArgs me)
        {
            if (mapPane == null)
            {
                return;
            }

            var newScale = scale + (me.Delta / 600.0f);

            if (newScale >= 0.1)
            {
                MapPane.scale.Value = (decimal)(newScale * 100);
            }

            ReAdjust();

            Invalidate();
        }

        private void MapCon_MouseDown(object sender, MouseEventArgs mouse)
        {
            if (mouse.Button == MouseButtons.Left)
            {
                // Range Circle Checks
                if (Settings.Default.RangeCircle > 0)
                {
                    float rCircleRadius = Settings.Default.RangeCircle;

                    var upperRadius = rCircleRadius + (4 * SpawnSize);

                    var lowerRadius = rCircleRadius - (4 * SpawnSize);

                    if (lowerRadius < 0)
                    {
                        lowerRadius = 0;
                    }

                    // Calc the proper loc for the mouse

                    MouseMapLoc(mouse, out var mousex, out var mousey);

                    // if within approximately one mob radius of the Range Circle
                    // then we are resizing range circle, and not dragging.

                    var sd = MouseDistance(mousex, mousey);

                    if (Settings.Default.AlertInsideRangeCircle && (sd > lowerRadius) && (sd < upperRadius))
                    {
                        // changing range cirlce size

                        m_rangechange = true;
                    }
                }

                if (!m_rangechange)

                {
                    Cursor.Current = CursorHand;

                    // Remember the mouse down loc and set semiphore

                    MouseDragging = true;

                    m_dragStartX = mouse.X;

                    m_dragStartY = mouse.Y;

                    // remember the original PanOffsets...

                    m_dragStartPanX = PanOffsetX;

                    m_dragStartPanY = PanOffsetY;
                }
            }
            else if (mouse.Button == MouseButtons.Right)
            {
                // right click of mouse.
                // see if there is a mob located there.
                RightMouseButton(mouse);
            }
        }

        private float MouseDistance(float mousex, float mousey)
        {
            return (float)Math.Sqrt(((mousey - eq.GamerInfo.Y) * (mousey - eq.GamerInfo.Y)) +

                ((mousex - eq.GamerInfo.X) * (mousex - eq.GamerInfo.X)));
        }

        private void MouseMapLoc(MouseEventArgs e, out float mousex, out float mousey)
        {
            mousex = mapCenter.X + ((PanOffsetX + screenCenter.X - e.X) / Ratio);
            mousey = mapCenter.Y + ((PanOffsetY + screenCenter.Y - e.Y) / Ratio);
        }

        private void RightMouseButton(MouseEventArgs e)
        {
            // Convert mouse coordinates to map coordinates.
            MouseMapLoc(e, out var mousex, out var mousey);

            //Proximity threshold.
            float delta = 5.0f / Ratio;

            // Try to find a mob at the clicked location.
            Spawninfo sp = eq.FindMob(mousex, mousey, delta, true, true, true);
            if (TryProcessEntity(sp)) return;

            // Try to find a ground item if no mob is found.
            GroundItem gi = eq.FindGroundItem(mousex, mousey, delta);
            if (TryProcessEntity(gi)) return;

            // Try to find a spawn timer if no ground item is found.
            Spawntimer st = eq.FindTimer(mousex, mousey, 5.0f);
            if (TryProcessEntity(st)) return;

            // Fallback: Try to find a general mob with less strict search criteria.
            sp = eq.FindMob(mousex, mousey, delta, true, true);
            if (TryProcessEntity(sp)) return;

            // If no entity is found, clear the alert and update context menu.
            ClearAlert();
            f1.SetContextMenu();
        }

        // Helper method to handle entity processing based on type.
        private bool TryProcessEntity(object entity)
        {
            switch (entity)
            {
                case Spawninfo sp when sp.Name.Length > 0:
                    f1.alertAddmobname = sp.Name.FilterAlertName();
                    SetAlertCoordinates(sp.X, sp.Y, sp.Z);
                    f1.SetContextMenu();
                    return true;

                case GroundItem gi when gi.Name.Length > 0:
                    f1.alertAddmobname = eq.GetItemDescription(gi.Name);
                    SetAlertCoordinates(gi.ItemLocation.X, gi.ItemLocation.X, gi.ItemLocation.Z);
                    f1.SetContextMenu();
                    return true;

                case Spawntimer st:
                    f1.alertAddmobname = GetSpawnTimerName(st);
                    SetAlertCoordinates(st.Location.X, st.Location.Y, st.Location.Z);
                    f1.SetContextMenu();
                    return true;

                default:
                    return false;
            }
        }

        private void SetAlertCoordinates(float x, float y, float z)
        {
            f1.alertX = x;
            f1.alertY = y;
            f1.alertZ = z;
        }

        private void MapCon_MouseUp(object sender, MouseEventArgs e)
        {
            MouseMapLoc(e, out var mousex, out var mousey);

            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                eq.ModKeyControl(this, mousex, mousey);
            }
            else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                // [42!] Mark an arbitrary spot on the map, or turn it off if a spot was marked already.

                if (selectedPoint.X == -1)
                {
                    SetSelectedPoint(mousex, mousey);
                }
                else
                {
                    SetSelectedPoint(-1, -1);
                }
            }
            else if (e.X == m_dragStartX && e.Y == m_dragStartY)
            {
                // allow a small margin of error in coordinates
                // value of 5 screen units in terms of mapcoords
                // try to select mob, if not then do timer

                var delta = 5.0f / Ratio;
                if (!SelectMapSpawn(e.Location) && !eq.SelectTimer(mousex, mousey, delta))
                {
                    if (eq.SelectGroundItem(mousex, mousey, delta))
                    {
                        f1?.GroundItemList.HighlightSelectedGroundItem();
                        GroundItem gi = eq.FindGroundItem(mousex, mousey, delta);
                        UpdateSelectionCardGroundItem(gi);
                    }
                }

                Invalidate();
            }

            ResetDragState();
            Cursor.Current = Cursors.Default;
        }

        private void MapCon_MouseMove(object sender, MouseEventArgs e)
        {
            if (mapPane != null && f1 != null)
            {
                // Limit TT popups to four times a sec
                if (canShowTooltip)
                {
                    // Show the tooltip
                    canShowTooltip = false;
                    tooltipTimer.Start(); // Start the timer to reset the flag after the interval
                }

                // Calc the proper loc for the mouse
                MouseMapLoc(e, out var mousex, out var mousey);

                // Range
                var sd = MouseDistance(mousex, mousey);

                f1.toolStripMouseLocation.Text = $"Map /loc: {mousey:f2}, {mousex:f2}";
                f1.toolStripDistance.Text = $"Distance: {sd:f1}";

                // If we are dragging, then change the origin.

                if (MouseDragging)
                {
                    // Compute delta x,y from original click
                    var dx = m_dragStartX - e.X;
                    var dy = m_dragStartY - e.Y;
                    mapPane.offsetx.Value = -(decimal)(m_dragStartPanX - dx);
                    mapPane.offsety.Value = -(decimal)(m_dragStartPanY - dy);
                    ReAdjust();
                    Invalidate();
                }
                else
                {
                    PopulateToolTip(e);
                }
            }
        }

        private string GetSpawnTimerName(Spawntimer st)
        {
            foreach (var name in st.AllNames.Split(','))
            {
                var trimmedName = name.TrimName();
                if (trimmedName.Length > 0)
                {
                    if (trimmedName.RegexMatch())
                    {
                        return trimmedName;
                    }

                    if (string.IsNullOrEmpty(f1.alertAddmobname))
                    {
                        f1.alertAddmobname = trimmedName;
                    }
                }
            }

            return f1.alertAddmobname;
        }

        private void ClearAlert()
        {
            f1.alertAddmobname = "";
            f1.alertX = 0.0f;
            f1.alertY = 0.0f;
            f1.alertZ = 0.0f;
        }

        private void MapCon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // if double click in range circle, turn it on/off
            MouseMapLoc(e, out var mousex, out var mousey);
            if (MouseDistance(mousex, mousey) < Settings.Default.RangeCircle)
            {
                Settings.Default.AlertInsideRangeCircle = !Settings.Default.AlertInsideRangeCircle;
            }
        }

        #endregion MouseOps

        #region MapMath

        public void ReAdjust()
        {
            // Calculate the dimensions of the map
            float mapWidth = Math.Abs(MaxMapX - MinmapX);
            float mapHeight = Math.Abs(MaxMapY - MinMapY);

            // Calculate screen dimensions with a padding of 30
            float screenWidth = Width - 30;
            float screenHeight = Height - 30;

            // Calculate screen center only once
            screenCenter = new PointF(Width / 2f, Height / 2f);

            // Calculate the smaller ratio to fit the map within the screen
            float xRatio = screenWidth / mapWidth;
            float yRatio = screenHeight / mapHeight;
            Ratio = xRatio < yRatio ? xRatio * scale : yRatio * scale;

            // Determine the map center based on the follow option
            UpdateMapCenter(mapWidth, mapHeight);

            // Adjust for follow options and apply offsets
            AdjustWhileFollow(mapWidth, mapHeight);
            adjustment.X = PanOffsetX + screenCenter.X + (mapCenter.X * Ratio);
            adjustment.Y = PanOffsetY + screenCenter.Y + (mapCenter.Y * Ratio);
        }

        internal int SetFilterPos(int value) => filterneg = value;

        internal int SetFilterNeg(int value) => filterpos = value;

        private void UpdateMapCenter(float mapWidth, float mapHeight)
        {
            switch (Settings.Default.FollowOption)
            {
                case FollowOption.None:
                    // Center of the map
                    mapCenter.X = MinmapX + (mapWidth / 2);
                    mapCenter.Y = MinMapY + (mapHeight / 2);
                    break;

                case FollowOption.Player:
                    // Follow player coordinates
                    mapCenter.X = eq.GamerInfo.X;
                    mapCenter.Y = eq.GamerInfo.Y;
                    break;

                case FollowOption.Target:
                    // Follow target coordinates if available
                    Spawninfo target = eq.GetSelectedMob();
                    if (target != null)
                    {
                        mapCenter.X = target.X;
                        mapCenter.Y = target.Y;
                    }
                    break;
            }
        }

        private void AdjustWhileFollow(float mapWidth, float mapHeight)
        {
            if (!Settings.Default.KeepCentered && Settings.Default.FollowOption != FollowOption.None)
            {
                // Calculate screen edges in map coordinates.
                float screenMinY = ScreenToMapCoordY(Height - 15);
                float screenMaxY = ScreenToMapCoordY(15);
                float screenMapHeight = Math.Abs(screenMaxY - screenMinY);

                float screenMinX = ScreenToMapCoordX(Width - 15);
                float screenMaxX = ScreenToMapCoordX(15);
                float screenMapWidth = Math.Abs(screenMaxX - screenMinX);

                // Center map horizontally if it fits within screen width.
                if (mapWidth <= screenMapWidth)
                {
                    mapCenter.X = MinmapX + (mapWidth / 2);
                }
                else
                {
                    // Adjust the X center point to avoid blank space.
                    AdjustCenterX(screenMinX, screenMaxX);
                }

                // Center map vertically if it fits within screen height.
                if (mapHeight <= screenMapHeight)
                {
                    mapCenter.Y = MinMapY + (mapHeight / 2);
                }
                else
                {
                    // Adjust the Y center point to avoid blank space.
                    AdjustCenterY(screenMinY, screenMaxY);
                }
            }
        }

        private void AdjustCenterX(float screenMinX, float screenMaxX)
        {
            // Adjust map center X based on screen boundaries.
            if (screenMinX < MinmapX)
            {
                mapCenter.X += MinmapX - screenMinX;
            }
            else if (screenMaxX > MaxMapX)
            {
                mapCenter.X -= screenMaxX - MaxMapX;
            }
        }

        private void AdjustCenterY(float screenMinY, float screenMaxY)
        {
            // Adjust map center Y based on screen boundaries.
            if (screenMinY < MinMapY)
            {
                mapCenter.Y += MinMapY - screenMinY;
            }
            else if (screenMaxY > MaxMapY)
            {
                mapCenter.Y -= screenMaxY - MaxMapY;
            }
        }

        public float CalcScreenCoordX(float mapCoordinateX) => adjustment.X - (mapCoordinateX * Ratio);

        // Formula Should be
        // Screen X =CenterScreenX + ((mapCoordinateX - MapCenterX) * m_ratio)

        // However Eq's Map coordinates are in the oposite sense to the screen
        // so we have to multiply the second portion by -1, which is the same
        // as changing the plus to a minus...

        //m_ratio = (ScreenWidth/MapWidth) * zoom (Calculated ahead of time in ReAdjust)

        public float CalcScreenCoordY(float mapCoordinateY) => adjustment.Y - (mapCoordinateY * Ratio);

        private float ScreenToMapCoordX(float screenCoordX) => mapCenter.X + ((screenCenter.X - screenCoordX) / Ratio);

        private float ScreenToMapCoordY(float screenCoordY) => mapCenter.Y + ((screenCenter.Y - screenCoordY) / Ratio);

        private void MapChanged(EQMap map)
        {
            DrawOptions DrawOpts = f1.DrawOpts;

            // if the autoexpand is not checked, scale is not at 100, then maintain the map scale
            if (eq.Longname.Length > 0 && mapPane != null && !Settings.Default.AutoExpand &&
                MapPane.scale.Value != 100)
            {
                GetRatioAndSetScale();
                ClearPan();
            }
            else if (Settings.Default.KeepCentered)
            {
                ClearPan();
            }
            else
            {
                MapPane.scale.Value = 100M;
                SetScale_1();
                ClearPan();
            }

            // check that map text doesn't change extents

            if ((DrawOpts & DrawOptions.ZoneText) != DrawOptions.None)
            {
                VerifyTextExtents(DrawOpts);
            }

            Invalidate();
        }

        private void GetRatioAndSetScale()
        {
            var mapWidth = Math.Abs(MaxMapX - MinmapX);
            var mapHeight = Math.Abs(MaxMapY - MinMapY);

            var screenWidth = Width - 30;  // 2 * 15
            var screenHeight = Height - 30; // 2 * 15

            var ratio = Math.Min(screenWidth / mapWidth, screenHeight / mapHeight);

            SetScale(ratio);
        }

        private void SetScale(float ratio)
        {
            scale = ratio > 0.0f ? Ratio / ratio : 1.0f;

            if (scale < 0.1f)
            {
                SetScale_1();
            }

            MapPane.scale.Value = (decimal)(Math.Round(scale, 1) * 100);
        }

        public void SetScale_1() => scale = 1.0f;

        private void VerifyTextExtents(DrawOptions drawOpts)
        {
            float xlabelOffset = 0, ylabelOffset = 0;
            float factor = 1 / Ratio; // Scaling factor based on the map's zoom ratio.

            // Adjust for grid line labels if enabled
            if ((drawOpts & DrawOptions.GridLines) != 0)
            {
                ylabelOffset = drawFont.GetHeight(); // Height of the text font
                xlabelOffset = bkgBuffer.Graphics.MeasureString("10000", drawFont).Width; // Example width for labels
            }

            foreach (MapLabel label in mapData.Labels)
            {
                SizeF textSize = bkgBuffer.Graphics.MeasureString(label.Text, drawFont);

                float labelMinX = label.Position.X - ((textSize.Width + xlabelOffset) * factor);
                MinmapX = Math.Min(MinmapX, labelMinX);
                MaxMapX = Math.Max(MaxMapX, label.Position.X);

                float labelMinY = label.Position.Y - ((textSize.Height + ylabelOffset) * factor);
                MinMapY = Math.Min(MinMapY, labelMinY);
                MaxMapY = Math.Max(MaxMapY, label.Position.Y);
            }
            ReAdjust();
        }

        #endregion MapMath

        #region DrawShapes

        private void DrawCross(Pen pen, PointF center, float offset)
        {
            // Calculate start and end points for each line of the cross
            PointF left = new PointF(center.X - offset, center.Y);
            PointF right = new PointF(center.X + offset, center.Y);
            PointF top = new PointF(center.X, center.Y - offset);
            PointF bottom = new PointF(center.X, center.Y + offset);

            // Draw the cross
            bkgBuffer.Graphics.DrawLine(pen, left, right);
            bkgBuffer.Graphics.DrawLine(pen, top, bottom);
        }

        private void DrawBigX(PointF drawPoint, float offset)
        {
            // Diagonal lines forming the "X"
            bkgBuffer.Graphics.DrawLine(YellowPen, drawPoint.X - offset, drawPoint.Y - offset, drawPoint.X + offset, drawPoint.Y + offset);
            bkgBuffer.Graphics.DrawLine(YellowPen, drawPoint.X - offset, drawPoint.Y + offset, drawPoint.X + offset, drawPoint.Y - offset);
        }

        private void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            try
            {
                bkgBuffer.Graphics.DrawLine(pen, x1, y1, x2, y2);
            }
            catch (Exception ex) { LogLib.WriteLine($"Error with DrawLine({x1}, {y1}, {x2}, {y2}): ", ex); }
        }

        private void DrawLines(Color pen, Point3D start, Point3D end)
        {
            try
            {
                bkgBuffer.Graphics.DrawLine(new Pen(pen), start.X, start.Y, end.X, end.Y);
            }
            catch (Exception ex) { LogLib.WriteLine("Error with DrawLines: ", ex); }
        }

        private void FillEllipse(Brush brush, float x1, float y1, float width, float height)
        {
            try
            {
                bkgBuffer.Graphics.FillEllipse(brush, x1, y1, width, height);
            }
            catch (Exception ex) { LogLib.WriteLine($"Error with Fill Ellipse({x1}, {y1}, {width}, {height}): ", ex); }
        }

        private void DrawEllipse(Pen pen, float x1, float y1, float width, float height)
        {
            try
            {
                bkgBuffer.Graphics.DrawEllipse(pen, x1, y1, width, height);
            }
            catch (Exception ex) { LogLib.WriteLine($"Error with DrawEllipse({x1}, {y1}, {width}, {height}): ", ex); }
        }

        private void DrawTriangle(Pen pen, float x1, float y1, float radius)
        {
            PointF[] points = TrianglePoints(x1, y1, radius);
            try
            {
                bkgBuffer.Graphics.DrawLines(pen, points);
            }
            catch (Exception ex) { LogLib.WriteLine($"Error with DrawTriangle({x1}, {y1}, {radius}): ", ex); }
        }

        private void FillTriangle(Brush brush, float x1, float y1, float radius)
        {
            PointF[] points = TrianglePoints(x1, y1, radius);

            try
            {
                bkgBuffer.Graphics.FillPolygon(brush, points);
            }
            catch (Exception ex) { LogLib.WriteLine($"Error with FillTriangle({x1}, {y1}, {radius}): ", ex); }
        }

        private static PointF[] TrianglePoints(float x1, float y1, float radius)
        {
            // Defining constants for better readability
            const float cos30 = 0.866025f; // Approximation of sqrt(3) / 2
            const float sin30 = 0.5f;      // Sine of 30 degrees

            return new PointF[]
            {
        new PointF(x1 + (radius * cos30), y1 + (radius * sin30)),  // Right vertex
        new PointF(x1 - (radius * cos30), y1 + (radius * sin30)),  // Left vertex
        new PointF(x1, y1 - radius)                                // Top vertex
            };
        }

        private void FillRectangle(Brush brush, float x1, float y1, float width, float height)
        {
            try { bkgBuffer.Graphics.FillRectangle(brush, x1, y1, width, height); }
            catch (Exception ex) { LogLib.WriteLine($"Error with FillRectangle({x1}, {y1}, {width}, {height}): ", ex); }
        }

        private void DrawModernCircleMarker(float x, float y, Brush brush, bool selected, bool matched)
        {
            float size = Math.Max(SpawnSize + 3, 9);
            float offset = size / 2f;

            FillEllipse(MarkerShadowBrush, x - offset + 1, y - offset + 2, size, size);
            FillEllipse(brush, x - offset, y - offset, size, size);
            DrawEllipse(new Pen(Color.FromArgb(235, 255, 255, 255), 1.2f), x - offset, y - offset, size, size);

            if (selected || matched)
            {
                DrawEllipse(new Pen(selected ? ModernTheme.AccentWarm : Color.White, selected ? 2.4f : 1.8f), x - offset - 4, y - offset - 4, size + 8, size + 8);
            }
        }

        private void DrawModernPlayerMarker(float x, float y, Brush brush, bool selected, bool matched)
        {
            float size = Math.Max(SpawnPlusSize + 3, 11);
            float offset = size / 2f;
            PointF[] points =
            {
                new PointF(x, y - offset),
                new PointF(x + offset, y),
                new PointF(x, y + offset),
                new PointF(x - offset, y)
            };

            bkgBuffer.Graphics.FillPolygon(MarkerShadowBrush, OffsetPoints(points, 1, 2));
            bkgBuffer.Graphics.FillPolygon(brush, points);
            bkgBuffer.Graphics.DrawPolygon(new Pen(Settings.Default.PCBorderColor, 1.4f), points);

            if (selected || matched)
            {
                DrawEllipse(new Pen(selected ? ModernTheme.AccentWarm : Color.White, selected ? 2.4f : 1.8f), x - offset - 4, y - offset - 4, size + 8, size + 8);
            }
        }

        private void DrawModernCorpseMarker(PointF point, Color color, bool matched)
        {
            float size = Math.Max(SpawnPlusSize + 6, 14);
            float offset = size / 2f;
            RectangleF skull = new RectangleF(point.X - offset, point.Y - offset, size, size * 0.82f);
            RectangleF jaw = new RectangleF(point.X - (size * 0.26f), point.Y + (size * 0.12f), size * 0.52f, size * 0.28f);

            using (var skullBrush = new SolidBrush(Color.FromArgb(238, color)))
            using (var jawBrush = new SolidBrush(Color.FromArgb(220, color)))
            using (var outline = new Pen(Color.FromArgb(235, 30, 34, 40), 1.2f))
            using (var eyeBrush = new SolidBrush(Color.FromArgb(235, 30, 34, 40)))
            using (var toothPen = new Pen(Color.FromArgb(235, 30, 34, 40), 1f))
            {
                FillEllipse(MarkerShadowBrush, skull.Left + 1, skull.Top + 2, skull.Width, skull.Height);
                bkgBuffer.Graphics.FillEllipse(skullBrush, skull);
                bkgBuffer.Graphics.DrawEllipse(outline, skull);
                bkgBuffer.Graphics.FillRectangle(jawBrush, jaw);
                bkgBuffer.Graphics.DrawRectangle(outline, jaw.X, jaw.Y, jaw.Width, jaw.Height);

                FillEllipse(eyeBrush, point.X - (size * 0.27f), point.Y - (size * 0.17f), size * 0.22f, size * 0.24f);
                FillEllipse(eyeBrush, point.X + (size * 0.05f), point.Y - (size * 0.17f), size * 0.22f, size * 0.24f);
                PointF[] nose =
                {
                    new PointF(point.X, point.Y + (size * 0.02f)),
                    new PointF(point.X - (size * 0.08f), point.Y + (size * 0.18f)),
                    new PointF(point.X + (size * 0.08f), point.Y + (size * 0.18f))
                };
                bkgBuffer.Graphics.FillPolygon(eyeBrush, nose);
                bkgBuffer.Graphics.DrawLine(toothPen, point.X - (size * 0.09f), jaw.Top + 2, point.X - (size * 0.09f), jaw.Bottom - 1);
                bkgBuffer.Graphics.DrawLine(toothPen, point.X + (size * 0.09f), jaw.Top + 2, point.X + (size * 0.09f), jaw.Bottom - 1);
            }

            if (matched)
            {
                DrawEllipse(new Pen(Color.White, 1.8f), point.X - offset - 4, point.Y - offset - 4, size + 8, size + 8);
            }
        }

        private void DrawGroundSpawnMarker(PointF point, bool selected)
        {
            float size = Math.Max(SpawnPlusSize + 4, 12);
            float offset = size / 2f;
            PointF[] diamond =
            {
                new PointF(point.X, point.Y - offset),
                new PointF(point.X + offset, point.Y),
                new PointF(point.X, point.Y + offset),
                new PointF(point.X - offset, point.Y)
            };

            using (var fill = new SolidBrush(Color.FromArgb(224, 202, 137, 44)))
            using (var border = new Pen(Color.FromArgb(245, 255, 236, 178), 1.3f))
            {
                bkgBuffer.Graphics.FillPolygon(MarkerShadowBrush, OffsetPoints(diamond, 1, 2));
                bkgBuffer.Graphics.FillPolygon(fill, diamond);
                bkgBuffer.Graphics.DrawPolygon(border, diamond);
            }

            if (selected)
            {
                DrawEllipse(new Pen(ModernTheme.AccentWarm, 2.4f), point.X - offset - 4, point.Y - offset - 4, size + 8, size + 8);
            }
        }

        private static PointF[] OffsetPoints(PointF[] points, float dx, float dy)
        {
            return points.Select(point => new PointF(point.X + dx, point.Y + dy)).ToArray();
        }

        private static Brush MarkerBrushFor(Spawninfo sp)
        {
            if (sp == null)
            {
                return Brushes.White;
            }

            if (sp.isPet || sp.isFamiliar || sp.isMount)
            {
                return Brushes.Gray;
            }

            return SpawnColors.ConColors[sp.Level] ?? Brushes.White;
        }

        private void DrawSpawnNames(Brush dBrush, string tName, float x1, float y1)//, string gName)
        {
            var xoffset = bkgBuffer.Graphics.MeasureString(tName, drawFont).Width * 0.5f;
            //            float goffset = bkgBuffer.Graphics.MeasureString(gName, drawFont).Width * 0.5f;

            try
            {
                bkgBuffer.Graphics.DrawString(tName, drawFont, dBrush, CalcScreenCoordX(x1) - xoffset, CalcScreenCoordY(y1) - SpawnSize - drawFont.GetHeight());
                //if (gName != "") bkgBuffer.Graphics.DrawString(gName, drawFont, dBrush, CalcScreenCoordX(x1) - goffset, CalcScreenCoordY(y1) - SpawnSize - drawFont.GetHeight());
            }
            catch (Exception ex) { LogLib.WriteLine($"Error with DrawSpawnNames({tName}, {x1}, {y1}): ", ex); }
        }

        private void DrawRectangle(Pen pen, float x1, float y1, float width, float height)
        {
            try
            {
                if (pen is null) return;
                else
                    bkgBuffer.Graphics.DrawRectangle(pen, x1, y1, width, height);
            }
            catch (Exception ex) { LogLib.WriteLine($"Error with DrawRectangle({x1}, {y1}, {width}, {height}): ", ex); }
        }

        private void DrawDashLine(DrawOptions drawOpts, PointF player)
        {
            if (selectedPoint.X == -1 || (drawOpts & DrawOptions.SpotLine) == DrawOptions.None)
                return;

            var endX = CalcScreenCoordX(selectedPoint.X);
            var endY = CalcScreenCoordY(selectedPoint.Y);

            DrawLine(new Pen(new SolidBrush(Color.White))
            {
                DashStyle = DashStyle.Dash,
                DashPattern = new float[] { 8, 4 }
            }, player.X, player.Y, endX, endY);
        }

        #endregion DrawShapes

        #region TooltipWindows

        private void TooltipTimer_Tick(object sender, EventArgs e)
        {
            canShowTooltip = true;
            tooltipTimer.Stop(); // Stop the timer until the next tooltip is shown
        }

        private void PopulateToolTip(MouseEventArgs e)
        {
            MouseMapLoc(e, out var mousex, out var mousey);
            var delta = 5.0f / Ratio;

            Spawninfo sp = FindHoverSpawn(e.Location);

            bool found;
            if (sp == null)
            {
                found = false;
            }
            else
            {
                found = true;
                string info = SpawnInfoWindow(sp).ToString();
                RefreshInfoCardForSelectedHover(sp);
                toolTip.SetToolTip(this, info);
                toolTip.AutomaticDelay = 0;
                toolTip.Active = true;
            }

            if (!found)
            {
                GroundItem gi = eq.FindGroundItem(mousex, mousey, delta);

                found = ToolTipGroundItem(found, gi);
            }

            if (!found)
            {
                Spawntimer st = eq.MobsTimers.Find(delta, mousex, mousey);
                found = ToolTipSpawnTimer(st, found);
            }

            if (!found)
            {
                toolTip.SetToolTip(this, "");
            }
        }

        private Spawninfo FindHoverSpawn(Point mousePoint)
        {
            if (IsPointOnGamer(mousePoint))
            {
                return eq.GamerInfo;
            }

            Spawninfo bestSpawn = null;
            float bestDistance = float.MaxValue;
            float hitRadius = Math.Max(12f, SpawnPlusSize + 8f);

            foreach (Spawninfo sp in eq.GetMobSnapshot())
            {
                if (!CanHitTestMapSpawn(sp))
                {
                    continue;
                }

                float screenX = CalcScreenCoordX(sp.X);
                float screenY = CalcScreenCoordY(sp.Y);
                float dx = Math.Abs(screenX - mousePoint.X);
                float dy = Math.Abs(screenY - mousePoint.Y);
                if (dx > hitRadius || dy > hitRadius)
                {
                    continue;
                }

                float distance = (dx * dx) + (dy * dy);
                bool preferSpawn = bestSpawn == null
                    || (SpawnVisibility.IsPlayer(sp) && !SpawnVisibility.IsPlayer(bestSpawn))
                    || (SpawnVisibility.IsPlayer(sp) == SpawnVisibility.IsPlayer(bestSpawn) && distance < bestDistance);

                if (preferSpawn)
                {
                    bestSpawn = sp;
                    bestDistance = distance;
                }
            }

            return bestSpawn;
        }

        private void RefreshInfoCardForSelectedHover(Spawninfo sp)
        {
            if (sp == null)
            {
                return;
            }

            bool isSelected = eq != null && eq.SelectedID == sp.SpawnID;
            if (!isSelected && f1?.SpawnList?.TryGetSelectedSpawnId(out int selectedSpawnId) == true)
            {
                isSelected = selectedSpawnId == sp.SpawnID;
            }

            if (isSelected)
            {
                SetSelectionCardSpawn(sp);
            }
        }

        private bool SelectMapSpawn(Point mousePoint)
        {
            Spawninfo sp = FindHoverSpawn(mousePoint);
            if (sp == null)
            {
                return false;
            }

            eq.SetSelectedID(sp.SpawnID);
            f1?.SpawnList.HighlightSelectedSpawn();

            if (Settings.Default.AutoSelectSpawnList && sp.listitem != null)
            {
                sp.listitem.EnsureVisible();
                sp.listitem.Selected = true;
            }

            SetSelectionCardSpawn(sp);
            return true;
        }

        private bool IsPointOnGamer(Point mousePoint)
        {
            if (eq.GamerInfo == null || eq.GamerInfo.SpawnID == 0)
            {
                return false;
            }

            float hitRadius = Math.Max(14f, SpawnPlusSize + 8f);
            float screenX = CalcScreenCoordX(eq.GamerInfo.X);
            float screenY = CalcScreenCoordY(eq.GamerInfo.Y);

            return Math.Abs(screenX - mousePoint.X) <= hitRadius
                && Math.Abs(screenY - mousePoint.Y) <= hitRadius;
        }

        private bool CanHitTestMapSpawn(Spawninfo sp)
        {
            if (sp == null || string.IsNullOrEmpty(sp.Name))
            {
                return false;
            }

            if (SpawnVisibility.IsCorpse(sp))
            {
                bool depthFilter = SpawnVisibility.IsPlayer(sp)
                    ? Settings.Default.DepthFilter && Settings.Default.FilterPlayerCorpses
                    : Settings.Default.DepthFilter && Settings.Default.FilterNPCCorpses;

                return SpawnVisibility.ShouldShowCorpse(sp)
                    && (!depthFilter || IsWithinDepthFilter(sp.Z, eq.GamerInfo.Z));
            }

            if (SpawnVisibility.IsPlayer(sp))
            {
                return Settings.Default.ShowPlayers
                    && (!FilterPlayers || IsWithinDepthFilter(sp.Z, eq.GamerInfo.Z));
            }

            return sp.flags == PacketType.Spawn && !sp.hidden && !sp.filtered;
        }

        private bool ToolTipGroundItem(bool found, GroundItem gi)
        {
            if (gi != null)

            {
                var ItemName = gi.Name;

                foreach (ListItem listItem in eq.GroundSpawn)
                {
                    if (gi.Name == listItem.ActorDef)
                    {
                        ItemName = listItem.Name;
                    }
                }

                var s = $"Name: {ItemName}\n{gi.Name}";

                toolTip.SetToolTip(this, s);

                toolTip.AutomaticDelay = 0;

                toolTip.Active = true;

                found = true;
            }

            return found;
        }

        private bool ToolTipSpawnTimer(Spawntimer st, bool found)
        {
            if (st != null)
            {
                var description = st.GetDescription();
                if (description != null)
                {
                    toolTip.SetToolTip(this, description);
                    toolTip.AutomaticDelay = 0;
                    toolTip.Active = true;
                }
                found = true;
            }

            return found;
        }

        private string MobInfo(Spawninfo si, bool SetColor, bool ChangeSize)
        {
            if (f1 == null)
            { return ""; }

            if (si == null)
            {
                return NoSpawnInfo(ChangeSize);
            }

            StringBuilder mobInfo = SpawnInfoWindow(si);

            if (SetColor)
            {
                InfoSetColor(si);
            }

            tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Absolute;
            string info = MobshowInfo(mobInfo);
            CacheSelectedInfo(si.SpawnID, info);
            return info;
        }

        private string NoSpawnInfo(bool ChangeSize)
        {
            if (!string.IsNullOrWhiteSpace(lastInfoText))
            {
                return lastInfoText;
            }

            if (TryBuildSelectedListInfo(out string selectedListInfo))
            {
                return selectedListInfo;
            }

            if (ChangeSize)
            {
                tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Absolute;

                tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Absolute;

                if (Settings.Default.ShowTargetInfo)
                {
                    MeasureStrings("Spawn Information Window", out SizeF gt, out SizeF gf);

                    tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Absolute;

                    tableLayoutPanel1.Width = (int)gf.Width + 40;
                    tableLayoutPanel1.ColumnStyles[0].Width = (int)gf.Width + 40;

                    tableLayoutPanel1.RowStyles[0].Height = (int)gt.Height + 7;
                }
            }
            MobInfoLabel.BackColor = Color.FromArgb(46, 53, 63);
            MobInfoLabel.ForeColor = Color.FromArgb(232, 238, 244);
            lblGameClock.BackColor = ModernTheme.Accent;
            tableLayoutPanel1.BringToFront();
            return "No spawn selected";
        }

        private void MeasureStrings(string label, out SizeF gt, out SizeF gf)
        {
            Graphics graphics = MobInfoLabel.CreateGraphics();

            gt = graphics.MeasureString(lblGameClock.Text, lblGameClock.Font);
            gf = graphics.MeasureString(label, MobInfoLabel.Font);
            graphics.Dispose();
        }

        private string MobshowInfo(StringBuilder mobInfo)
        {
            MeasureStrings(mobInfo.ToString(), out SizeF sc, out SizeF sf);

            tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Absolute;
            tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Absolute;
            if (Settings.Default.ShowTargetInfo)
            {
                MobInfoLabel.Visible = true;
                AdjustTableLayout(Settings.Default.SmallTargetInfo ? 22 : 30, sf, sc);
                tableLayoutPanel1.BringToFront();
            }
            else
            {
                tableLayoutPanel1.Width = (int)sc.Width + 10;
                tableLayoutPanel1.ColumnStyles[0].Width = (int)sc.Width + 10;
                tableLayoutPanel1.RowStyles[0].Height = 0;
                tableLayoutPanel1.RowStyles[1].Height = 0;
            }

            return mobInfo.ToString();
        }

        private void InfoSetColor(Spawninfo si)
        {
            if (si.Level < (conColor.GreyRange + eq.GamerInfo.Level))
            {
                lblGameClock.BackColor = Color.FromArgb(150, 150, 150);
            }
            else if (si.Level < (conColor.GreenRange + eq.GamerInfo.Level))
            {
                lblGameClock.BackColor = Color.FromArgb(80, 150, 92);
            }
            else if (si.Level < (conColor.CyanRange + eq.GamerInfo.Level))
            {
                lblGameClock.BackColor = Color.FromArgb(68, 154, 174);
            }
            else if (si.Level < eq.GamerInfo.Level)
            {
                lblGameClock.BackColor = Color.FromArgb(64, 126, 205);
            }
            else if (si.Level == eq.GamerInfo.Level)
            {
                lblGameClock.BackColor = Color.FromArgb(232, 238, 244);
            }
            else
            {
                lblGameClock.BackColor = si.Level <= eq.GamerInfo.Level + conColor.YellowRange ? Color.FromArgb(202, 137, 44) : Color.FromArgb(190, 73, 64);
            }

            if (si.isEventController)
            {
                lblGameClock.BackColor = Color.FromArgb(132, 82, 170);
            }

            if (si.isLDONObject)
            {
                lblGameClock.BackColor = Color.FromArgb(150, 150, 150);
            }

            MobInfoLabel.BackColor = Color.FromArgb(46, 53, 63);
            MobInfoLabel.ForeColor = Color.FromArgb(232, 238, 244);
        }

        private StringBuilder SpawnInfoWindow(Spawninfo si)
        {
            StringBuilder mobInfo = new StringBuilder();

            if (Settings.Default.SmallTargetInfo)
            {
                SmallWindow(si, mobInfo);
            }
            else
            {
                // long target window version
                LargeWindow(si, mobInfo);
            }

            return mobInfo;
        }

        private void LargeWindow(Spawninfo si, StringBuilder mobInfo)
        {
            mobInfo.AppendFormat("{0}\n", si.Name.FixMobName());
            mobInfo.AppendFormat("{0}  Level {1}  {2}\n", SpawnKind(si), si.Level, si.Hide.GetHideStatus());
            mobInfo.AppendFormat("{0} / {1}\n", eq.GetRace(si.Race), eq.GetClass(si.Class));
            mobInfo.AppendFormat("Dist {0:f0}  Speed {1:f2}  ID {2}\n", SpawnDistance(si), si.SpeedRun, si.SpawnID);
            mobInfo.AppendFormat("Y {0:f1}  X {1:f1}  Z {2:f1}", si.Y, si.X, si.Z);

            if (si.Primary > 0 || si.Offhand > 0)
            {
                mobInfo.AppendFormat("\n{0}{1}",
                    si.Primary > 0 ? $"Primary {si.PrimaryName} " : "",
                    si.Offhand > 0 ? $"Offhand {si.OffhandName}" : "");
            }
        }

        private void SmallWindow(Spawninfo si, StringBuilder mobInfo)
        {
            mobInfo.AppendFormat("{0}: {1}\n", SpawnKind(si), si.Name.FixMobName());
            mobInfo.AppendFormat("L{0} {1}  Dist {2:f0}\n", si.Level, eq.GetClass(si.Class), SpawnDistance(si));
            mobInfo.AppendFormat("Y {0:f1} X {1:f1} Z {2:f1}", si.Y, si.X, si.Z);
        }

        private static string SpawnKind(Spawninfo si)
        {
            if (si.isMerc) return "Merc";
            if (si.isPet) return "Pet";
            if (si.isFamiliar) return "Familiar";
            if (si.isMount) return "Mount";
            if (SpawnVisibility.IsPlayer(si)) return "Player";
            if (si.isCorpse) return "Corpse";
            if (si.isEventController) return "Controller";
            if (si.isLDONObject) return "Object";
            return "NPC";
        }

        public void ResetInfoWindow()
        {
            MobInfoLabel.Text = "Spawn Information Window";

            MobInfoLabel.BackColor = Color.FromArgb(46, 53, 63);
            MobInfoLabel.ForeColor = Color.FromArgb(232, 238, 244);
            lblGameClock.BackColor = ModernTheme.Accent;

            MobInfoLabel.Visible = true;
        }

        private float SpawnDistance(Spawninfo si)
        {
            var dx = si.X - eq.GamerInfo.X;
            var dy = si.Y - eq.GamerInfo.Y;
            var dz = si.Z - eq.GamerInfo.Z;

            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        #endregion TooltipWindows

        #region DrawSpawns

        private bool NPCDepthFilter = Settings.Default.DepthFilter && Settings.Default.FilterNPCs;
        private bool FilterPlayers = Settings.Default.DepthFilter && Settings.Default.FilterPlayers;

        private void DrawSpawns(DrawOptions DrawOpts, float pX, float pY, float pZ, PointF player)
        {
            if ((DrawOpts & DrawOptions.Spawns) != DrawOptions.None)
            {
                //            string gName;
                var ShowRings = (DrawOpts & DrawOptions.SpawnRings) != DrawOptions.None;

                var DrawDirection = (DrawOpts & DrawOptions.DirectionLines) != DrawOptions.None;
                var colorRangeCircle = Settings.Default.AlertInsideRangeCircle;
                if ((eq.SelectedID == 99999) && (eq.SpawnX == -1))
                {
                    MobInfoLabel.Text = SelectedListInfo();
                }
                else if (eq.SelectedID != 99999 && !eq.TryGetMobBySpawnId(eq.SelectedID, out _))
                {
                    MobInfoLabel.Text = SelectedListInfo();
                }

                // Collect mob trails, every 8th pass - approx once every 1 sec
                map.trails.CountMobTrails(eq);
                // Draw Spawns

                foreach (Spawninfo sp in eq.GetMobSnapshot())
                {
                    var sPoint = new PointF(
                    (float)Math.Round(CalcScreenCoordX(sp.X), 0),
                    (float)Math.Round(CalcScreenCoordY(sp.Y), 0));
                    //                gName = eq.GuildNumToString(sp.Guild);

                    // Draw Line from Gamer to the Selected Spawn
                    if (eq.SelectedID == sp.SpawnID)
                    {
                        LineGamerToSelected(player.X, player.Y, sp, sPoint.X, sPoint.Y);
                    }
                    else if (colorRangeCircle && Settings.Default.RangeCircle > 0)
                    {
                        ProxAlert(pX, pY, pZ, sp);
                    }
                    else
                    {
                        sp.proxAlert = false;
                    }

                    // Draw All Other Spawns

                    if (sp.flags == 0 && sp.Name.Length > 0)
                    {
                        if (curTarget == sp.Name)
                        {
                            DrawEllipse(new Pen(Color.White, 2.2f), sPoint.X - PlusSzOZ - 5, sPoint.Y - PlusSzOZ - 5, SpawnPlusSize + 10, SpawnPlusSize + 10);
                        }

                        if (sp.isEventController)
                        {
                            DrawSpecialMobs(pZ, sp, sPoint.X, sPoint.Y, Color.Purple);
                        }
                        else if (sp.isLDONObject)
                        {
                            DrawSpecialMobs(pZ, sp, sPoint.X, sPoint.Y, Color.Gray);
                        }
                        else if (SpawnVisibility.IsPlayer(sp))
                        {
                            // Draw Other Players

                            DrawOtherPlayers(DrawOpts, pZ, sp, sPoint.X, sPoint.Y);
                        }
                        else if (sp.Type == 1 || sp.Type == 4)
                        {
                            DrawNPCs(pZ, DrawDirection, sp, sPoint.X, sPoint.Y);
                        }
                        DrawRings(sPoint.X, sPoint.Y, sp);
                        DrawFlashes(pZ, sPoint.X, sPoint.Y, sp);
                        MarkSpecial(pZ, sPoint.X, sPoint.Y, ShowRings, sp);
                    }
                }
            }
        }

        internal Font GetdrawFont() => drawFont;

        internal void SetDrawFont(Font newfont)
        {
            drawFont = newfont;
            if (drawFont != null)
            {
                drawFont1 = new Font(drawFont.Name, drawFont.Size * 0.9f, drawFont.Style);
                drawFont3 = new Font(drawFont.Name, drawFont.Size * 1.1f, drawFont.Style);
            }
        }

        private void DrawNPCs(float pZ, bool DrawDirection, Spawninfo sp, float x, float y)
        {
            if (SpawnVisibility.ShouldHide(sp))
            {
                sp.filtered = true;
                return;
            }

            if ((!Settings.Default.DepthFilter && Settings.Default.FilterNPCs) || IsWithinDepthFilter(sp.Z, pZ))
            {
                sp.filtered = false;

                if (sp.Name.Length > 0)
                {
                    if (Settings.Default.ShowNPCNames)
                    {
                        DrawSpawnNames(textBrush, sp.Name, sp.X, sp.Y);//, gName);
                    }
                    if (Settings.Default.ShowNPCLevels)
                    {
                        // Adjusting the Y-coordinate to prevent overlap with the name, placing level below spawnpoint
                        DrawSpawnNames(textBrush, sp.Level.ToString(), sp.X, sp.Y - 15);//, gName);
                    }
                }
                if (DrawDirection)
                {
                    DrawDirectionLines(sp, x, y);
                }

                DrawModernCircleMarker(x, y, MarkerBrushFor(sp), eq.SelectedID == sp.SpawnID, false);

                // Draw PC color border around Mercenary

                if (sp.isMerc)
                {
                    DrawEllipse(PCBorder, x - SpawnSizeOffset, y - SpawnSizeOffset, SpawnSize, SpawnSize);
                }

                // Draw Purple border around invis mobs

                PurpleBorder(sp, x, y);
            }
            else
            {
                sp.filtered = true;
            }
        }

        private void PurpleBorder(Spawninfo sp, float x, float y)
        {
            if (sp.Hide != 0)
            {
                // Flashing purple ring around SoS mobs

                if (sp.Hide == 2)
                {
                    if (flash)
                    {
                        DrawEllipse(WhitePen, x - SelectSizeOffset, y - SelectSizeOffset, SelectSize, SelectSize);
                    }
                }
                else
                {
                    DrawEllipse(PurplePen, x - SelectSizeOffset, y - SelectSizeOffset, SelectSize, SelectSize);
                }
            }
        }

        private void DrawOtherPlayers(DrawOptions DrawOpts, float pZ, Spawninfo sp, float x, float y)
        {
            if (SpawnVisibility.ShouldHide(sp))
            {
                sp.filtered = true;
                return;
            }

            if (!FilterPlayers || IsWithinDepthFilter(sp.Z, pZ))
            {
                sp.filtered = false;
                if (SpawnColors.ConColors[sp.Level] != null)
                {
                    if ((DrawOpts & DrawOptions.DirectionLines) != DrawOptions.None)
                    {
                        DrawDirectionLines(sp, x, y);
                    }

                    // Draw Other Players

                    if (Settings.Default.ShowPVP)
                    {
                        if ((Math.Abs(eq.GamerInfo.Level - sp.Level) <= Settings.Default.PVPLevels) || (Settings.Default.PVPLevels == -1))
                        {
                            DrawPVP(sp, x, y);
                        }
                    }
                    else
                    {
                        DrawPlayer(sp, x, y);
                    }
                }
            }
            else
            {
                sp.filtered = true;
            }
        }

        private void DrawPlayer(Spawninfo sp, float x, float y)
        {
            DrawModernPlayerMarker(x, y, MarkerBrushFor(sp), eq.SelectedID == sp.SpawnID, false);

            // draw purple border around players

            DrawStealthorInvisPlayer(sp, x, y);

            if (ShowPCName && (sp.Name.Length > 0))
            {
                DrawSpawnNames(textBrush, $"{sp.Level}: {sp.Name}", sp.X, sp.Y);//, gName);
            }
            //else if (Settings.Default.ShowPCGuild && (gName.Length > 0))
            //{ DrawSpawnNames(textBrush, gName, sp.X, sp.Y); }//, gName); }
        }

        private void DrawStealthorInvisPlayer(Spawninfo sp, float x, float y)
        {
            if (sp.Hide != 0)
            {
                if (sp.Hide == 2)
                {
                    // SoS Players

                    if (flash)
                    {
                        DrawRectangle(WhitePen, x - PlusSzOZ - 0.5f, y - PlusSzOZ - 0.5f, SpawnPlusSize + 2.0f, SpawnPlusSize + 2.0f);
                    }
                }
                else
                {
                    // Player is invis

                    DrawRectangle(PurplePen, x - PlusSzOZ - 0.5f, y - PlusSzOZ - 0.5f, SpawnPlusSize + 2.0f, SpawnPlusSize + 2.0f);
                }
            }
        }

        private void DrawPVP(Spawninfo sp, float x, float y)
        {
            DrawModernPlayerMarker(x, y, MarkerBrushFor(sp), eq.SelectedID == sp.SpawnID, false);

            // Handle name drawing based on settings
            if (ShowPCName && !string.IsNullOrEmpty(sp.Name))
            {
                DrawSpawnNames(textBrush, $"{sp.Level}: {sp.Name}", sp.X, sp.Y);
            }
            else if (Settings.Default.ShowPVPLevel)
            {
                DrawSpawnNames(textBrush, sp.Level.ToString(), sp.X, sp.Y);
            }

            // Draw flashing ellipse if enabled
            if (flash)
            {
                using (Pen cPen = new Pen(eq.GetDistinctColor(Color.White)))
                {
                    DrawEllipse(cPen, x - SelectSizeOffset, y - SelectSizeOffset, SelectSize, SelectSize);
                }
            }
        }

        private void DrawCorpses(DrawOptions drawOpts, float pZ)
        {
            if ((drawOpts & DrawOptions.Spawns) == DrawOptions.None)
            {
                return; // No need to proceed if spawns shouldn't be drawn
            }

            var pcCorpseDepthFilter = Settings.Default.DepthFilter && Settings.Default.FilterPlayerCorpses;
            var npcCorpseDepthFilter = Settings.Default.DepthFilter && Settings.Default.FilterNPCCorpses;

            // Iterate through all spawns
            foreach (Spawninfo sp in eq.GetMobSnapshot())
            {
                if (!ShouldDrawCorpse(sp))
                {
                    continue; // Skip non-corpse or hidden spawns
                }

                var corpsePoint = new PointF(
                            (float)Math.Round(CalcScreenCoordX(sp.X), 0),
                            (float)Math.Round(CalcScreenCoordY(sp.Y), 0));

                sp.proxAlert = false;

                if (SpawnVisibility.IsPlayer(sp))
                {
                    HandleCorpseDrawing(sp, corpsePoint, pcCorpseDepthFilter, pZ, Color.Yellow, Settings.Default.ShowPlayerCorpseNames);
                }
                else
                {
                    HandleCorpseDrawing(sp, corpsePoint, npcCorpseDepthFilter, pZ, Color.Cyan, Settings.Default.ShowNPCCorpseNames);
                }
            }
        }

        private bool ShouldDrawCorpse(Spawninfo sp)
        {
            return SpawnVisibility.ShouldShowCorpse(sp);
        }

        private void HandleCorpseDrawing(Spawninfo sp, PointF corpsePoint, bool depthFilter, float pZ, Color color, bool showNames)
        {
            if (depthFilter && !IsWithinDepthFilter(sp.Z, pZ))
            {
                sp.filtered = true;
                return;
            }

            sp.filtered = false;
            DrawCorpseShape(corpsePoint, color, false);

            if (showNames && !string.IsNullOrEmpty(sp.Name))
            {
                DrawSpawnNames(textBrush, $"{sp.Level}: {sp.Name}", sp.X, sp.Y);
            }
        }

        private bool IsWithinDepthFilter(float z, float pZ)
        {
            return z > pZ - filterneg && z < pZ + filterpos;
        }

        private bool IsWithinDepthFilter(float z, float minZ, float maxZ)
        {
            // Check if the Z value is within the specified depth range.
            return z >= minZ && z <= maxZ;
        }

        private void DrawCorpseShape(PointF corpsePoint, Color color, bool matched)
        {
            DrawModernCorpseMarker(corpsePoint, color, matched);
        }

        private void DrawSpecialMobs(float pZ, Spawninfo sp, float x, float y, Color color)
        {
            if (!NPCDepthFilter || IsWithinDepthFilter(sp.Z, pZ))
            {
                using (var brush = new SolidBrush(color))
                {
                    DrawModernCircleMarker(x, y, brush, eq.SelectedID == sp.SpawnID, false);
                }
                sp.filtered = false;
            }
            else
            {
                sp.filtered = true;
            }
        }

        private void LineGamerToSelected(float playerx, float playery, Spawninfo sp, float x, float y)
        {
            DrawEllipse(SelectionPen, x - SelectSizeOffset, y - SelectSizeOffset, SelectSize, SelectSize);
            SetSelectionCardSpawn(sp);
            DrawLine(SelectionPen, playerx, playery, x, y);

            sp.proxAlert = false;
        }

        private void ProxAlert(float pX, float pY, float pZ, Spawninfo sp)
        {
            // Validate if the spawn should be alertable or if it's a non-alertable type.
            if (!sp.alertMob || sp.Type == 2)
            {
                sp.proxAlert = false;
                return;
            }

            // Determine the minimum level required for an alert.
            int minLevel = (Settings.Default.MinAlertLevel == -1)
                ? eq.GamerInfo.Level + conColor.GreyRange
                : Settings.Default.MinAlertLevel;

            // Exit early if the spawn level is below the minimum alert level.
            if (sp.Level < minLevel)
            {
                sp.proxAlert = false;
                return;
            }

            // Calculate the Z-axis range for proximity checks.
            float range = Settings.Default.RangeCircle;
            float adjustedMinZ = pZ - range;
            float adjustedMaxZ = pZ + range;

            // Apply a depth filter adjustment if required.
            if (NPCDepthFilter)
            {
                adjustedMinZ = pZ - filterpos;
                adjustedMaxZ = pZ + filterpos;
            }

            // Determine proximity state and handle enter or exit accordingly.
            if (sp.proxAlert)
            {
                HandleProximityChange(pX, pY, sp, adjustedMinZ, adjustedMaxZ, range, isEntering: false);
            }
            else
            {
                HandleProximityChange(pX, pY, sp, adjustedMinZ, adjustedMaxZ, range, isEntering: true);
            }
        }

        private void HandleProximityChange(float pX, float pY, Spawninfo sp, float minZ, float maxZ, float range, bool isEntering)
        {
            // Adjust range and depth multipliers based on whether we're handling entering or exiting.
            float rangeMultiplier = isEntering ? 1.0f : 1.4f;
            float depthMultiplier = isEntering ? 1.0f : 1.2f;

            // Check if the spawn is within the desired depth and range.
            if (IsWithinDepthFilter(sp.Z, minZ * depthMultiplier, maxZ * depthMultiplier) &&
                IsWithinRange(pX, pY, sp.X, sp.Y, range * rangeMultiplier))
            {
                if (isEntering)
                {
                    // Enter proximity: enable proximity alert and trigger sound settings.
                    sp.proxAlert = true;
                    if (Settings.Default.playAlerts)
                    {
                        new FormMethods().SwitchOnSoundSettings();
                    }
                }
            }
            else if (!isEntering)
            {
                // Exit proximity: disable proximity alert if leaving the range.
                sp.proxAlert = false;
            }
        }

        private bool IsWithinRange(float pX, float pY, float spX, float spY, float range)
        {
            // Calculate the squared distance and check against the squared range to avoid using sqrt.
            var distanceSquared = (pX - spX) * (pX - spX) + (pY - spY) * (pY - spY);
            return distanceSquared < (range * range);
        }

        private void MarkSpecial(float pZ, float x, float y, bool ShowRings, Spawninfo sp)
        {
            if (ShowRings && (!NPCDepthFilter || !IsWithinDepthFilter(sp.Z, pZ)))
            {
                if (sp.Class == 40)// Draw Ring around Bankers
                {
                    DrawEllipse(WhitePen, x - SpawnSizeOffset, y - SpawnSizeOffset, SpawnSize, SpawnSize);

                    DrawEllipse(GreenPen, x - PlusSzOZ, y - PlusSzOZ, SpawnPlusSize, SpawnPlusSize);
                }
                if (sp.Class > 19 && sp.Class < 35)                // Draw Ring around Guild Master
                {
                    DrawEllipse(WhitePen, x - SpawnSizeOffset, y - SpawnSizeOffset, SpawnSize, SpawnSize);

                    DrawEllipse(RedPen, x - PlusSzOZ, y - PlusSzOZ, SpawnPlusSize, SpawnPlusSize);
                }

                if (sp.Class == 41)                // Draw Ring around Shopkeepers
                {
                    DrawEllipse(WhitePen, x - SpawnSizeOffset, y - SpawnSizeOffset, SpawnSize, SpawnSize);

                    DrawEllipse(BluePen, x - PlusSzOZ, y - PlusSzOZ, SpawnPlusSize, SpawnPlusSize);
                }
            }
        }

        private void DrawFlashes(float pZ, float x, float y, Spawninfo sp)
        {
            if (flash)
            {
                var x1 = x - PlusSzOZ;
                var y1 = y - PlusSzOZ;
                var above = sp.Z < pZ + filterpos;
                var below = sp.Z > pZ - filterneg;

                // Draw Ring around Hunted Mobs

                if (!NPCDepthFilter || (below && above))
                {
                    if (sp.isHunt || sp.proxAlert)
                    {
                        DrawEllipse(HuntAlertPen, x1, y1, SpawnPlusSize, SpawnPlusSize);
                    }

                    // Draw Ring around Caution Mobs

                    if (sp.isCaution)
                    {
                        DrawEllipse(CautionAlertPen, x1, y1, SpawnPlusSize, SpawnPlusSize);
                    }

                    // Draw Ring around Danger Mobs

                    if (sp.isDanger)
                    {
                        DrawEllipse(DangerAlertPen, x1, y1, SpawnPlusSize, SpawnPlusSize);
                    }

                    // Draw Ring around Rare Mobs

                    if (sp.isAlert)
                    {
                        DrawEllipse(RareAlertPen, x1, y1, SpawnPlusSize, SpawnPlusSize);
                    }
                }
            }
        }

        private void DrawRings(float x, float y, Spawninfo sp)
        {
            //            string gName = eq.GuildNumToString(sp.Guild);
            if (!sp.isLookup || (sp.isCorpse && !Settings.Default.CorpseAlerts))
            {
                return;
            }

            float lookupRingSize = SpawnPlusSize + (skittle / (float)UpdateSteps * SelectSize);
            DrawEllipse(LookupPen, x - lookupRingSize / 2.0f, y - lookupRingSize / 2.0f, lookupRingSize, lookupRingSize);

            if (Settings.Default.ShowLookupText)
            {
                string displayName = Settings.Default.ShowLookupNumber ? sp.lookupNumber : sp.Name;
                DrawSpawnNames(textBrush, displayName, sp.X, sp.Y);
            }
        }

        public void DrawSpawnTrails()
        {
            try
            {
                // Draw Mob Trails only if there are any
                if (map.trails.GetMobTrailsReadonly().Any())
                {
                    foreach (MobTrailPoint mtp in map.trails.GetMobTrailsReadonly())
                    {
                        FillEllipse(WhiteBrush, CalcScreenCoordX(mtp.X) - 2, CalcScreenCoordY(mtp.Y) - 2, 2, 2);
                    }
                }
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in DrawSpawnTrails(): ", ex);
            }
        }

        #endregion DrawSpawns

        #region DrawMap

        public void DrawMapLines(DrawOptions DrawOpts)
        {
            try
            {
                // Draw Zone Map
                if (eq.Longname != "" && ((DrawOpts & DrawOptions.DrawMap) != DrawOptions.None))
                {
                    if (!Settings.Default.DepthFilter || (Settings.Default.DepthFilter && !Settings.Default.FilterMapLines))
                    // No depth filtering
                    {
                        foreach (LineSegment mapLine in mapData.LineSegments)
                        {
                            DrawLines(mapLine.LineColor, mapLine.Start, mapLine.End);
                        }
                    }
                    else
                    {
                        MinMaxFilter(out var minZ, out var maxZ);

                        foreach (LineSegment mapLine in mapData.LineSegments)
                        {
                            // All the points in this set of lines are good
                            if (mapLine.Start.Z > minZ || mapLine.Start.Z < maxZ || mapLine.End.Z > minZ || mapLine.End.Z < maxZ)
                            {
                                DrawLines(mapLine.LineColor, mapLine.Start, mapLine.End);
                            }
                            else if (mapLine.Start.Z < minZ || mapLine.Start.Z > maxZ || mapLine.End.Z < minZ || mapLine.End.Z < maxZ)
                            {
                                DrawLines(mapLine.LineColor, mapLine.Start, mapLine.End);
                            }
                            else
                            {
                                AlphaFiltering(minZ, maxZ, mapLine);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in DrawMapLines(): ", ex); }
        }

        private void AlphaFiltering(float minZ, float maxZ, LineSegment mapLine)
        {
            bool curValid, lastValid;

            float curX, curY, curZ, lastX, lastY, lastZ;

            lastX = mapLine.Start.X;

            lastY = mapLine.Start.Y;

            lastZ = mapLine.Start.Z;

            lastValid = (lastZ > minZ) && (lastZ < maxZ);

            for (var d = 1; d < mapData.LineSegments.Count; d++)
            {
                curX = mapLine.Start.X;

                curY = mapLine.Start.Y;

                curZ = mapLine.Start.Z;

                curValid = (curZ > minZ) && (curZ < maxZ);

                // Original Depth Filter method (use z-axis values only)

                // instead of not drawing filtered lines, we draw light ones

                if (!curValid && !lastValid)
                {
                    if (Settings.Default.UseDynamicAlpha)
                    {
                        var alpha = Settings.Default.FadedLines * 255 / 100;
                        using (Pen Fade_color = new Pen(Color.FromArgb(alpha, mapLine.LineColor)))
                        { DrawLine(Fade_color, lastX, lastY, curX, curY); }
                    }
                }
                else
                {
                    DrawLine(new Pen(mapLine.LineColor), lastX, lastY, curX, curY);
                }

                lastX = curX;

                lastY = curY;

                lastValid = curValid;
            }
        }

        private void MinMaxFilter(out float minZ, out float maxZ)
        {
            minZ = eq.GamerInfo.Z - filterneg;
            maxZ = eq.GamerInfo.Z + filterpos;
        }

        public void DrawMap(DrawOptions DrawOpts)
        {
            try
            {
                if ((DrawOpts & DrawOptions.GridLines) != DrawOptions.None)
                    DrawGridLines();

                if ((DrawOpts & DrawOptions.ZoneText) != DrawOptions.None)
                {
                    DepthfilterText();
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in DrawMap(): ", ex); }
        }

        private void DrawGridLines()
        {
            var gridInterval = Settings.Default.GridInterval;
            var curGridColor = Settings.Default.GridColor;
            var gridLabelColor = Settings.Default.GridLabelColor;

            using (var gridPen = new Pen(curGridColor))
            using (var gridBrush = new SolidBrush(gridLabelColor))
            {
                // Cache label font height for consistent spacing and avoid repeated method calls.
                float labelHeight = drawFont.GetHeight() + 5;
                DrawGridLinesInternal(gridInterval, gridPen, gridBrush, labelHeight, true);
                DrawGridLinesInternal(gridInterval, gridPen, gridBrush, labelHeight, false);
            }
        }

        private void DrawGridLinesInternal(int gridInterval, Pen gridPen, Brush gridBrush, float labelHeight, bool isHorizontal)
        {
            int start, end, screenLimit;
            Func<int, float> CalcScreenCoord;
            Func<int, string> GetLabelStr = label => label.ToString();

            if (isHorizontal)
            {
                // Calculate start and end for horizontal lines.
                start = (int)(MinmapX / gridInterval) - 2;
                end = (int)(MaxMapX / gridInterval) + 2;
                screenLimit = Height;
                CalcScreenCoord = label => (float)Math.Round(CalcScreenCoordX(label), 0);
            }
            else
            {
                // Calculate start and end for vertical lines.
                start = (int)(MinMapY / gridInterval) - 1;
                end = (int)(MaxMapY / gridInterval) + 2;
                screenLimit = Width;
                CalcScreenCoord = label => (float)Math.Round(CalcScreenCoordY(label), 0);
            }

            for (int i = start; i < end; i++)
            {
                int label = i * gridInterval;
                string labelStr = GetLabelStr(label);
                float screenCoord = CalcScreenCoord(label);

                if (isHorizontal)
                {
                    // Draw vertical grid lines for horizontal labels.
                    DrawLine(gridPen, screenCoord, 0, screenCoord, screenLimit);
                    DrawLabel(labelStr, gridBrush, screenCoord, screenLimit - labelHeight);
                }
                else
                {
                    // Draw horizontal grid lines for vertical labels.
                    DrawLine(gridPen, 0, screenCoord, screenLimit, screenCoord);
                    float labelWidth = bkgBuffer.Graphics.MeasureString(labelStr, drawFont).Width;
                    DrawLabel(labelStr, gridBrush, screenLimit - (labelWidth + 5), screenCoord);
                }
            }
        }

        private void DrawLabel(string text, Brush brush, float x, float y)
        {
            bkgBuffer.Graphics.DrawString(text, drawFont, brush, x, y);
        }

        private void DepthfilterText()
        {
            // Draw Zone Text
            if (Settings.Default.DepthFilter && Settings.Default.FilterMapText)
            {
                // Depth Filter
                MinMaxFilter(out var minZ, out var maxZ);

                foreach (MapLabel label in mapData.Labels)
                {
                    if (label.Position.Z != -99999 && label.Position.Z > minZ && label.Position.Z < maxZ)
                    {
                        AddTextToDrawnMap(label);
                    }
                }
            }
            else
            {
                // No Depth Filtering
                foreach (MapLabel text in mapData.Labels)
                {
                    AddTextToDrawnMap(text);
                }
            }
        }

        private void AddTextToDrawnMap(MapLabel label)
        {
            try
            {
                var x_cord = (int)CalcScreenCoordX(label.Position.X);
                var y_cord = (int)CalcScreenCoordY(label.Position.Y);
                if (Settings.Default.MapLabel.Size == 2)
                {// check for null
                    bkgBuffer.Graphics.DrawString(label.Text, drawFont, new SolidBrush(label.TextColor), x_cord, y_cord);
                }
                else if (Settings.Default.MapLabel.Size == 1)
                {
                    bkgBuffer.Graphics.DrawString(label.Text, drawFont1, new SolidBrush(label.TextColor), x_cord, y_cord);
                }
                else
                {
                    bkgBuffer.Graphics.DrawString(label.Text, drawFont3, new SolidBrush(label.TextColor), x_cord, y_cord);
                }
                using (Pen pen = new Pen(label.TextColor))
                {
                    bkgBuffer.Graphics.DrawLine(pen, x_cord - 1, y_cord, x_cord + 1, y_cord);
                    bkgBuffer.Graphics.DrawLine(pen, x_cord, y_cord - 1, x_cord, y_cord + 1);
                }
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in AddText" + ex.StackTrace, ex);
            }
        }

        #endregion DrawMap

        #region DrawGamer

        public void DrawGamer(PointF gamer, float SpawnSize, float SpawnSizeOffset, DrawOptions DrawOpts)
        {
            try
            {
                var xHead = (int)eq.GamerInfo.Heading;

                // Draw Range Circle

                if (Settings.Default.RangeCircle > 0)

                {
                    var rCircleRadius = Settings.Default.RangeCircle * Ratio;
                    MakeRangeCircle(gamer.X, gamer.Y, rCircleRadius);

                    // Draw Red V in the Range Circle

                    if (Settings.Default.DrawFoV && xHead >= 0 && xHead < 512)

                    {
                        DrawFoV(gamer.X, gamer.Y, xHead, rCircleRadius);
                    }

                    if (m_rangechange)

                    {
                        if (flash)
                        {
                            DrawEllipse(eq.GetDistinctColor(new Pen(Settings.Default.RangeCircleColor)), gamer.X - rCircleRadius, gamer.Y - rCircleRadius, rCircleRadius * 2, rCircleRadius * 2);
                        }
                    }
                    else
                    {
                        DrawEllipse(eq.GetDistinctColor(new Pen(Settings.Default.RangeCircleColor)), gamer.X - rCircleRadius, gamer.Y - rCircleRadius, rCircleRadius * 2, rCircleRadius * 2);
                    }
                }

                // Draw Player  (only if we actually have a player)

                if (eq.GamerInfo.SpawnID != 0)

                {
                    // Draw Player Heading Line

                    if ((DrawOpts & DrawOptions.DirectionLines) != DrawOptions.None && xHead >= 0 && xHead < 512)

                    {
                        var y1 = -(xCos[xHead] * (eq.GamerInfo.SpeedRun * Ratio * 100));

                        var x1 = -(xSin[xHead] * (eq.GamerInfo.SpeedRun * Ratio * 100));

                        DrawLine(WhitePen, gamer.X, gamer.Y, gamer.X + x1, gamer.Y + y1);
                    }

                    FillRectangle(WhiteBrush, gamer.X - SpawnSizeOffset, gamer.Y - SpawnSizeOffset, SpawnSize, SpawnSize);

                    DrawRectangle(PCBorder, gamer.X - SpawnSizeOffset - 0.5f, gamer.Y - SpawnSizeOffset - 0.5f, SpawnSize + 1.0f, SpawnSize + 1.0f);
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in DrawPlayer(): ", ex); }
        }

        private void DrawFoV(float gamerX, float gamerY, int xHead, float rCircleRadius)
        {
            float x, y;

            if (xHead < 448)
            {
                y = -(xCos[xHead + 64] * rCircleRadius * 1.05f);
                x = -(xSin[xHead + 64] * rCircleRadius * 1.05f);
            }
            else
            {
                y = -(float)(xCos[xHead + 64 - 512] * rCircleRadius * 1.05);

                x = -(float)(xSin[xHead + 64 - 512] * rCircleRadius * 1.05);
            }

            DrawLine(RedPen, gamerX, gamerY, gamerX + x, gamerY + y);

            if (xHead >= 64)

            {
                y = -(xCos[xHead - 64] * rCircleRadius * 1.05f);

                x = -(xSin[xHead - 64] * rCircleRadius * 1.05f);
            }
            else
            {
                y = -(xCos[xHead - 64 + 512] * rCircleRadius * 1.05f);

                x = -(xSin[xHead - 64 + 512] * rCircleRadius * 1.05f);
            }

            DrawLine(RedPen, gamerX, gamerY, gamerX + x, gamerY + y);

            // Draw Heading Line

            y = -(xCos[xHead] * rCircleRadius);

            x = -(xSin[xHead] * rCircleRadius);

            DrawLine(YellowPen, gamerX, gamerY, gamerX + x, gamerY + y);
        }

        private void MakeRangeCircle(float gamerx, float gamery, float rCircleRadius)
        {
            if (Settings.Default.AlertInsideRangeCircle)
            {
                HatchStyle hs = (HatchStyle)Enum.Parse(typeof(HatchStyle), Settings.Default.HatchIndex, true);

                HatchBrush hatchBrush = new HatchBrush(hs, Settings.Default.RangeCircleColor, Color.Transparent);

                FillEllipse(hatchBrush, gamerx - rCircleRadius, gamery - rCircleRadius, rCircleRadius * 2, rCircleRadius * 2);
            }
        }

        #endregion DrawGamer

        #region DrawSpawnTimers

        private void DrawSpawntimers(DrawOptions DrawOpts)
        {
            if ((DrawOpts & DrawOptions.SpawnTimers) != DrawOptions.None)
            {
                try
                {
                    MinMaxFilter(out var minZ, out var maxZ);

                    // Draw Spawn Timers

                    Pen pen = new Pen(new SolidBrush(Color.LightGray));

                    foreach (Spawntimer st in eq.MobsTimers.GetRespawned().Values)
                    {
                        if (st.Zone == eq.Shortname)
                        {
                            PointF timerPoint = new PointF((float)Math.Round(CalcScreenCoordX(st.Location.X), 0), (float)Math.Round(CalcScreenCoordY(st.Location.Y), 0));

                            var stOffset = PlusSzOZ - 0.5f;

                            var checkTimer = st.SecondsUntilSpawn(DateTime.Now);

                            var canDraw = false;

                            CheckTimer(ref pen, checkTimer, ref canDraw);

                            // if depth filter on make adjustments to spawn points
                            canDraw = CheckDepthFilter(minZ, maxZ, st, canDraw);
                            if (canDraw)
                            {
                                DrawCross(pen, timerPoint, stOffset);

                                if (Settings.Default.SpawnCountdown && (checkTimer > 0) && (checkTimer < 120))
                                {
                                    DrawSpawnNames(textBrush, checkTimer.ToString(), st.Location.X, st.Location.Y);
                                }
                            }

                            // Draw Blue Line to selected spawn location

                            if ((st.Location.X == eq.SpawnX) && (st.Location.Y == eq.SpawnY))
                            {
                                GetGamerPoint();

                                bkgBuffer.Graphics.DrawLine(BluePen, gamerPos, timerPoint);

                                // Update the Spawn Information Window

                                MobInfoLabel.Text = TimerInfo(st);
                            }
                        }
                    }
                }
                catch (Exception ex) { LogLib.WriteLine("Error in DrawSpawnTimers(): ", ex); }
            }
        }

        private static bool CheckDepthFilter(float minZ, float maxZ, Spawntimer st, bool canDraw)
        {
            if (Settings.Default.DepthFilter && Settings.Default.FilterSpawnPoints)
            {
                if ((st.Location.Z > maxZ) || (st.Location.Z < minZ))
                {
                    canDraw = false;
                    st.Filtered = true;
                }
                else
                {
                    st.Filtered = false;
                }
            }
            else
            {
                st.Filtered = false;
            }

            return canDraw;
        }

        private string GroundItemInfo(GroundItem gi)
        {
            if (f1 == null) { return ""; }

            StringBuilder grounditemInfo = new StringBuilder();

            grounditemInfo.AppendFormat("Ground Item: {0}\n", gi.Desc);

            grounditemInfo.AppendFormat("ActorDef: {0}\n", gi.Name);

            grounditemInfo.AppendFormat(gi.ItemLocation.ToString());

            MobInfoLabel.BackColor = Color.FromArgb(46, 53, 63);
            MobInfoLabel.ForeColor = Color.FromArgb(232, 238, 244);
            lblGameClock.BackColor = ModernTheme.Accent;

            MeasureStrings(grounditemInfo.ToString(), out SizeF sc, out SizeF sf);

            sf.ToPointF();

            sc.ToPointF();

            AdjustTableLayout(24, sf, sc);

            return grounditemInfo.ToString();
        }

        private void CheckTimer(ref Pen pen, int checkTimer, ref bool canDraw)
        {
            if (checkTimer == 0)
                canDraw = true;

            if (checkTimer > 0)
            {
                canDraw = true;
                // Set Pen Colors
                if (checkTimer < 30)

                {
                    if (flash)
                    {
                        pen = RedPen;
                    }
                }
                else if (checkTimer < 60)

                {
                    pen = RedPen;
                }
                else if (checkTimer < 90)

                {
                    pen = new Pen(new SolidBrush(Color.Orange));
                }
                else if (checkTimer < 120)

                {
                    pen = YellowPen;
                }
            }
        }

        private void GetGamerPoint()
        {
            gamerPos.X = CalcScreenCoordX(eq.GamerInfo.X);
            gamerPos.Y = CalcScreenCoordY(eq.GamerInfo.Y);
        }

        #endregion DrawSpawnTimers

        #region DrawGroundItems

        private void DrawGroundItems(DrawOptions DrawOpts, float pZ)
        {
            if ((DrawOpts & DrawOptions.GroundItems) != DrawOptions.None)
            {
                var GroundItemDepthFilter = Settings.Default.DepthFilter && Settings.Default.FilterGroundItems;

                float x, y;

                // Draw Ground Spawns

                foreach (GroundItem gi in eq.GetItemSnapshot())

                {
                    x = (float)Math.Round(CalcScreenCoordX(gi.ItemLocation.X), 0);

                    y = (float)Math.Round(CalcScreenCoordY(gi.ItemLocation.Y), 0);

                    if (!GroundItemDepthFilter || IsWithinDepthFilter(gi.ItemLocation.Z, pZ))
                    {
                        gi.Filtered = false;
                        PointF giPoint = new PointF(x, y);
                        DrawGroundSpawnMarker(giPoint, eq.SpawnX == gi.ItemLocation.X && eq.SpawnY == gi.ItemLocation.Y && eq.SelectedID == 99999);
                    }
                    else
                    {
                        gi.Filtered = true;
                    }

                    // Draw Yellow Line to selected ground item location
                    DrawYellowLine(x, y, gi);

                    if (flash)
                    {
                        FlashAlertGroundSpawns(pZ, GroundItemDepthFilter, x, y, gi);
                    }
                }
            }
        }

        private void DrawYellowLine(float x, float y, GroundItem gi)
        {
            if (eq.SpawnX == gi.ItemLocation.X && eq.SpawnY == gi.ItemLocation.Y && eq.SelectedID == 99999)
            {
                GetGamerPoint();

                DrawLine(SelectionPen, gamerPos.X, gamerPos.Y, x, y);

                DrawEllipse(SelectionPen, x - SelectSizeOffset, y - SelectSizeOffset, SelectSize, SelectSize);

                UpdateSelectionCardGroundItem(gi);
            }
        }

        private void FlashAlertGroundSpawns(float pZ, bool GroundItemDepthFilter, float x, float y, GroundItem gi)
        {
            var x1 = x - PlusSzOZ - 1;
            var y1 = y - PlusSzOZ - 1;
            var width = SpawnPlusSize + 2;
            var height = SpawnPlusSize + 2;

            // Draw alert rings around ground items using the same colors as NPC alert rings.
            if (!GroundItemDepthFilter || IsWithinDepthFilter(gi.ItemLocation.Z, pZ))
            {
                if (gi.IsCaution)
                {
                    DrawEllipse(CautionAlertPen, x1, y1, width, height);
                }
                if (gi.IsDanger)
                {
                    DrawEllipse(DangerAlertPen, x1, y1, width, height);
                }

                if (gi.IsAlert)
                {
                    DrawEllipse(RareAlertPen, x1, y1, width, height);
                }

                if (gi.IsHunt)
                {
                    DrawEllipse(HuntAlertPen, x1, y1, width, height);
                }
            }
        }

        #endregion DrawGroundItems

        #region DrawDirectionLines

        public void DrawDirectionLines(Spawninfo sp, float x, float y)
        {
            // Ensure heading is within bounds
            if (sp.Heading >= 0 && sp.Heading < 512)
            {
                // Calculate line length based on speed and ratio
                float lineLength = sp.SpeedRun * Ratio * 150;

                // Determine the end-point using heading direction
                float y1 = -(xCos[(int)sp.Heading] * lineLength);
                float x1 = -(xSin[(int)sp.Heading] * lineLength);

                // Draw the direction line
                DrawLine(WhitePen, x, y, x + x1, y + y1);
            }
        }

        #endregion DrawDirectionLines

        public void SetUpdateSteps() => SetUpdateSteps(Settings.Default.UpdateDelay);

        public void SetUpdateSteps(int updateDelay)
        {
            // Define constants for minimum values
            const int MinUpdateSteps = 3;
            const int MinUpdateTicks = 1;

            // Prevent division by zero
            if (updateDelay <= 0)
            {
                // Set to a sensible default value to avoid division by zero
                updateDelay = 1;
            }

            // Calculate the number of update steps per second based on UpdateDelay
            // Add 1 to ensure that the calculation doesn't result in too few steps
            int updateSteps = (1000 / updateDelay) + 1;
            UpdateSteps = Math.Max(updateSteps, MinUpdateSteps);

            // Calculate the number of ticks per 250 ms timeframe based on UpdateDelay
            // Ensure there is at least 1 tick (to avoid zero or negative values)
            int updateTicks = 250 / updateDelay;
            UpdateTicks = Math.Max(updateTicks, MinUpdateTicks);
        }

        private void SetSpawnSizes()
        {
            SettingsSpawnSize = Settings.Default.SpawnDrawSize;

            SpawnSize = (SettingsSpawnSize * 2.0f) - 1.0f;

            SpawnSizeOffset = SpawnSize / 2.0f;

            SpawnPlusSize = SpawnSize + 2.0f;

            PlusSzOZ = SpawnPlusSize / 2.0f;

            SelectSize = SpawnSize + 4.0f;

            SelectSizeOffset = SelectSize / 2.0f;
        }

        private string TimerInfo(Spawntimer st)
        {
            // Exit early if the main form reference is not available.
            if (f1 == null || st == null) return string.Empty;

            int heightAdder = 20;

            // Get the spawn timer information as a formatted string.
            string timerInfo = BuildTimerInfoString(st, ref heightAdder);

            // Set the background color of the label.
            MobInfoLabel.BackColor = Color.White;

            // Measure the size of the string for layout purposes.
            MeasureStrings(timerInfo, out SizeF stringContentSize, out SizeF labelSize);

            // Adjust the layout based on measured sizes.
            AdjustTableLayout(heightAdder, stringContentSize, labelSize);

            return timerInfo;
        }

        private static string BuildTimerInfoString(Spawntimer st, ref int heightAdder)
        {
            var stringBuilder = new StringBuilder();

            // Add spawn name information.
            stringBuilder.AppendLine($"Spawn Name: {st.LastSpawnName}");

            // Process and append all names encountered.
            AppendEncounteredNames(st, stringBuilder, ref heightAdder);

            // Append other relevant information from the spawn timer description.
            stringBuilder.AppendLine(st.GetDescription());

            return stringBuilder.ToString();
        }

        private static void AppendEncounteredNames(Spawntimer st, StringBuilder stringBuilder, ref int heightAdder)
        {
            var namesToAdd = new StringBuilder("Names encountered: ");
            var names = st.AllNames.Split(',');

            const int MaxLineLength = 45; // Define a maximum line length for wrapping.
            var lineLength = namesToAdd.Length;

            foreach (var name in names.Select(n => n.TrimName()))
            {
                var nameLength = name.Length;

                // Check if adding the next name exceeds the maximum line length.
                if (lineLength + nameLength + 2 >= MaxLineLength)
                {
                    stringBuilder.AppendLine(namesToAdd.ToString());
                    heightAdder += 2; // Adjust height for the new line.

                    namesToAdd.Clear();
                    lineLength = 0;
                }
                else if (lineLength > 0) // Add a comma separator if it's not the first name.
                {
                    namesToAdd.Append(", ");
                    lineLength += 2;
                }

                namesToAdd.Append(name);
                lineLength += nameLength;
            }

            // Append any remaining names that haven't been added.
            if (namesToAdd.Length > 0)
            {
                stringBuilder.AppendLine(namesToAdd.ToString());
            }
        }

        private void AdjustTableLayout(int heightAdder, SizeF stringContentSize, SizeF labelSize)
        {
            tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Absolute;

            if (Settings.Default.ShowTargetInfo)
            {
                // Determine the necessary width based on content sizes.
                int maxWidth = Math.Max(220, (int)Math.Ceiling(Math.Max(stringContentSize.Width, labelSize.Width)) + 28);
                int infoHeight = (int)Math.Ceiling(stringContentSize.Height) + heightAdder;
                int clockHeight = Math.Max(24, (int)Math.Ceiling(labelSize.Height) + 8);

                tableLayoutPanel1.Width = maxWidth;
                tableLayoutPanel1.ColumnStyles[0].Width = maxWidth;
                MobInfoLabel.Width = maxWidth;

                tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Absolute;
                tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Absolute;

                tableLayoutPanel1.RowStyles[0].Height = clockHeight;
                tableLayoutPanel1.RowStyles[1].Height = infoHeight;
                MobInfoLabel.Height = infoHeight;
                tableLayoutPanel1.Height = clockHeight + infoHeight + 2;
            }
            else
            {
                HideTableRows();
            }
        }

        private void HideTableRows()
        {
            // Hide rows by setting their heights to zero.
            tableLayoutPanel1.RowStyles[0].Height = 0;
            tableLayoutPanel1.RowStyles[1].Height = 0;
        }
    }
}
