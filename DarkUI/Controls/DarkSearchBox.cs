using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A text box with a search icon on the left and a clear ("✕") button on the right.
    /// </summary>
    public class DarkSearchBox : UserControl
    {
        private readonly TextBox _textBox = new TextBox();
        private readonly Label _iconLabel = new Label();
        private readonly Label _clearLabel = new Label();
        private string _placeholder = "Search…";

        public event EventHandler SearchTextChanged;

        [Category("Appearance")]
        [DefaultValue("Search…")]
        public string Placeholder
        {
            get { return _placeholder; }
            set { _placeholder = value ?? ""; UpdatePlaceholder(); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SearchText
        {
            get { return _textBox.Text; }
            set { _textBox.Text = value; }
        }

        public DarkSearchBox()
        {
            BackColor = Colors.GreyBackground;
            Height = 26;

            _iconLabel.Text = "🔍";
            _iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            _iconLabel.ForeColor = Colors.DisabledText;
            _iconLabel.Cursor = Cursors.Default;

            _clearLabel.Text = "✕";
            _clearLabel.TextAlign = ContentAlignment.MiddleCenter;
            _clearLabel.ForeColor = Colors.DisabledText;
            _clearLabel.Cursor = Cursors.Hand;
            _clearLabel.Visible = false;
            _clearLabel.Click += (s, e) => { _textBox.Text = ""; _textBox.Focus(); };

            _textBox.BorderStyle = BorderStyle.None;
            _textBox.BackColor = Colors.GreyBackground;
            _textBox.ForeColor = Colors.LightText;
            _textBox.Font = new Font("Segoe UI", 9.5F);

            _textBox.TextChanged += (s, e) =>
            {
                _clearLabel.Visible = _textBox.Text.Length > 0;
                UpdatePlaceholder();
                SearchTextChanged?.Invoke(this, EventArgs.Empty);
            };
            _textBox.Enter += (s, e) => Invalidate();
            _textBox.Leave += (s, e) => Invalidate();

            Controls.Add(_textBox);
            Controls.Add(_iconLabel);
            Controls.Add(_clearLabel);
        }

        private void UpdatePlaceholder()
        {
            _textBox.Invalidate(); // placeholder handled in paint override below
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            const int iconW = 24, clearW = 22, pad = 4;
            _iconLabel.Bounds = new Rectangle(0, 0, iconW, Height);
            _clearLabel.Bounds = new Rectangle(Width - clearW - pad, 0, clearW, Height);
            _textBox.Bounds = new Rectangle(iconW, (Height - 20) / 2, Width - iconW - clearW - pad, 20);
        }

        // Placeholder painting for the borderless textbox
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Focused ? Colors.BlueHighlight : Colors.LightBorder))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _textBox.Focus();
        }
    }
}
