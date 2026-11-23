using System;
using System.Drawing;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Fenêtre des thèmes : liste courte (ascenseur et molette), le thème s'applique dès le clic, la fenêtre reste ouverte jusqu'à un clic ailleurs.
    class ThemePicker : Form
    {
        readonly ListBox list; readonly Button custom; bool init = true; readonly bool forAdd; int Sel { get { return forAdd && Theme.AddIndex >= 0 ? Theme.AddIndex : Theme.Index; } }
        public ThemePicker(Action openColors, bool forAdd = false)
        {
            this.forAdd = forAdd; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; Font = Ui.Normal; KeyPreview = true;
            list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 22 };
            foreach (ThemeDef t in Theme.All) list.Items.Add(t.Name);
            custom = new Button { Dock = DockStyle.Bottom, Height = 30, Text = Tr.T("Personnaliser les couleurs du thème actuel..."), FlatStyle = FlatStyle.Flat, TabStop = false };
            custom.Click += delegate { Close(); openColors(); };
            Controls.Add(list); Controls.Add(custom);
            int wmax = 0; foreach (ThemeDef t in Theme.All) wmax = Math.Max(wmax, TextRenderer.MeasureText("✔ " + t.Name, Font).Width); ClientSize = new Size(wmax + SystemInformation.VerticalScrollBarWidth + 24, 24 * 22 + (forAdd ? 0 : 30)); Padding = new Padding(1);
            list.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return; bool sel = e.Index == Sel; Color bg = sel ? Theme.Get("lb.accent") : Theme.Get("lb.grid.back"), fg = sel ? Theme.Get("lb.accent.text") : Theme.Get("lb.grid.text");
                if ((e.State & DrawItemState.Focus) != 0 && !sel) bg = Theme.Get("lb.grid.alt");
                using (SolidBrush b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, e.Bounds);
                TextRenderer.DrawText(e.Graphics, (sel ? "✔ " : "   ") + list.Items[e.Index], Font, e.Bounds, fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            };
            list.SelectedIndexChanged += delegate { if (!init && list.SelectedIndex >= 0 && list.SelectedIndex != Sel) { if (forAdd) Theme.SetAdd(list.SelectedIndex); else Theme.Set(list.SelectedIndex); } };
            Action recolor = delegate { BackColor = Theme.Get("lb.line"); custom.BackColor = Theme.Get("lb.btn.back"); custom.ForeColor = Theme.Get("lb.btn.text"); list.BackColor = Theme.Get("lb.grid.back"); list.Invalidate(); };
            Theme.Changed += recolor; Disposed += delegate { Theme.Changed -= recolor; }; recolor();
            list.SelectedIndex = Sel; init = false; if (forAdd) custom.Visible = false;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            Deactivate += delegate { if (!IsDisposed) Close(); };
            Shown += delegate { list.Focus(); };
        }
        protected override bool ShowWithoutActivation { get { return false; } }
    }
}
