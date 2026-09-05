using System;
using System.Drawing;
using System.Windows.Forms;

namespace RtbSelectionTest
{
    /// <summary>
    /// Demonstrates that RichTextBox.SelectionColor / SelectionBackColor are
    /// PERSISTENT CHARACTER FORMATTING, not the transient selection highlight.
    ///
    /// Test steps:
    ///  1. Drag-select some text -> watch the native blue selection bar.
    ///  2. Click "Set BackColor (SelectionBackColor)" -> the selected characters
    ///     get a permanent background baked into the RTF (like Word highlighter).
    ///  3. Click elsewhere to deselect -> the background REMAINS. That proves it
    ///     is document formatting, not a selection visual.
    ///  4. Click "Show RTF" -> the \highlight or \cb background control words are
    ///     visible in the document text.
    ///  5. Apply SelectionColor the same way -> text color baked in.
    ///  6. For contrast: the same operation does NOT exist on the plain TextBox.
    /// </summary>
    public class TestForm : Form
    {
        private readonly RichTextBox _rtb;
        private readonly TextBox _normal;
        private readonly RichTextBox _rtfView;

        public TestForm()
        {
            Text = "RichTextBox SelectionColor — what it really is";
            Size = new Size(980, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(40, 42, 46);

            var label = new Label
            {
                Text = "1) Select text with the mouse  2) click a button  3) click outside to deselect — the color STAYS.",
                ForeColor = Color.White,
                Location = new Point(16, 12),
                AutoSize = true
            };

            _rtb = new RichTextBox
            {
                Location = new Point(16, 40),
                Size = new Size(640, 160),
                Text = "Plain text. Red text. Blue text. Bold text. Yellow-background text.\n" +
                       "Select any part of this and apply colors — then deselect.\n" +
                       "Observe: the colors are baked into the document (like Word's font color / highlighter).\n" +
                       "The blue selection bar Windows draws is a SEPARATE thing and is not changed by these properties."
            };
            _rtb.Select(12, 9); _rtb.SelectionColor = Color.Red;
            _rtb.Select(22, 10); _rtb.SelectionColor = Color.FromArgb(80, 140, 255);
            _rtb.Select(33, 10); _rtb.SelectionFont = new Font(_rtb.Font, FontStyle.Bold);
            _rtb.Select(44, 22); _rtb.SelectionBackColor = Color.FromArgb(200, 190, 60);
            _rtb.DeselectAll();

            var normalLabel = new Label
            {
                Text = "Plain TextBox (for contrast — it has NO such property):",
                ForeColor = Color.White,
                Location = new Point(16, 216),
                AutoSize = true
            };
            _normal = new TextBox
            {
                Location = new Point(16, 240),
                Size = new Size(640, 23),
                Text = "Select text here and try to color it — impossible without document formatting."
            };

            var btnBack = MakeButton("Set BackColor on selection", new Point(16, 280));
            btnBack.Click += (s, e) =>
            {
                if (_rtb.SelectionLength == 0) { MessageBox.Show("Select some text first."); return; }
                _rtb.SelectionBackColor = Color.FromArgb(75, 110, 175); // BlueSelection-like
                _rtb.SelectionColor = Color.White;
            };

            var btnColor = MakeButton("Set ForeColor on selection", new Point(236, 280));
            btnColor.Click += (s, e) =>
            {
                if (_rtb.SelectionLength == 0) { MessageBox.Show("Select some text first."); return; }
                _rtb.SelectionColor = Color.FromArgb(220, 80, 80);
            };

            var btnRtf = MakeButton("Show RTF", new Point(456, 280));
            btnRtf.Click += (s, e) => _rtfView.Rtf = _rtb.Rtf;

            var rtfLabel = new Label
            {
                Text = "RTF below — look for \\highlight (back color) and \\cfN (fore color) control words after applying:",
                ForeColor = Color.White,
                Location = new Point(16, 320),
                AutoSize = true
            };

            _rtfView = new RichTextBox
            {
                Location = new Point(16, 344),
                Size = new Size(640, 320),
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 9f)
            };

            var note = new Label
            {
                Text = "Conclusion: SelectionColor / SelectionBackColor = persistent character formatting\n" +
                       "(font color + highlighter). They do NOT change the transient selection bar,\n" +
                       "which is drawn by the OS with system colors. That is why DarkUI keeps native selection.",
                ForeColor = Color.FromArgb(180, 200, 230),
                Location = new Point(16, 672),
                AutoSize = true
            };

            Controls.AddRange(new Control[] { label, _rtb, normalLabel, _normal, btnBack, btnColor, btnRtf, rtfLabel, _rtfView, note });
        }

        private Button MakeButton(string text, Point loc)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(210, 30),
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            return b;
        }
    }
}
