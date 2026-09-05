using System;
using System.Drawing;
using System.Windows.Forms;
using DarkUI.Config;
using DarkUI.Experiments.WindowlessRichEdit;

namespace WindowlessRichEditTest
{
    public class TestForm : Form
    {
        private readonly DarkWindowlessRichEditPrototype _proto;
        private readonly TextBox _normalBox;

        public TestForm()
        {
            Text = "Windowless RichEdit — Selection Theme POC";
            Size = new Size(720, 480);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(60, 63, 65);

            var protoLabel = new Label
            {
                Text = "WINDOWLESS RichEdit (selection must follow theme):",
                ForeColor = Color.White,
                Location = new Point(20, 16),
                AutoSize = true
            };

            _proto = new DarkWindowlessRichEditPrototype
            {
                Location = new Point(20, 40),
                Size = new Size(660, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var normalLabel = new Label
            {
                Text = "NORMAL WinForms TextBox (selection must stay Windows blue):",
                ForeColor = Color.White,
                Location = new Point(20, 140),
                AutoSize = true
            };

            _normalBox = new TextBox
            {
                Location = new Point(20, 164),
                Size = new Size(660, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Select text here too — this must NOT change color."
            };

            var btnLabel = new Label
            {
                Text = "Themes (selection colors):",
                ForeColor = Color.White,
                Location = new Point(20, 210),
                AutoSize = true
            };

            var btnCharcoal = MakeButton("Charcoal #4B6EAF", new Point(20, 236), () => Apply("Charcoal", 0x4B, 0x6E, 0xAF, 60, 63, 65));
            var btnEmerald = MakeButton("Emerald #4E7656", new Point(150, 236), () => Apply("Emerald", 0x4E, 0x76, 0x56, 46, 58, 51));
            var btnCrimson = MakeButton("Crimson #8A4B55", new Point(280, 236), () => Apply("Crimson", 0x8A, 0x4B, 0x55, 58, 46, 46));
            var btnViolet = MakeButton("Violet #68548D", new Point(410, 236), () => Apply("Violet", 0x68, 0x54, 0x8D, 51, 46, 58));

            var hint = new Label
            {
                Text = "1) Click the windowless box, drag-select text. 2) Click a theme button while selected. 3) Selection must recolor instantly.",
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(20, 290),
                AutoSize = true
            };

            Controls.AddRange(new Control[] { protoLabel, _proto, normalLabel, _normalBox, btnLabel,
                                              btnCharcoal, btnEmerald, btnCrimson, btnViolet, hint });
        }

        private Button MakeButton(string text, Point loc, Action onClick)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            b.Click += (s, e) => onClick();
            return b;
        }

        private void Apply(string name, int sr, int sg, int sb, int br, int bg, int bb)
        {
            var t = new Theme(name)
            {
                GreyBackground = Color.FromArgb(br, bg, bb),
                HeaderBackground = Color.FromArgb(br - 4, bg - 4, bb - 4),
                MediumBackground = Color.FromArgb(br + 10, bg + 10, bb + 10),
                LightBackground = Color.FromArgb(br + 22, bg + 22, bb + 22),
                LighterBackground = Color.FromArgb(br + 34, bg + 34, bb + 34),
                LightestBackground = Color.FromArgb(160, 160, 165),
                DarkBackground = Color.FromArgb(br - 12, bg - 12, bb - 12),
                BlueBackground = Color.FromArgb(br + 8, bg + 12, bb + 16),
                DarkBlueBackground = Color.FromArgb(br - 6, bg - 4, bb),
                LightBorder = Color.FromArgb(br + 16, bg + 16, bb + 16),
                DarkBorder = Color.FromArgb(br - 10, bg - 10, bb - 10),
                DarkBlueBorder = Color.FromArgb(br - 8, bg - 8, bb - 6),
                LightBlueBorder = Color.FromArgb(br + 20, bg + 20, bb + 22),
                LightText = Color.FromArgb(235, 235, 240),
                DisabledText = Color.FromArgb(140, 140, 148),
                BlueHighlight = Color.FromArgb(sr, sg, sb),
                BlueSelection = Color.FromArgb(sr, sg, sb),
                GreyHighlight = Color.FromArgb(br + 30, bg + 30, bb + 30),
                GreySelection = Color.FromArgb(br + 14, bg + 14, bb + 14),
                DarkGreySelection = Color.FromArgb(br + 6, bg + 6, bb + 6),
                ActiveControl = Color.FromArgb(sr + 30, sg + 30, sb + 30),
                MenuItemToggledOnFill = Color.FromArgb(br + 10, bg + 10, bb + 10),
                MenuItemToggledOnBorder = Color.FromArgb(sr, sg, sb),
            };
            ThemeManager.Apply(t);
        }
    }
}
