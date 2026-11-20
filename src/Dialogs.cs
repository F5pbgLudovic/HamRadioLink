using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Éditeur de la commande CAT d'un élément (clic droit en « vue config »)
    public class CmdEditor : Form
    {
        readonly Ctl c; readonly Rig rig;
        TextBox tbLabel, tbSet, tbRead, tbParse, tbItems, tbOn, tbOff, tbMin, tbMax, tbSample; Label lbPrev, lbRes, lbTest;
        public bool Changed, Deleted;

        public CmdEditor(Ctl ctl, Rig r) : this(ctl, r, null) { }
        public CmdEditor(Ctl ctl, Rig r, RadioDef radio)
        {
            c = ctl; rig = r; def = radio ?? (r != null ? r.Def : null); Text = "Commande CAT — " + c.Label; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = T.Panel; ForeColor = T.Text; Font = new Font("Segoe UI", 9f); ClientSize = new Size(660, 560);
            int y = 12;
            Label h = L("Élément : " + c.Label + "   (identifiant interne : " + c.Id + ")", 12, y, 636, 20); h.ForeColor = T.Amber; h.Font = new Font("Segoe UI Semibold", 9.5f); y += 26;
            if (!c.Local && def != null) NameCombo(ref y); else tbLabel = Box("Nom affiché", c.Label, ref y, false);
            if (c.Local && c.K != Kind.Choice)
            {
                L(c.K == Kind.Frame || c.K == Kind.Text ? "Cadre décoratif : pas de commande CAT." : "Voyant ou réglage interne : aucune commande CAT n'est envoyée par cet élément (il reprend l'état d'un autre élément : « " + c.Ref + " »).", 12, y, 636, 40); y += 44;
            }
            else if (c.Local) { L("Réglage interne du programme (pas de commande CAT). Les valeurs du choix peuvent être modifiées :", 12, y, 636, 20); y += 24; }
            if (!c.Local || c.K == Kind.Choice)
            {
                if (!c.Local) { tbSet = Box(c.K == Kind.Action ? "Commande à envoyer" : "Commande d'envoi (SET)", c.Set, ref y, true); tbSet.TextChanged += delegate { Prev(); }; }
                if (!c.Local && c.K != Kind.Action) tbRead = Box("Commande de lecture (READ)", c.Read, ref y, true);
                if (!c.Local && c.K != Kind.Action) { tbParse = Box("Réponse attendue (expression régulière, le groupe « v » = la valeur)", c.Parse, ref y, true); tbParse.TextChanged += delegate { Test(); }; }
                if (c.K == Kind.Choice) tbItems = Box("Valeurs : Libellé=code, Libellé=code|alias...   (le code est mis à la place de {v})", c.Items, ref y, true);
                if (c.K == Kind.Toggle) { tbOn = Box("Valeur « allumé » (plusieurs possibles : 1|2 ; la première est envoyée)", c.OnVal, ref y, true); tbOff = Box("Valeur « éteint »", c.OffVal, ref y, true); }
                if (c.K == Kind.Knob) { tbMin = Box("Valeur minimale", c.Min.ToString(), ref y, true); tbMax = Box("Valeur maximale", c.Max.ToString(), ref y, true); }
            }
            y += 2; lbPrev = L("", 12, y, 636, 20); lbPrev.ForeColor = T.Cyan; lbPrev.Font = new Font("Consolas", 9.5f); y += 24;
            if (c.Hint.Length > 0) { Label hh = L("Note : " + c.Hint, 12, y, 636, 50); hh.ForeColor = T.Dim; y += 52; }
            L("Gabarits : {v} valeur · {v:3} sur 3 chiffres · {v:+4} signe + 4 chiffres · {p:RU:4:;}/{n:RD:4:;} selon le signe · {wait:1000} pause en ms.", 12, y, 636, 34).ForeColor = T.Dim; y += 38;
            if (tbParse != null)
            {
                tbSample = Box("Essai : collez ici une réponse du poste (ex. FA014250000;)", "", ref y, true); tbSample.TextChanged += delegate { Test(); };
                lbRes = L("", 12, y, 636, 20); y += 24;
            }
            lbTest = L("", 12, y, 636, 36); lbTest.ForeColor = T.Dim;
            int by = ClientSize.Height - 44;
            Button bd = Btn("Défaut", 12, by, 100); bd.Click += delegate { Reset(); };
            Button bt = Btn(c.K == Kind.Action ? "Tester l'envoi" : "Tester la lecture", 120, by, 150); bt.Click += delegate { TestLive(); }; bt.Enabled = !c.Local && rig != null && rig.Connected;
            Button ok = Btn("OK", 480, by, 80); ok.DialogResult = DialogResult.None; ok.Click += delegate { if (Apply()) { DialogResult = DialogResult.OK; Close(); } };
            Button ca = Btn("Annuler", 568, by, 80); ca.DialogResult = DialogResult.Cancel; CancelButton = ca;
            Button bx = null; if (c.Custom) { bx = Btn("Supprimer", 278, by, 100); bx.ForeColor = T.Red; bx.Click += delegate { if (MessageBox.Show(this, "Supprimer l'élément « " + c.Label + " » ?\n(Retour arrière peut l'annuler tant que vous n'avez pas validé.)", "Supprimer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { Deleted = true; DialogResult = DialogResult.OK; Close(); } }; }
            ClientSize = new Size(660, Math.Max(ClientSize.Height, y + 120)); bd.Top = bt.Top = ok.Top = ca.Top = ClientSize.Height - 44; if (bx != null) bx.Top = ClientSize.Height - 44;
            Prev(); Test();
        }

        // ---- menu déroulant « Nom affiché » : toutes les commandes de la face avant du poste et de ses menus ; le choix remplit les champs
        readonly RadioDef def; ComboBox cbName; List<AddDialog.Entry> entries; string lastPick = "";
        public bool Resized, KindChanged; public string Warning = ""; public static bool Quiet;
        Kind? pKind; string pFmt; int? pMax, pW, pH, pX, pY;
        public IList<AddDialog.Entry> Entries { get { return entries; } }
        string Nm { get { return cbName != null ? cbName.Text : tbLabel.Text; } set { if (cbName != null) cbName.Text = value; else tbLabel.Text = value; } }
        void NameCombo(ref int y)
        {
            entries = AddDialog.Entries(def);
            Label l = L("Nom affiché — ou choisissez une commande dans la liste (face avant et menus du poste) : les champs ci-dessous se remplissent", 12, y, 636, 18); l.ForeColor = T.Dim; y += 19;
            cbName = new ComboBox(); cbName.DropDownStyle = ComboBoxStyle.DropDown; cbName.FlatStyle = FlatStyle.Flat; cbName.BackColor = T.Bg; cbName.ForeColor = T.Text; cbName.SetBounds(12, y, 636, 24); cbName.MaxDropDownItems = 20; cbName.DropDownWidth = 636;
            cbName.Items.AddRange(entries.ToArray()); cbName.Text = c.Label; cbName.AutoCompleteMode = AutoCompleteMode.SuggestAppend; cbName.AutoCompleteSource = AutoCompleteSource.ListItems;
            cbName.SelectionChangeCommitted += delegate { AddDialog.Entry e = cbName.SelectedItem as AddDialog.Entry; if (e != null) Pick(e); };
            cbName.Leave += delegate { string t = cbName.Text; if (t == c.Label || t == lastPick) return; foreach (AddDialog.Entry e in entries) if (e.ToString() == t) { Pick(e); return; } };
            Controls.Add(cbName); y += 32;
        }
        void Warn(string t) { Warning = t; if (!Quiet) MessageBox.Show(this, t, "Commande", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        // Choix d'une commande de la liste : remplit envoi / lecture / réponse / valeurs ; un S-mètre prend l'échelle S et une case assez grande
        public void Pick(AddDialog.Entry e)
        {
            bool s = AddDialog.IsS(e), toMeter = false;
            if (s && c.K != Kind.Meter)
            {
                if (!c.Custom) { Warn("« " + e.Name + " » est un S-mètre : cet élément (" + c.K + ") ne peut pas l'afficher.\nUtilisez « Ajouter » > « Vu-mètre » pour créer une barre S-mètre, ou choisissez ici un élément de type vu-mètre."); return; }
                toMeter = true;
            }
            lastPick = e.Name; Nm = e.Name; Warning = "";
            if (toMeter) pKind = Kind.Meter;
            if (c.K == Kind.Meter || toMeter)
            {
                pFmt = s ? "s" : (e.Fmt == "s" ? "raw" : e.Fmt); pMax = e.Max > 1 ? e.Max : 255;
                int w = c.W, h = c.H; if (toMeter) { w = 480; h = 34; } else { if (w < 240) w = 480; if (h < 26) h = 34; }
                if (w != c.W || h != c.H) { pW = w; pH = h; pX = Math.Max(0, Math.Min(c.X, 1860 - w)); pY = Math.Max(118, Math.Min(c.Y, 980 - h)); }
            }
            else if (c.K == Kind.Knob && e.Fmt != "raw") pFmt = e.Fmt;
            if (tbSet != null) tbSet.Text = toMeter ? "" : (c.K == Kind.Action && e.Set.Contains("{") ? Tpl.Fill(e.Set, c.OnSend) : e.Set);
            if (tbRead != null) tbRead.Text = e.Read; if (tbParse != null) tbParse.Text = e.Parse; if (tbItems != null && e.Items.Length > 0) tbItems.Text = e.Items;
            if (tbOn != null) { tbOn.Text = e.On; tbOff.Text = e.Off; } if (tbMin != null) { tbMin.Text = e.Min.ToString(); tbMax.Text = (e.Max > e.Min ? e.Max : 255).ToString(); }
            if (lbTest != null) { lbTest.ForeColor = T.Dim; lbTest.Text = s ? "S-mètre : affichage en échelle S" + (pW.HasValue ? ", case agrandie à " + pW + " × " + pH : "") + (toMeter ? ", l'élément devient un vu-mètre." : ".") : "Commande reprise : vérifiez envoi, lecture et réponse."; }
            Prev(); Test();
        }
        public bool Commit() { return Apply(); }

        Label L(string t, int x, int y, int w, int h) { Label l = new Label(); l.Text = t; l.SetBounds(x, y, w, h); l.ForeColor = T.Text; Controls.Add(l); return l; }
        Button Btn(string t, int x, int y, int w) { Button b = new Button(); b.Text = t; b.SetBounds(x, y, w, 30); b.FlatStyle = FlatStyle.Flat; b.BackColor = T.Panel2; b.ForeColor = T.Text; b.FlatAppearance.BorderColor = T.Edge; Controls.Add(b); return b; }
        TextBox Box(string label, string val, ref int y, bool mono)
        {
            Label l = L(label, 12, y, 636, 18); l.ForeColor = T.Dim; y += 19;
            TextBox tb = new TextBox(); tb.Text = val ?? ""; tb.SetBounds(12, y, 636, 24); tb.BackColor = T.Bg; tb.ForeColor = T.Text; tb.BorderStyle = BorderStyle.FixedSingle; if (mono) tb.Font = new Font("Consolas", 10f);
            Controls.Add(tb); y += 32; return tb;
        }

        void Prev()
        {
            if (lbPrev == null) return; string s = tbSet != null ? tbSet.Text : ""; string err = Tpl.Check(s);
            if (err != null) { lbPrev.ForeColor = T.Red; lbPrev.Text = err; return; }
            lbPrev.ForeColor = T.Cyan;
            string v = c.K == Kind.Knob ? "128" : (c.K == Kind.Freq ? "14250000" : (c.K == Kind.Toggle ? c.OnSend : (c.K == Kind.Choice && c.ItemList.Length > 0 ? c.ItemList[0].Code : "1")));
            if (c.K == Kind.Knob && c.Min < 0) v = "-120";
            lbPrev.Text = s.Length == 0 ? "" : "Exemple (valeur " + v + ") : " + Tpl.Fill(s, v);
        }
        void Test()
        {
            if (lbRes == null) return; string p = tbParse.Text; Regex re; try { re = new Regex(p); } catch (Exception ex) { lbRes.ForeColor = T.Red; lbRes.Text = "Expression invalide : " + ex.Message; return; }
            string smp = tbSample != null ? tbSample.Text.Trim() : ""; if (smp.Length == 0) { lbRes.ForeColor = T.Dim; lbRes.Text = "Expression valide."; return; }
            Match m = re.Match(smp);
            if (!m.Success) { lbRes.ForeColor = T.Red; lbRes.Text = "La réponse d'essai ne correspond pas."; return; }
            string t = "Correspond : "; foreach (string g in re.GetGroupNames()) if (!char.IsDigit(g[0]) && m.Groups[g].Success) t += g + " = " + m.Groups[g].Value + "   "; lbRes.ForeColor = T.Green; lbRes.Text = t;
        }
        void TestLive()
        {
            if (rig == null || !rig.Connected) return;
            if (c.K == Kind.Action) { rig.Raw(Tpl.Fill(tbSet.Text, null)); lbTest.ForeColor = T.Dim; lbTest.Text = "Commande envoyée : " + Tpl.Fill(tbSet.Text, null); return; }
            string cmd = tbRead.Text; if (cmd.Length == 0) { lbTest.Text = "Pas de commande de lecture."; return; }
            string a = rig.Ask(cmd, 900); lbTest.ForeColor = a == null ? T.Red : T.Green; lbTest.Text = a == null ? "Pas de réponse à " + cmd : "Réponse à " + cmd + " : " + a;
            if (a != null && tbSample != null) tbSample.Text = a;
        }
        void Reset()
        {
            c.ResetCat(); Nm = c.Label; pKind = null; pFmt = null; pMax = pW = pH = pX = pY = null;
            if (tbSet != null) tbSet.Text = c.DSet; if (tbRead != null) tbRead.Text = c.DRead; if (tbParse != null) tbParse.Text = c.DParse; if (tbItems != null) tbItems.Text = c.DItems;
            if (tbOn != null) tbOn.Text = c.DOnVal; if (tbOff != null) tbOff.Text = c.DOffVal; if (tbMin != null) tbMin.Text = c.DMin.ToString(); if (tbMax != null) tbMax.Text = c.DMax.ToString();
            Changed = true; Prev(); Test();
        }
        bool Apply()
        {
            string e1 = tbSet != null ? Tpl.Check(tbSet.Text) : null, e2 = tbRead != null ? Tpl.Check(tbRead.Text) : null;
            if (e1 != null || e2 != null) { MessageBox.Show(this, e1 ?? e2, "Commande", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            if (tbParse != null && tbParse.Text.Length > 0) { try { new Regex(tbParse.Text); } catch (Exception ex) { MessageBox.Show(this, "Expression de réponse invalide : " + ex.Message, "Commande", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; } }
            int mn = c.Min, mx = c.Max;
            if (tbMin != null && (!int.TryParse(tbMin.Text, out mn) || !int.TryParse(tbMax.Text, out mx) || mn >= mx)) { MessageBox.Show(this, "Minimum et maximum doivent être des nombres entiers (minimum < maximum).", "Commande", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            if (tbItems != null && Item.ParseList(tbItems.Text).Length == 0) { MessageBox.Show(this, "Au moins une valeur « Libellé=code » est nécessaire.", "Commande", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            if (tbOn != null && tbOn.Text.Length == 0) { MessageBox.Show(this, "La valeur « allumé » ne peut pas être vide.", "Commande", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            c.Label = Nm.Length > 0 ? Nm : c.DLabel;
            if (tbSet != null) c.Set = tbSet.Text; if (tbRead != null) c.Read = tbRead.Text; if (tbParse != null) c.Parse = tbParse.Text; if (tbItems != null) c.Items = tbItems.Text;
            if (tbOn != null) { c.OnVal = tbOn.Text; c.OffVal = tbOff.Text; } if (tbMin != null) { c.Min = mn; c.Max = mx; }
            if (pFmt != null && c.Fmt != pFmt) { if (c.DFmt == null) c.DFmt = c.Fmt; c.Fmt = pFmt; }
            if (pMax.HasValue && tbMax == null) c.Max = pMax.Value;
            if (pKind == Kind.Meter) { c.K = Kind.Meter; c.Set = ""; c.Min = 0; c.Period = 300; c.Fmt = "s"; KindChanged = true; }
            if (pW.HasValue) { c.W = pW.Value; c.H = pH.Value; c.X = pX.Value; c.Y = pY.Value; Resized = true; }
            Changed = true; return true;
        }
    }

    // Moniteur des échanges CAT, avec envoi manuel
    public class MonitorForm : Form
    {
        readonly Rig rig; readonly ListBox lb = new ListBox(); readonly TextBox tb = new TextBox(); readonly CheckBox pause = new CheckBox(); readonly Action<string, bool> h;
        readonly List<string> lines = new List<string>();
        public MonitorForm(Rig r)
        {
            rig = r; Text = "Moniteur CAT"; BackColor = T.Panel; ForeColor = T.Text; ClientSize = new Size(560, 460); Font = new Font("Segoe UI", 9f); StartPosition = FormStartPosition.Manual;
            lb.Dock = DockStyle.Fill; lb.BackColor = T.Bg; lb.ForeColor = T.Text; lb.Font = new Font("Consolas", 9.5f); lb.BorderStyle = BorderStyle.None; lb.DrawMode = DrawMode.OwnerDrawFixed; lb.ItemHeight = 17; lb.IntegralHeight = false;
            lb.DrawItem += delegate(object s, DrawItemEventArgs e)
            {
                if (e.Index < 0 || e.Index >= lines.Count) return; string t = lines[e.Index]; e.Graphics.FillRectangle(Brushes.Transparent, e.Bounds);
                using (Brush bg = new SolidBrush(T.Bg)) e.Graphics.FillRectangle(bg, e.Bounds);
                Color col = t.StartsWith("→") ? T.Amber : (t.Contains("?;") ? T.Red : T.Cyan); using (Brush b = new SolidBrush(col)) e.Graphics.DrawString(t, e.Font, b, e.Bounds.X + 2, e.Bounds.Y);
            };
            Panel bot = new Panel(); bot.Dock = DockStyle.Bottom; bot.Height = 38; bot.BackColor = T.Panel;
            tb.SetBounds(8, 7, 300, 24); tb.BackColor = T.Bg; tb.ForeColor = T.Text; tb.BorderStyle = BorderStyle.FixedSingle; tb.Font = new Font("Consolas", 10f);
            Button send = new Button(); send.Text = "Envoyer"; send.SetBounds(314, 5, 80, 28); send.FlatStyle = FlatStyle.Flat; send.BackColor = T.Panel2; send.ForeColor = T.Text; send.FlatAppearance.BorderColor = T.Edge;
            pause.Text = "Pause"; pause.SetBounds(404, 8, 70, 22); Button clr = new Button(); clr.Text = "Effacer"; clr.SetBounds(476, 5, 76, 28); clr.FlatStyle = FlatStyle.Flat; clr.BackColor = T.Panel2; clr.ForeColor = T.Text; clr.FlatAppearance.BorderColor = T.Edge;
            bot.Controls.AddRange(new Control[] { tb, send, pause, clr }); Controls.Add(lb); Controls.Add(bot);
            Action go = delegate { string t = tb.Text.Trim(); if (t.Length == 0) return; if (!t.EndsWith(";")) t += ";"; rig.Raw(t); tb.Text = ""; };
            send.Click += delegate { go(); }; tb.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { go(); e.SuppressKeyPress = true; } };
            clr.Click += delegate { lines.Clear(); lb.Items.Clear(); };
            h = delegate(string s, bool tx) { if (pause.Checked) return; lock (pend) { if (pend.Count < 2000) pend.Enqueue((tx ? "→ " : "← ") + s); } };
            fl.Interval = 150; fl.Tick += delegate { Flush(); }; fl.Start();
            rig.Log += h; FormClosed += delegate { rig.Log -= h; fl.Stop(); fl.Dispose(); };
        }
        readonly Queue<string> pend = new Queue<string>(); readonly System.Windows.Forms.Timer fl = new System.Windows.Forms.Timer();
        void Flush()
        {
            List<string> b = new List<string>(); lock (pend) { while (pend.Count > 0) b.Add(pend.Dequeue()); }
            if (b.Count == 0) return; if (b.Count > 600) b.RemoveRange(0, b.Count - 600);
            lb.BeginUpdate();
            foreach (string s in b) { lines.Add(s); lb.Items.Add(" "); }
            while (lines.Count > 600) { lines.RemoveAt(0); lb.Items.RemoveAt(0); }
            lb.TopIndex = Math.Max(0, lb.Items.Count - lb.ClientSize.Height / lb.ItemHeight); lb.EndUpdate();
        }
    }
}
