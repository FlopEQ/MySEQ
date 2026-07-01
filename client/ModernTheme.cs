using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace myseq
{
    internal static class ModernTheme
    {
        public static readonly Font UiFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Color Shell = Color.FromArgb(32, 36, 43);
        public static readonly Color ShellAlt = Color.FromArgb(43, 49, 58);
        public static readonly Color Surface = Color.FromArgb(246, 248, 250);
        public static readonly Color SurfaceAlt = Color.FromArgb(232, 237, 242);
        public static readonly Color Border = Color.FromArgb(198, 207, 216);
        public static readonly Color Text = Color.FromArgb(30, 35, 41);
        public static readonly Color MutedText = Color.FromArgb(92, 103, 115);
        public static readonly Color Accent = Color.FromArgb(31, 119, 139);
        public static readonly Color AccentWarm = Color.FromArgb(202, 137, 44);
        public static readonly Color ListOddRow = Color.FromArgb(250, 252, 253);
        public static readonly Color ListEvenRow = Color.White;
        public static readonly Color ListSelected = Color.FromArgb(213, 232, 239);
        public static readonly Color ListHeader = Color.FromArgb(52, 61, 72);

        public static void ApplyApplicationDefaults()
        {
            Application.SetDefaultFont(UiFont);
            ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable());
        }

        public static void ApplyMainForm(Form form)
        {
            form.Font = UiFont;
            form.BackColor = SurfaceAlt;
            form.ForeColor = Text;
            StyleControls(form.Controls);
        }

        public static void ApplyListPanel(Form panel)
        {
            panel.Font = UiFont;
            panel.BackColor = Surface;
            panel.ForeColor = Text;
            StyleControls(panel.Controls);
        }

        public static void ApplyMapSurface(Control control)
        {
            control.Font = UiFont;
            control.BackColor = Color.FromArgb(18, 21, 24);
            StyleControls(control.Controls);
        }

        private static void StyleControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case MenuStrip menu:
                        StyleToolStrip(menu, Shell, Color.White);
                        break;

                    case StatusStrip status:
                        StyleToolStrip(status, Shell, Color.White);
                        status.SizingGrip = false;
                        break;

                    case ContextMenuStrip contextMenu:
                        StyleToolStrip(contextMenu, Surface, Text);
                        break;

                    case ToolStrip toolStrip:
                        StyleToolStrip(toolStrip, Surface, Text);
                        toolStrip.Padding = new Padding(6, 3, 6, 3);
                        break;

                    case ListView listView:
                        StyleListView(listView);
                        break;

                    case TextBox textBox:
                        StyleTextBox(textBox);
                        break;

                    case ComboBox comboBox:
                        comboBox.Font = UiFont;
                        comboBox.BackColor = Color.White;
                        comboBox.ForeColor = Text;
                        break;

                    case Button button:
                        StyleButton(button);
                        break;

                    case Label label:
                        label.Font = UiFont;
                        if (label.BackColor == SystemColors.Control || label.BackColor == Color.White)
                        {
                            label.BackColor = Surface;
                            label.ForeColor = Text;
                        }
                        break;

                    case TabControl tabControl:
                        tabControl.Font = UiFont;
                        tabControl.BackColor = Surface;
                        tabControl.Padding = new Point(14, 6);
                        break;

                    case TabPage tabPage:
                        tabPage.Font = UiFont;
                        tabPage.BackColor = Surface;
                        tabPage.ForeColor = Text;
                        break;

                    case GroupBox groupBox:
                        groupBox.Font = new Font(UiFont, FontStyle.Bold);
                        groupBox.BackColor = Surface;
                        groupBox.ForeColor = Text;
                        break;
                }

                if (control.HasChildren)
                {
                    StyleControls(control.Controls);
                }
            }
        }

        private static void StyleToolStrip(ToolStrip strip, Color backColor, Color foreColor)
        {
            strip.Font = UiFont;
            strip.BackColor = backColor;
            strip.ForeColor = foreColor;
            strip.GripStyle = ToolStripGripStyle.Hidden;
            strip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            strip.ImageScalingSize = new Size(20, 20);

            foreach (ToolStripItem item in strip.Items)
            {
                StyleToolStripItem(item, foreColor);
            }
        }

        private static void StyleToolStripItem(ToolStripItem item, Color foreColor)
        {
            item.Font = UiFont;
            item.ForeColor = foreColor;
            item.Margin = item is ToolStripSeparator ? Padding.Empty : new Padding(1, 2, 1, 2);

            if (item is ToolStripControlHost host)
            {
                host.Control.Font = UiFont;
                host.Control.BackColor = Color.White;
                host.Control.ForeColor = Text;
            }

            if (item is ToolStripDropDownItem dropDownItem)
            {
                dropDownItem.DropDown.BackColor = Surface;
                dropDownItem.DropDown.ForeColor = Text;

                foreach (ToolStripItem child in dropDownItem.DropDownItems)
                {
                    StyleToolStripItem(child, Text);
                }
            }
        }

        private static void StyleListView(ListView listView)
        {
            listView.Font = UiFont;
            listView.BorderStyle = BorderStyle.None;
            listView.BackColor = Color.White;
            listView.ForeColor = Text;
            listView.GridLines = false;
            listView.FullRowSelect = true;
            listView.HideSelection = false;
            listView.Margin = new Padding(0);
        }

        private static void StyleTextBox(TextBox textBox)
        {
            textBox.Font = UiFont;
            textBox.BackColor = Color.White;
            textBox.ForeColor = Text;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleButton(Button button)
        {
            button.Font = UiFont;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = SurfaceAlt;
            button.ForeColor = Text;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(221, 231, 236);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(207, 221, 229);
        }

        public static void StyleRailButton(Button button, bool active)
        {
            button.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.Width = 42;
            button.Height = 34;
            button.Margin = new Padding(0, 0, 0, 6);
            button.Padding = Padding.Empty;
            button.TabStop = false;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = false;
            button.BackColor = active ? Accent : Color.FromArgb(42, 48, 56);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = active ? AccentWarm : Color.FromArgb(72, 82, 94);
            button.FlatAppearance.MouseOverBackColor = active ? Color.FromArgb(37, 139, 163) : Color.FromArgb(58, 66, 76);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 100, 121);
        }

        public static Image CreateToolbarIcon(ModernIcon icon, bool active = false)
        {
            const int size = 22;
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            using (var pen = new Pen(active ? AccentWarm : Accent, 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            })
            using (var brush = new SolidBrush(active ? AccentWarm : Accent))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                switch (icon)
                {
                    case ModernIcon.Connect:
                        g.FillPolygon(brush, new[] { new PointF(8, 5), new PointF(17, 11), new PointF(8, 17) });
                        break;
                    case ModernIcon.Disconnect:
                    case ModernIcon.Clear:
                        g.DrawLine(pen, 7, 7, 15, 15);
                        g.DrawLine(pen, 15, 7, 7, 15);
                        break;
                    case ModernIcon.ZoomIn:
                        DrawMagnifier(g, pen);
                        g.DrawLine(pen, 8, 11, 14, 11);
                        g.DrawLine(pen, 11, 8, 11, 14);
                        break;
                    case ModernIcon.ZoomOut:
                        DrawMagnifier(g, pen);
                        g.DrawLine(pen, 8, 11, 14, 11);
                        break;
                    case ModernIcon.Depth:
                        g.DrawRectangle(pen, 5, 5, 12, 12);
                        g.DrawLine(pen, 7, 9, 15, 9);
                        g.DrawLine(pen, 7, 13, 15, 13);
                        break;
                    case ModernIcon.Up:
                        g.DrawLines(pen, new[] { new PointF(6, 13), new PointF(11, 8), new PointF(16, 13) });
                        break;
                    case ModernIcon.Down:
                        g.DrawLines(pen, new[] { new PointF(6, 9), new PointF(11, 14), new PointF(16, 9) });
                        break;
                    case ModernIcon.Reset:
                        g.DrawArc(pen, 5, 5, 12, 12, 35, 285);
                        g.FillPolygon(brush, new[] { new PointF(15, 4), new PointF(18, 8), new PointF(13, 8) });
                        break;
                    case ModernIcon.Options:
                        g.DrawEllipse(pen, 7, 7, 8, 8);
                        for (int i = 0; i < 8; i++)
                        {
                            double angle = i * Math.PI / 4;
                            float x1 = 11 + (float)Math.Cos(angle) * 6;
                            float y1 = 11 + (float)Math.Sin(angle) * 6;
                            float x2 = 11 + (float)Math.Cos(angle) * 8;
                            float y2 = 11 + (float)Math.Sin(angle) * 8;
                            g.DrawLine(pen, x1, y1, x2, y2);
                        }
                        break;
                    case ModernIcon.SearchMode:
                        g.DrawEllipse(pen, 5, 5, 9, 9);
                        g.DrawLine(pen, 13, 13, 17, 17);
                        break;
                }
            }

            return bitmap;
        }

        private static void DrawMagnifier(Graphics g, Pen pen)
        {
            g.DrawEllipse(pen, 5, 5, 11, 11);
            g.DrawLine(pen, 14, 14, 18, 18);
        }

        private sealed class ModernColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => Surface;
            public override Color ToolStripGradientMiddle => Surface;
            public override Color ToolStripGradientEnd => Surface;
            public override Color MenuStripGradientBegin => Shell;
            public override Color MenuStripGradientEnd => Shell;
            public override Color StatusStripGradientBegin => Shell;
            public override Color StatusStripGradientEnd => Shell;
            public override Color ToolStripBorder => Border;
            public override Color MenuBorder => Border;
            public override Color MenuItemBorder => Accent;
            public override Color MenuItemSelected => Color.FromArgb(218, 235, 240);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(218, 235, 240);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(218, 235, 240);
            public override Color MenuItemPressedGradientBegin => ShellAlt;
            public override Color MenuItemPressedGradientMiddle => ShellAlt;
            public override Color MenuItemPressedGradientEnd => ShellAlt;
            public override Color ImageMarginGradientBegin => SurfaceAlt;
            public override Color ImageMarginGradientMiddle => SurfaceAlt;
            public override Color ImageMarginGradientEnd => SurfaceAlt;
            public override Color ButtonSelectedHighlight => Color.FromArgb(218, 235, 240);
            public override Color ButtonSelectedGradientBegin => Color.FromArgb(218, 235, 240);
            public override Color ButtonSelectedGradientEnd => Color.FromArgb(218, 235, 240);
            public override Color ButtonPressedGradientBegin => Color.FromArgb(197, 222, 229);
            public override Color ButtonPressedGradientEnd => Color.FromArgb(197, 222, 229);
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Color.White;
        }
    }

    internal enum ModernIcon
    {
        Connect,
        Disconnect,
        ZoomIn,
        ZoomOut,
        Depth,
        Up,
        Down,
        Reset,
        Options,
        SearchMode,
        Clear
    }
}
