using DarkUI.Config;
using DarkUI.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    public class DarkComboBox : ComboBox
    {
        #region Static
        public static Bitmap DefaultButtonIcon { get { return Icons.ComboBoxIcons.combobox_arrow; } }
        #endregion

        #region Fields
        // Visual look
        private Color _borderColor = Colors.GreySelection;
        private ButtonBorderStyle _borderStyle = ButtonBorderStyle.Solid;
        private Color _buttonColor = Colors.LightBackground;
        private Bitmap _buttonIcon = DefaultButtonIcon;

        // Text
        private Padding _textPadding = new Padding(2);
        #endregion Fields

        #region Constructor
        public DarkComboBox()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            DrawMode = DrawMode.OwnerDrawVariable;

            FlatStyle = FlatStyle.Flat;
            DropDownStyle = ComboBoxStyle.DropDownList;

            BackColor = Colors.LightBackground;
            ForeColor = Colors.LightText;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode) ApplyThemeColors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => ApplyThemeColors();

        private void ApplyThemeColors()
        {
            BackColor = Colors.LightBackground;
            ForeColor = Colors.LightText;
            _borderColor = Colors.GreySelection;
            _buttonColor = Colors.LightBackground;
            Invalidate(true);
        }

        #endregion Constructor

        #region Properties
        [DefaultValue(DrawMode.OwnerDrawVariable)]
        [RefreshProperties(RefreshProperties.Repaint)]
        public new DrawMode DrawMode
        {
            get { return base.DrawMode; }
            set { base.DrawMode = value; }
        }

        [DefaultValue(FlatStyle.Flat)]
        public new FlatStyle FlatStyle
        {
            get { return base.FlatStyle; }
            set { base.FlatStyle = value; }
        }

        [DefaultValue(ComboBoxStyle.DropDownList)]
        public new ComboBoxStyle DropDownStyle
        {
            get { return base.DropDownStyle; }
            set { base.DropDownStyle = value; }
        }

        // ── Designer freeze guard ─────────────────────────────────────
        // Theme-owned colors must not be serialized by the designer — a
        // frozen value in Designer.cs would stop the control from
        // following ThemeManager.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public sealed override Color ForeColor
        {
            get => Colors.LightText;
            set
            {
                base.ForeColor = Colors.LightText;
                Invalidate();
            }
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public sealed override Color BackColor
        {
            get => Colors.LightBackground;
            set
            {
                base.BackColor = Colors.LightBackground;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ButtonColor
        {
            get { return _buttonColor; }
            set
            {
                _buttonColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Bitmap ButtonIcon
        {
            get { return _buttonIcon; }
            set
            {
                _buttonIcon = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(ButtonBorderStyle.Solid)]
        public ButtonBorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set
            {
                _borderStyle = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool DrawDropdownHoverOutline { get; set; }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool DrawFocusRectangle { get; set; }

        [Category("Appearance")]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Padding TextPadding
        {
            get { return _textPadding; }
            set
            {
                _textPadding = value;
                Invalidate();
            }
        }

        #endregion Properties

        #region Methods
        public new void Invalidate()
        {
            base.Invalidate();
        }
        #endregion Methods

        #region On Events

        #region Data Events
        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();

            base.OnTextChanged(e);
        }

        protected override void OnTextUpdate(EventArgs e)
        {
            Invalidate();

            base.OnTextUpdate(e);
        }

        protected override void OnSelectedValueChanged(EventArgs e)
        {
            Invalidate();

            base.OnSelectedValueChanged(e);
        }
        #endregion

        #region Drawing Events
        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            // The open dropdown list is a native popup that paints a
            // system-blue focus border around itself — repaint it with the
            // theme border. Re-applied per open (the list window is
            // recreated for each dropdown session).
            NativeFocusBorder.ApplyToComboList(Handle);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (Items.Count <= e.Index || e.Index <= -1) return;

            bool sel = (e.State & DrawItemState.Selected) != 0;
            Color bg = sel ? Colors.BlueSelection : BackColor;
            Color fg = !Enabled ? Colors.DisabledText : (sel ? Colors.SelectionText : ForeColor);

            using (var b = new SolidBrush(bg))
                e.Graphics.FillRectangle(b, e.Bounds);

            var formatE = new ListControlConvertEventArgs(null, typeof(string), Items[e.Index]);
            OnFormat(formatE);
            string text = formatE.Value?.ToString() ?? Items[e.Index].ToString();
            TextRenderer.DrawText(e.Graphics, text, Font, e.Bounds, fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle buttonRect = new Rectangle(ClientRectangle.Width - SystemInformation.VerticalScrollBarWidth, 0, SystemInformation.VerticalScrollBarWidth, ClientRectangle.Height);
            Rectangle buttonIconRect = new Rectangle(buttonRect.Left + (buttonRect.Width - _buttonIcon.Width) / 2, buttonRect.Top + (buttonRect.Height / 2 - _buttonIcon.Height / 2), _buttonIcon.Width, _buttonIcon.Height);
            Rectangle textRect = new Rectangle(1 + _textPadding.Left, 1 + _textPadding.Top, ClientRectangle.Width - (2 + buttonRect.Width + _textPadding.Horizontal), ClientRectangle.Height - (2 + _textPadding.Vertical));

            // Draw background
            using (var buttonBrush = new SolidBrush(_buttonColor))
                e.Graphics.FillRectangle(buttonBrush, buttonRect);
            e.Graphics.DrawImage(_buttonIcon, buttonIconRect);
            ControlPaint.DrawBorder(e.Graphics, buttonRect, _borderColor, ButtonBorderStyle.Solid);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, _borderColor, ButtonBorderStyle.Solid);

            // Draw text
            string text = Text;
            if (SelectedItem != null)
            {
                var formatE = new ListControlConvertEventArgs(null, typeof(string), SelectedItem);
                OnFormat(formatE);
                text = formatE.Value?.ToString() ?? SelectedItem.ToString();
            }
            using (var backBrush = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(backBrush, textRect);
            using (var foreBrush = new SolidBrush(Enabled ? ForeColor : Colors.DisabledText))
                e.Graphics.DrawString(text ?? Text, Font, foreBrush, textRect, StringFormat.GenericDefault);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }
        #endregion Drawing

        #endregion On Events
    }
}
