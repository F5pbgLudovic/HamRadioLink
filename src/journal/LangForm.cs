using System;
using System.Drawing;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Configuration > Langue : langue de l'interface du journal (français par défaut). Appliquée à la réouverture du journal.
    class LangForm : Form
    {
        public LangForm()
        {
            Text = "Langue / Language"; ClientSize = new Size(420, 150); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            Ui.Lbl(this, "Langue du journal / Log language :", 12, 16);
            ComboBox c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = 12, Top = 42, Width = 250, MaxDropDownItems = 16 }; Controls.Add(c);
            foreach (string n in Tr.Names) c.Items.Add(n); c.SelectedIndex = Math.Max(0, Array.IndexOf(Tr.Codes, Tr.Code));
            Label note = Ui.Lbl(this, "Appliquée à la réouverture du journal (fermez-le puis rouvrez-le). / Applied when the log is reopened.", 12, 76, 396); note.Height = 36; note.ForeColor = SystemColors.GrayText;
            Ui.Btn(this, "OK", 232, 114, 80, 28, (s, e) => { string old = Tr.Code; Tr.Code = Tr.Codes[c.SelectedIndex]; DialogResult = DialogResult.OK; Close(); });
            Ui.Btn(this, "Cancel", 322, 114, 80, 28, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
        }
    }
}
