using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Forme des boutons : arrondis, plats (angles droits) ou en relief (années 90).
    public enum LookKind { Round, Flat, Bevel }

    // Un thème = une palette + une forme de boutons. Dlg = palette des fenêtres secondaires (fiche QSO, réglages...) si elle diffère.
    public class ThemeDef
    {
        public string Name = ""; public LookKind Look = LookKind.Round; public int Radius = 5; public string GridFont = "Consolas"; public bool Native, SkinMenus = true;
        public int Back, Text, EditBack, EditText, Accent, AccentText, BtnBack, BtnText, GridBack, GridAlt, GridText, HeadBack, HeadText, Sel, SelText, Line;
        public ThemeDef Dlg; public int[] Orig;
        public static readonly string[] FieldNames = { "Fond des fenêtres", "Texte", "Fond des champs de saisie", "Texte des champs de saisie", "Couleur d'accent (onglet actif)", "Texte sur l'accent", "Fond des boutons", "Texte des boutons", "Fond des tableaux", "Lignes alternées des tableaux", "Texte des tableaux", "Fond des en-têtes de colonnes", "Texte des en-têtes", "Ligne sélectionnée", "Texte de la ligne sélectionnée", "Traits et bordures" };
        public int this[int i]
        {
            get { switch (i) { case 0: return Back; case 1: return Text; case 2: return EditBack; case 3: return EditText; case 4: return Accent; case 5: return AccentText; case 6: return BtnBack; case 7: return BtnText; case 8: return GridBack; case 9: return GridAlt; case 10: return GridText; case 11: return HeadBack; case 12: return HeadText; case 13: return Sel; case 14: return SelText; default: return Line; } }
            set { switch (i) { case 0: Back = value; break; case 1: Text = value; break; case 2: EditBack = value; break; case 3: EditText = value; break; case 4: Accent = value; break; case 5: AccentText = value; break; case 6: BtnBack = value; break; case 7: BtnText = value; break; case 8: GridBack = value; break; case 9: GridAlt = value; break; case 10: GridText = value; break; case 11: HeadBack = value; break; case 12: HeadText = value; break; case 13: Sel = value; break; case 14: SelText = value; break; default: Line = value; break; } }
        }
        public void Snap() { Orig = new int[16]; for (int i = 0; i < 16; i++) Orig[i] = this[i]; }
    }

    public static class Theme
    {
        public static event Action Changed;
        public static readonly List<ThemeDef> All = Build();
        public static int Index, Version = 1;
        public static ThemeDef Cur { get { return All[Index]; } }

        static ThemeDef T(string name, LookKind look, int radius, string gridFont, params int[] c)
        {
            // back, text, editBack, editText, accent, accentText, btnBack, btnText, gridBack, gridAlt, gridText, headBack, headText, sel, selText, line
            return new ThemeDef { Name = name, Look = look, Radius = radius, GridFont = gridFont, Back = c[0], Text = c[1], EditBack = c[2], EditText = c[3], Accent = c[4], AccentText = c[5], BtnBack = c[6], BtnText = c[7], GridBack = c[8], GridAlt = c[9], GridText = c[10], HeadBack = c[11], HeadText = c[12], Sel = c[13], SelText = c[14], Line = c[15] };
        }

        static List<ThemeDef> Build()
        {
            List<ThemeDef> l = new List<ThemeDef>();
            // 0 : l'aspect d'origine du journal (fenêtre sombre bleu nuit, fiches secondaires gris clair)
            ThemeDef a = T("Actuel (nuit bleue)", LookKind.Round, 5, "Consolas", 0x161828, 0xD6E2FF, 0x1A1D30, 0xE1E8FF, 0x3C64AA, 0xFFFFFF, 0x2C3252, 0xD6E2FF, 0x1A1D30, 0x222640, 0xE1E8FF, 0x2B2F52, 0xC8D6FF, 0x2A5AAA, 0xFFFFFF, 0x33385A);
            a.SkinMenus = false;
            a.Dlg = T("Actuel (fiches)", LookKind.Round, 5, "Consolas", 0xE6E6E6, 0x000000, 0xFFFFFF, 0x000000, 0x3399FF, 0xFFFFFF, 0xE1E1E1, 0x000000, 0xFFFFFF, 0xF4F4F4, 0x000000, 0xE1E1E1, 0x000000, 0x3399FF, 0xFFFFFF, 0xC8C8C8); a.Dlg.Native = true;
            l.Add(a);
            l.Add(T("Thème IA n°1 – Anthracite moderne", LookKind.Round, 6, "Consolas", 0x1E1F22, 0xE3E5E8, 0x2B2D31, 0xF2F3F5, 0x20C997, 0x08130F, 0x313338, 0xF2F3F5, 0x232428, 0x2A2C31, 0xDCDDDE, 0x2F3136, 0x7FE6C4, 0x1F7A62, 0xFFFFFF, 0x3A3C42));
            l.Add(T("Thème IA n°2 – Windows 3.11", LookKind.Bevel, 0, "Arial", 0xC0C0C0, 0x000000, 0xFFFFFF, 0x000000, 0x000080, 0xFFFFFF, 0xC0C0C0, 0x000000, 0xFFFFFF, 0xFFFFFF, 0x000000, 0xC0C0C0, 0x000000, 0x000080, 0xFFFFFF, 0x808080));
            l.Add(T("Thème IA n°3 – Bureau 2001 (bleu / beige)", LookKind.Round, 3, "Tahoma", 0xECE9D8, 0x000000, 0xFFFFFF, 0x000000, 0x316AC5, 0xFFFFFF, 0xF4F3EE, 0x000000, 0xFFFFFF, 0xF4F7FC, 0x000000, 0xD8E4F8, 0x1A3A7A, 0x316AC5, 0xFFFFFF, 0xC6CEDE));
            l.Add(T("Thème IA n°4 – Terminal vert", LookKind.Flat, 0, "Consolas", 0x000000, 0x33FF66, 0x001A08, 0x66FF99, 0x33FF66, 0x000000, 0x002B10, 0x33FF66, 0x000A03, 0x001505, 0x33FF66, 0x003B16, 0x99FFB8, 0x33FF66, 0x000000, 0x0A4A1E));
            l.Add(T("Thème IA n°5 – Terminal ambre", LookKind.Flat, 0, "Consolas", 0x0A0600, 0xFFB000, 0x1A1000, 0xFFC640, 0xFFB000, 0x000000, 0x2B1B00, 0xFFB000, 0x100A00, 0x1A1100, 0xFFB000, 0x3D2800, 0xFFD27A, 0xFFB000, 0x000000, 0x4A3200));
            l.Add(T("Thème IA n°6 – Clair moderne", LookKind.Round, 6, "Segoe UI", 0xF5F6F8, 0x1F2328, 0xFFFFFF, 0x1F2328, 0x2563EB, 0xFFFFFF, 0xE8ECF2, 0x1F2328, 0xFFFFFF, 0xF3F6FB, 0x1F2328, 0xE3E8F0, 0x3B4656, 0x2563EB, 0xFFFFFF, 0xDDE2EA));
            l.Add(T("Thème IA n°7 – Océan", LookKind.Round, 8, "Consolas", 0x0B2A3A, 0xD8F3F8, 0x103B52, 0xF0FCFF, 0x22D3EE, 0x042A33, 0x134A66, 0xE6FAFF, 0x0E3347, 0x123D55, 0xD8F3F8, 0x17506E, 0xA5F0FF, 0x0891B2, 0xFFFFFF, 0x1F5A78));
            l.Add(T("Thème IA n°8 – Forêt", LookKind.Round, 5, "Consolas", 0x14241A, 0xE1F0D8, 0x1E3626, 0xF3FBEA, 0xA3D65C, 0x14240A, 0x2A4A33, 0xE8F5DC, 0x192D20, 0x203727, 0xDDEBD2, 0x2F5239, 0xC9EBA0, 0x4F8A2B, 0xFFFFFF, 0x2C4A35));
            l.Add(T("Thème IA n°9 – Crépuscule (violet)", LookKind.Round, 10, "Consolas", 0x1D1630, 0xEADFFF, 0x2A2142, 0xF6EFFF, 0xE056FD, 0x2A0A33, 0x3A2D5C, 0xF3EAFF, 0x211A38, 0x2A2145, 0xE8DDFB, 0x3B2F63, 0xD9B8FF, 0x9B3DD1, 0xFFFFFF, 0x3F3366));
            l.Add(T("Thème IA n°10 – Papier sépia", LookKind.Flat, 0, "Georgia", 0xF3EBD8, 0x3B2F1E, 0xFFFBF0, 0x3B2F1E, 0x9C5B1F, 0xFFFFFF, 0xE5D8BB, 0x3B2F1E, 0xFFFBF0, 0xF6EEDB, 0x3B2F1E, 0xE0D0AC, 0x5A4325, 0xB9772E, 0xFFFFFF, 0xD9C9A3));
            l.Add(T("Thème IA n°11 – Contraste élevé", LookKind.Flat, 0, "Consolas", 0x000000, 0xFFFFFF, 0x000000, 0xFFFF00, 0xFFFF00, 0x000000, 0x000000, 0xFFFF00, 0x000000, 0x101010, 0xFFFFFF, 0x000000, 0x00FFFF, 0xFFFF00, 0x000000, 0xFFFFFF));
            // ambiances mer / nature
            l.Add(T("Thème IA n°12 – Mer : lagon (clair)", LookKind.Round, 8, "Segoe UI", 0xE6F6F7, 0x0B3B44, 0xFFFFFF, 0x0B3B44, 0x0EA5A4, 0xFFFFFF, 0xCDEBEE, 0x0B3B44, 0xF7FDFD, 0xE3F4F5, 0x0B3B44, 0xBEE3E6, 0x0B4F5A, 0x0EA5A4, 0xFFFFFF, 0xB5DADD));
            l.Add(T("Thème IA n°13 – Mer : abysses", LookKind.Round, 9, "Consolas", 0x040E1A, 0xB8E6FF, 0x081A2E, 0xDDF4FF, 0x00E5FF, 0x00141A, 0x0C2A47, 0xCFF1FF, 0x06152A, 0x0A1E38, 0xBFE8FF, 0x0F3558, 0x7FE9FF, 0x0097B8, 0xFFFFFF, 0x14385C));
            l.Add(T("Thème IA n°14 – Mer : corail", LookKind.Round, 10, "Segoe UI", 0xFFF3EE, 0x3A2A2A, 0xFFFFFF, 0x3A2A2A, 0xFF6F59, 0xFFFFFF, 0xFFE0D6, 0x5A2D26, 0xFFFAF7, 0xFFEDE5, 0x3A2A2A, 0xFFD3C4, 0x7A2E22, 0xFF6F59, 0xFFFFFF, 0xF2C6B8));
            l.Add(T("Thème IA n°15 – Mer : sable et vague", LookKind.Round, 7, "Georgia", 0xF4EBDD, 0x2D3F4A, 0xFFFDF8, 0x2D3F4A, 0x1B7F9E, 0xFFFFFF, 0xE3D5BC, 0x2D3F4A, 0xFFFDF8, 0xF5EEDF, 0x2D3F4A, 0xDCE9EE, 0x1B5C73, 0x1B7F9E, 0xFFFFFF, 0xD8CBB0));
            l.Add(T("Thème IA n°16 – Mer : tempête", LookKind.Round, 6, "Consolas", 0x252E38, 0xDCE4EC, 0x303B47, 0xEEF3F8, 0x6C9BD2, 0x0B1620, 0x3A4856, 0xE6EDF4, 0x2A3440, 0x313D4A, 0xD5DEE8, 0x3E4D5C, 0xB7D0EA, 0x4F7FB5, 0xFFFFFF, 0x465566));
            l.Add(T("Thème IA n°17 – Aurore boréale", LookKind.Round, 8, "Consolas", 0x0B1B1F, 0xDFF5EA, 0x123037, 0xF0FFF7, 0x6EF0B0, 0x06241A, 0x1B4A4F, 0xE2FFF2, 0x0F2529, 0x15313A, 0xD9F4E7, 0x1E5258, 0xA8FFD6, 0x2DA67A, 0xFFFFFF, 0x24606A));
            l.Add(T("Thème IA n°18 – Menthe fraîche", LookKind.Round, 9, "Segoe UI", 0xEAF7F0, 0x1D3A2C, 0xFFFFFF, 0x1D3A2C, 0x2FBF71, 0xFFFFFF, 0xD3EEDF, 0x1D3A2C, 0xF8FDFA, 0xEAF7F0, 0x1D3A2C, 0xC5E8D4, 0x1E5A3C, 0x2FBF71, 0xFFFFFF, 0xBFE0CD));
            l.Add(T("Thème IA n°19 – Terre cuite", LookKind.Round, 6, "Georgia", 0xF6E7DA, 0x3B241A, 0xFFFBF7, 0x3B241A, 0xC2571A, 0xFFFFFF, 0xEBD0BB, 0x3B241A, 0xFFFBF7, 0xF7E9DD, 0x3B241A, 0xE5C3A8, 0x6B2F0F, 0xC2571A, 0xFFFFFF, 0xDDBEA6));
            // ambiances IA arrondies / modernes
            l.Add(T("Thème IA n°20 – Verre (bleu pâle, très arrondi)", LookKind.Round, 12, "Segoe UI", 0xEAF1FB, 0x1A2A44, 0xFFFFFF, 0x1A2A44, 0x4C8DFF, 0xFFFFFF, 0xDCE8FA, 0x1A2A44, 0xFAFCFF, 0xEEF4FC, 0x1A2A44, 0xD5E3F8, 0x24467A, 0x4C8DFF, 0xFFFFFF, 0xCCDAF0));
            l.Add(T("Thème IA n°21 – Lavande douce", LookKind.Round, 10, "Segoe UI", 0xF1ECFA, 0x2E2447, 0xFFFFFF, 0x2E2447, 0x8B5CF6, 0xFFFFFF, 0xE3D9F7, 0x2E2447, 0xFCFAFF, 0xF3EEFB, 0x2E2447, 0xDCCFF5, 0x4B2E8A, 0x8B5CF6, 0xFFFFFF, 0xD3C6EE));
            l.Add(T("Thème IA n°22 – Graphite et orange", LookKind.Round, 5, "Consolas", 0x2B2B2B, 0xEEEEEE, 0x383838, 0xFFFFFF, 0xFF8A00, 0x1A0F00, 0x3B3B3B, 0xF2F2F2, 0x303030, 0x383838, 0xE6E6E6, 0x424242, 0xFFB866, 0xE07800, 0xFFFFFF, 0x4A4A4A));
            l.Add(T("Thème IA n°23 – Minuit doré", LookKind.Round, 6, "Georgia", 0x0F0E0B, 0xEFE6CF, 0x1B1914, 0xFFF4D6, 0xD4A64A, 0x1A1405, 0x2A2519, 0xF2E3B8, 0x14120E, 0x1C1912, 0xEBDFC0, 0x2E281B, 0xE6C875, 0xB8892E, 0xFFFFFF, 0x3A3422));
            l.Add(T("Thème IA n°24 – Néon (rose et cyan)", LookKind.Flat, 0, "Consolas", 0x0A0A12, 0xE6E6FF, 0x14141F, 0xFFFFFF, 0xFF2E93, 0xFFFFFF, 0x1C1C2E, 0x00F0FF, 0x0E0E18, 0x151524, 0xD8D8FF, 0x1F1F36, 0xFF7AC0, 0xFF2E93, 0xFFFFFF, 0x2E2E4A));
            // rétro
            l.Add(T("Thème IA n°25 – Rétro : monochrome années 90", LookKind.Flat, 0, "Arial", 0xDCDCDC, 0x000000, 0xFFFFFF, 0x000000, 0x000000, 0xFFFFFF, 0xEDEDED, 0x000000, 0xFFFFFF, 0xFFFFFF, 0x000000, 0xDCDCDC, 0x000000, 0x000000, 0xFFFFFF, 0x000000));
            l.Add(T("Thème IA n°26 – Rétro : écran 4 couleurs", LookKind.Flat, 0, "Consolas", 0x000000, 0x55FFFF, 0x000000, 0xFFFFFF, 0xFF55FF, 0x000000, 0x0000AA, 0xFFFFFF, 0x000000, 0x000055, 0x55FFFF, 0x0000AA, 0xFFFF55, 0xFF55FF, 0x000000, 0x5555FF));
            l.Add(T("Thème IA n°27 – Rétro : console bleue", LookKind.Flat, 0, "Consolas", 0x0000AA, 0xFFFFFF, 0x000080, 0xFFFFFF, 0xFFFF55, 0x0000AA, 0x00AAAA, 0x000000, 0x0000AA, 0x0000C0, 0xFFFFFF, 0x00AAAA, 0x000000, 0xAAAAAA, 0x000000, 0x5555FF));
            // nouvelles ambiances
            l.Add(T("Thème IA n°28 – Solarized sombre", LookKind.Round, 5, "Consolas", 0x002B36, 0x93A1A1, 0x073642, 0xEEE8D5, 0xB58900, 0x002B36, 0x0A4150, 0xEEE8D5, 0x00252E, 0x073642, 0x93A1A1, 0x0E4A5A, 0xFDF6E3, 0x268BD2, 0xFFFFFF, 0x0F4450));
            l.Add(T("Thème IA n°29 – Solarized clair", LookKind.Round, 5, "Segoe UI", 0xFDF6E3, 0x586E75, 0xFFFFFF, 0x073642, 0x268BD2, 0xFFFFFF, 0xEEE8D5, 0x586E75, 0xFDF6E3, 0xF5EFDC, 0x586E75, 0xEEE8D5, 0x073642, 0xB4D7F0, 0x002B36, 0xE0D9C3));
            l.Add(T("Thème IA n°30 – Dracula", LookKind.Round, 7, "Consolas", 0x282A36, 0xF8F8F2, 0x343746, 0xF8F8F2, 0xBD93F9, 0x282A36, 0x44475A, 0xF8F8F2, 0x21222C, 0x282A36, 0xF8F8F2, 0x44475A, 0xF8F8F2, 0x6272A4, 0xFFFFFF, 0x3B3E52));
            l.Add(T("Thème IA n°31 – Nord (glace)", LookKind.Round, 6, "Segoe UI", 0x2E3440, 0xD8DEE9, 0x3B4252, 0xECEFF4, 0x88C0D0, 0x2E3440, 0x434C5E, 0xECEFF4, 0x2A303B, 0x303644, 0xD8DEE9, 0x434C5E, 0xECEFF4, 0x5E81AC, 0xFFFFFF, 0x3B4252));
            l.Add(T("Thème IA n°32 – Cockpit rouge", LookKind.Flat, 0, "Consolas", 0x1A0A0A, 0xFFD9D9, 0x2A1010, 0xFFEAEA, 0xFF3B30, 0xFFFFFF, 0x3A1414, 0xFFE0E0, 0x140707, 0x1D0C0C, 0xFFD0D0, 0x3A1414, 0xFFE8E8, 0x8B1A1A, 0xFFFFFF, 0x3A1818));
            l.Add(T("Thème IA n°33 – Brume (gris bleuté clair)", LookKind.Round, 6, "Segoe UI", 0xE9EDF1, 0x2B3640, 0xFFFFFF, 0x1F2933, 0x4F6D8F, 0xFFFFFF, 0xD7DEE6, 0x2B3640, 0xFFFFFF, 0xF1F4F7, 0x2B3640, 0xCBD5DF, 0x1F2933, 0xB9CCE0, 0x111A22, 0xD0D8E0));
            l.Add(T("Thème IA n°34 – Cacao (chocolat)", LookKind.Round, 7, "Georgia", 0x2A1E17, 0xF0E1D0, 0x3A2B22, 0xFFF1E0, 0xD9A066, 0x2A1E17, 0x4A372B, 0xFFF1E0, 0x231810, 0x2D2018, 0xF0E1D0, 0x4A372B, 0xFFEFD8, 0x8A5A3A, 0xFFFFFF, 0x4A372B));
            l.Add(T("Thème IA n°35 – Cerisier (rose pâle)", LookKind.Round, 11, "Segoe UI", 0xFCEEF3, 0x4A2030, 0xFFFFFF, 0x3A1626, 0xD6336C, 0xFFFFFF, 0xF6D6E2, 0x4A2030, 0xFFF8FA, 0xFCEBF1, 0x4A2030, 0xF2C4D5, 0x3A1626, 0xF3B6CB, 0x2A0F1B, 0xEBCFDA));
            l.Add(T("Thème IA n°36 – Soleil (jaune doux)", LookKind.Round, 8, "Segoe UI", 0xFFF8DC, 0x3D3300, 0xFFFFFF, 0x2E2700, 0xE6A800, 0x2E2300, 0xFBEBA8, 0x3D3300, 0xFFFDF0, 0xFFF6CF, 0x3D3300, 0xF7E08A, 0x2E2700, 0xFFE27A, 0x2E2700, 0xEDD98F));
            l.Add(T("Thème IA n°37 – Carbone (gris profond, bleu vif)", LookKind.Round, 6, "Consolas", 0x15181C, 0xC9D1D9, 0x0D1117, 0xE6EDF3, 0x58A6FF, 0x0D1117, 0x21262D, 0xC9D1D9, 0x0D1117, 0x12171E, 0xC9D1D9, 0x21262D, 0xE6EDF3, 0x1F6FEB, 0xFFFFFF, 0x30363D));
            l.Add(T("Thème IA n°38 – Radar (vert militaire)", LookKind.Flat, 0, "Consolas", 0x1C2418, 0xC8D6B0, 0x263020, 0xE0EBCB, 0x8DB255, 0x1C2418, 0x323D2A, 0xDDE8C6, 0x182014, 0x202A1B, 0xC8D6B0, 0x323D2A, 0xE0EBCB, 0x58702F, 0xFFFFFF, 0x344029));
            l.Add(T("Thème IA n°39 – Indigo électrique", LookKind.Round, 9, "Consolas", 0x0B1026, 0xCFD8FF, 0x131A3D, 0xE6EBFF, 0x4D7CFF, 0xFFFFFF, 0x1B2554, 0xDCE3FF, 0x090D20, 0x0F1530, 0xCFD8FF, 0x1B2554, 0xE6EBFF, 0x2F4FD0, 0xFFFFFF, 0x222C5E));
            l.Add(T("Thème IA n°40 – Aube (pêche et ciel)", LookKind.Round, 12, "Segoe UI", 0xFFF1E6, 0x40302A, 0xFFFFFF, 0x30221C, 0xFF8A5B, 0xFFFFFF, 0xFFDCC8, 0x40302A, 0xFFFAF5, 0xFFEBDD, 0x40302A, 0xFFD2B8, 0x30221C, 0xBFDFF5, 0x1A2A36, 0xF0D9C8));
            // palettes modernes très répandues
            l.Add(T("Thème IA n°41 – Tokyo Night", LookKind.Round, 8, "Consolas", 0x1A1B26, 0xC0CAF5, 0x24283B, 0xC0CAF5, 0x7AA2F7, 0x1A1B26, 0x2F334D, 0xC0CAF5, 0x16161E, 0x1A1B26, 0xA9B1D6, 0x292E42, 0xC0CAF5, 0x3D59A1, 0xFFFFFF, 0x3B4261));
            l.Add(T("Thème IA n°42 – Catppuccin Mocha", LookKind.Round, 10, "Segoe UI", 0x1E1E2E, 0xCDD6F4, 0x313244, 0xCDD6F4, 0xCBA6F7, 0x1E1E2E, 0x45475A, 0xCDD6F4, 0x181825, 0x1E1E2E, 0xCDD6F4, 0x313244, 0xF5E0DC, 0x585B70, 0xFFFFFF, 0x45475A));
            l.Add(T("Thème IA n°43 – Catppuccin Latte (clair)", LookKind.Round, 10, "Segoe UI", 0xEFF1F5, 0x4C4F69, 0xFFFFFF, 0x4C4F69, 0x8839EF, 0xFFFFFF, 0xDCE0E8, 0x4C4F69, 0xFFFFFF, 0xF2F4F8, 0x4C4F69, 0xCCD0DA, 0x4C4F69, 0xBCC0CC, 0x1E1E2E, 0xBCC0CC));
            l.Add(T("Thème IA n°44 – Monokai", LookKind.Flat, 0, "Consolas", 0x272822, 0xF8F8F2, 0x1E1F1C, 0xF8F8F2, 0xA6E22E, 0x272822, 0x3E3D32, 0xF8F8F2, 0x1E1F1C, 0x272822, 0xF8F8F2, 0x3E3D32, 0xF8F8F2, 0x49483E, 0xFFFFFF, 0x49483E));
            l.Add(T("Thème IA n°45 – Gruvbox sombre", LookKind.Round, 4, "Consolas", 0x282828, 0xEBDBB2, 0x3C3836, 0xEBDBB2, 0xFABD2F, 0x282828, 0x504945, 0xEBDBB2, 0x1D2021, 0x282828, 0xEBDBB2, 0x3C3836, 0xFBF1C7, 0x665C54, 0xFFFFFF, 0x504945));
            l.Add(T("Thème IA n°46 – One Dark", LookKind.Round, 6, "Consolas", 0x282C34, 0xABB2BF, 0x21252B, 0xD7DAE0, 0x61AFEF, 0x282C34, 0x393F4A, 0xABB2BF, 0x21252B, 0x282C34, 0xABB2BF, 0x2C313A, 0xD7DAE0, 0x3E4451, 0xFFFFFF, 0x3B4048));
            l.Add(T("Thème IA n°47 – Rosé Pine", LookKind.Round, 10, "Segoe UI", 0x191724, 0xE0DEF4, 0x1F1D2E, 0xE0DEF4, 0xEBBCBA, 0x191724, 0x26233A, 0xE0DEF4, 0x1F1D2E, 0x191724, 0xE0DEF4, 0x26233A, 0xF6C177, 0x403D52, 0xFFFFFF, 0x403D52));
            l.Add(T("Thème IA n°48 – Material océan", LookKind.Round, 7, "Segoe UI", 0x0F111A, 0xA6ACCD, 0x1A1C25, 0xEEFFFF, 0x80CBC4, 0x0F111A, 0x1F2233, 0xA6ACCD, 0x090B10, 0x0F111A, 0xA6ACCD, 0x1A1C25, 0xEEFFFF, 0x2B3050, 0xFFFFFF, 0x232637));
            l.Add(T("Thème IA n°49 – Cyber jaune et noir", LookKind.Flat, 0, "Consolas", 0x0D0D0D, 0xFCEE0A, 0x1A1A1A, 0xFCEE0A, 0xFCEE0A, 0x000000, 0x262626, 0xFCEE0A, 0x111111, 0x161616, 0xE8DC10, 0x2A2A00, 0xFFF76B, 0x8A8200, 0xFFFFFF, 0x3D3A00));
            l.Add(T("Thème IA n°50 – Ardoise et turquoise", LookKind.Round, 8, "Segoe UI", 0x263238, 0xECEFF1, 0x2E3C43, 0xFFFFFF, 0x26C6DA, 0x00252B, 0x37474F, 0xECEFF1, 0x1F292E, 0x263238, 0xECEFF1, 0x37474F, 0xFFFFFF, 0x00838F, 0xFFFFFF, 0x455A64));
            l.Add(T("Thème IA n°51 – Ciel d'été (clair vif)", LookKind.Round, 12, "Segoe UI", 0xF0F9FF, 0x0C4A6E, 0xFFFFFF, 0x0C4A6E, 0x0EA5E9, 0xFFFFFF, 0xE0F2FE, 0x0C4A6E, 0xFFFFFF, 0xF0F9FF, 0x0C4A6E, 0xBAE6FD, 0x075985, 0x7DD3FC, 0x082F49, 0xBAE6FD));
            l.Add(T("Thème IA n°52 – Rubis (carmin et gris)", LookKind.Round, 7, "Segoe UI", 0x211B1E, 0xF3E8EC, 0x2D2429, 0xFFFFFF, 0xE11D48, 0xFFFFFF, 0x3A2E34, 0xF3E8EC, 0x1A1518, 0x211B1E, 0xF3E8EC, 0x3A2E34, 0xFFE4EA, 0x9F1239, 0xFFFFFF, 0x4A3A41));
            l.Add(T("Thème IA n°53 – Papier blanc et vert", LookKind.Round, 6, "Segoe UI", 0xFFFFFF, 0x1B1F23, 0xFFFFFF, 0x1B1F23, 0x16A34A, 0xFFFFFF, 0xF1F5F2, 0x1B1F23, 0xFFFFFF, 0xF6F8F6, 0x1B1F23, 0xE4EFE7, 0x14532D, 0x86EFAC, 0x052E16, 0xD5DDD7));
            foreach (ThemeDef d in l) { d.Snap(); if (d.Dlg != null) d.Dlg.Snap(); }
            return l;
        }

        public static Color C(int v) { return Color.FromArgb((v >> 16) & 255, (v >> 8) & 255, v & 255); }
        public static Color Mix(Color a, Color b, double t) { return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        public static bool IsDark(Color c) { return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) < 128; }
        public static Color Lift(Color c, float f) { return IsDark(c) ? ControlPaint.Light(c, f) : ControlPaint.Dark(c, f / 2); }

        public static Color Get(string key)
        {
            ThemeDef t = Cur;
            switch (key)
            {
                case "lb.back": return C(t.Back); case "lb.tool.text": return C(t.Text);
                case "lb.grid.back": return C(t.GridBack); case "lb.grid.alt": return C(t.GridAlt); case "lb.grid.text": return C(t.GridText);
                case "lb.grid.sel": return C(t.Sel); case "lb.grid.seltext": return C(t.SelText); case "lb.grid.line": return C(t.Line);
                case "lb.head.back": return C(t.HeadBack); case "lb.head.text": return C(t.HeadText);
                case "lb.accent": return C(t.Accent); case "lb.accent.text": return C(t.AccentText);
                case "lb.btn.back": return C(t.BtnBack); case "lb.btn.text": return C(t.BtnText);
                case "lb.map.back": return C(t.GridBack); case "lb.map.text": return C(t.Text);
                case "lb.spot": return IsDark(C(t.GridBack)) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(138, 90, 0);
                case "lb.map.new": return C(0xFF5050); case "lb.map.alert": return C(0x30D070); case "lb.map.m": return C(0x4A8CFF);
            }
            return Color.Gray;
        }

        public static string Key(int idx, bool dlg, int k) { return "theme." + idx + (dlg ? ".d" : ".c") + k; }
        public static void Load()
        {
            int i; if (!int.TryParse(App.Cfg.Get("theme.n", "0"), out i) || i < 0 || i >= All.Count) i = 0; Index = i;
            for (int n = 0; n < All.Count; n++) foreach (bool dlg in new[] { false, true })
            {
                ThemeDef d = dlg ? All[n].Dlg : All[n]; if (d == null) continue;
                for (int k = 0; k < 16; k++) { string s = App.Cfg.Get(Key(n, dlg, k), ""); int v; if (s.Length == 6 && int.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out v)) d[k] = v; }
            }
            Version++;
        }
        // Couleur personnalisée : appliquée tout de suite à toutes les fenêtres et mémorisée pour ce thème
        public static void SetColor(int idx, bool dlg, int k, int rgb)
        {
            ThemeDef d = dlg ? All[idx].Dlg : All[idx]; if (d == null) return; d[k] = rgb; App.Cfg.Set(Key(idx, dlg, k), rgb.ToString("X6")); try { App.Cfg.Save(); } catch { }
            Version++; Fire();
        }
        public static void ResetTheme(int idx)
        {
            foreach (bool dlg in new[] { false, true }) { ThemeDef d = dlg ? All[idx].Dlg : All[idx]; if (d == null) continue; for (int k = 0; k < 16; k++) { d[k] = d.Orig[k]; App.Cfg.Set(Key(idx, dlg, k), ""); } }
            try { App.Cfg.Save(); } catch { } Version++; Fire();
        }
        public static void Set(int i) { if (i < 0 || i >= All.Count) return; Index = i; Version++; App.Cfg.Set("theme.n", i.ToString()); try { App.Cfg.Save(); } catch { } Fire(); }
        public static void Fire() { Action a = Changed; if (a != null) a(); }
    }

    // Boutons d'onglets et boutons de la ligne du haut : dessinés selon la forme du thème.
    public static class Looks
    {
        public static int Override = -1; public static bool Dark = true;
        public static int LookOf(string part) { return 0; }
        public static void Hook(Button b) { }
        public static Color DrawButton(Graphics g, Rectangle r, Color? fill, Color fore, bool hot, bool down, bool enabled, Color parentBack)
        {
            ThemeDef t = Theme.Cur; Color bg = fill ?? Theme.Get("lb.btn.back"); if (hot && !fill.HasValue) bg = Theme.Lift(bg, 0.25f);
            if (t.Look == LookKind.Round)
            {
                using (GraphicsPath gp = Round(r, Math.Max(2, t.Radius))) using (SolidBrush b = new SolidBrush(bg)) { g.SmoothingMode = SmoothingMode.AntiAlias; g.FillPath(b, gp); }
            }
            else
            {
                g.SmoothingMode = SmoothingMode.None; Rectangle q = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
                using (SolidBrush b = new SolidBrush(bg)) g.FillRectangle(b, q);
                if (t.Look == LookKind.Flat) using (Pen p = new Pen(fill.HasValue ? Theme.Get("lb.accent") : Theme.Mix(Theme.Get("lb.tool.text"), bg, 0.55))) g.DrawRectangle(p, q);
                else Skin.DrawBevel(g, q, down);
            }
            return fore;
        }
        public static GraphicsPath Round(Rectangle r, int d)
        {
            GraphicsPath p = new GraphicsPath(); int k = d * 2; r.Width -= 1; r.Height -= 1;
            p.AddArc(r.X, r.Y, k, k, 180, 90); p.AddArc(r.Right - k, r.Y, k, k, 270, 90); p.AddArc(r.Right - k, r.Bottom - k, k, k, 0, 90); p.AddArc(r.X, r.Bottom - k, k, k, 90, 90); p.CloseFigure(); return p;
        }
    }

    // Applique le thème à toutes les fenêtres du journal et à leurs sous-fenêtres, même celles créées après coup.
    public static class Skin
    {
        static bool installed; static readonly Dictionary<Form, int> applied = new Dictionary<Form, int>();
        static readonly ConditionalWeakTable<Control, string> roles = new ConditionalWeakTable<Control, string>();
        static readonly ConditionalWeakTable<Control, object> hooked = new ConditionalWeakTable<Control, object>();
        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        static readonly SkinRenderer renderer = new SkinRenderer(); static bool rendererTouched;

        public static void Install()
        {
            if (installed) return; installed = true; Theme.Load();
            Application.Idle += delegate { ApplyAll(); };
            Theme.Changed += delegate { ApplyAll(); foreach (Form f in Forms()) f.Invalidate(true); };
        }

        static List<Form> Forms() { List<Form> l = new List<Form>(); foreach (Form f in Application.OpenForms) if (f != null && !f.IsDisposed && IsJournal(f)) l.Add(f); return l; }
        static bool IsJournal(Form f) { for (Form x = f; x != null; x = x.Owner) if ((x.GetType().Namespace ?? "").StartsWith("HamRadioLink.Journal")) return true; return false; }

        public static void ApplyAll()
        {
            try
            {
                if (Theme.Cur.SkinMenus || rendererTouched) { rendererTouched = true; ToolStripManager.Renderer = Theme.Cur.SkinMenus ? (ToolStripRenderer)renderer : new ToolStripProfessionalRenderer(); }
                foreach (Form f in Forms()) { int v; if (!applied.TryGetValue(f, out v) || v != Theme.Version) Apply(f); }
            }
            catch { }
        }

        static ThemeDef PalFor(Form f) { ThemeDef t = Theme.Cur; return (f is LogbookForm || f is AwardsForm) ? t : (t.Dlg ?? t); }

        public static void Apply(Form f)
        {
            ThemeDef p = PalFor(f); Classify(f); Paint(f, p); Hook(f);
            bool first = !applied.ContainsKey(f); applied[f] = Theme.Version; if (first) f.FormClosed += delegate { applied.Remove(f); };
            DarkTitle(f, Theme.IsDark(Theme.C(p.Back)));
        }

        // ----- rôles des étiquettes, lus AVANT toute modification (une couleur voulue par le programme est conservée) -----
        static void Classify(Control c)
        {
            string r;
            if (!roles.TryGetValue(c, out r))
            {
                r = "";
                if (c is Label || c is CheckBox || c is RadioButton)
                {
                    Color par = c.Parent != null ? c.Parent.ForeColor : SystemColors.ControlText;
                    if (c.ForeColor != par) r = c.ForeColor == SystemColors.GrayText ? "dim" : "keep";
                }
                else if (c is TextBox && c.BackColor == Color.FromArgb(255, 255, 200)) r = "hl";
                roles.Add(c, r);
            }
            foreach (Control k in c.Controls) Classify(k);
        }

        static void Hook(Control c)
        {
            object o; if (!hooked.TryGetValue(c, out o)) { hooked.Add(c, c); c.ControlAdded += OnAdded; }
            foreach (Control k in c.Controls) Hook(k);
        }
        static void OnAdded(object s, ControlEventArgs e)
        {
            try { Form f = e.Control.FindForm(); if (f == null || !IsJournal(f)) return; Classify(e.Control); Paint(e.Control, PalFor(f)); Hook(e.Control); } catch { }
        }

        static Font GridFontOf(ThemeDef p, Font basis) { string k = p.GridFont + "|" + basis.Size; Font f; if (!fonts.TryGetValue(k, out f)) { try { f = new Font(p.GridFont, basis.Size); } catch { f = basis; } fonts[k] = f; } return f; }

        static void Paint(Control c, ThemeDef p)
        {
            Color back = Theme.C(p.Back), text = Theme.C(p.Text), eb = Theme.C(p.EditBack), et = Theme.C(p.EditText); string role; roles.TryGetValue(c, out role);
            if (c is Form) { c.BackColor = back; c.ForeColor = text; }
            else if (c is DataGridView) Grid((DataGridView)c, p);
            else if (c is TextBox || c is RichTextBox)
            {
                bool hl = role == "hl", light = !Theme.IsDark(eb);   // champ « indicatif » : jaune pâle sur fond clair, teinte d'accent sur fond sombre
                c.BackColor = hl ? (light ? Color.FromArgb(255, 255, 200) : Theme.Mix(Theme.C(p.Accent), eb, 0.65)) : eb; c.ForeColor = hl && light ? Color.Black : et;
                TextBox tb = c as TextBox; if (tb != null && !p.Native) { BorderStyle bs = p.Look == LookKind.Bevel ? BorderStyle.Fixed3D : BorderStyle.FixedSingle; if (tb.BorderStyle != bs) tb.BorderStyle = bs; }
            }
            else if (c is ComboBox) { ComboBox cb = (ComboBox)c; cb.BackColor = eb; cb.ForeColor = et; cb.FlatStyle = p.Native ? FlatStyle.Standard : FlatStyle.Flat; }
            else if (c is ListBox || c is ListView || c is TreeView) { c.BackColor = eb; c.ForeColor = et; }
            else if (c is NumericUpDown) { c.BackColor = eb; c.ForeColor = et; }
            else if (c is DateTimePicker) { DateTimePicker d = (DateTimePicker)c; d.CalendarMonthBackground = eb; d.CalendarForeColor = et; d.CalendarTitleBackColor = Theme.C(p.Accent); d.CalendarTitleForeColor = Theme.C(p.AccentText); }
            else if (c is Button) Btn((Button)c, p);
            else if (c is CheckBox || c is RadioButton || c is Label)
            {
                if (role == "dim") c.ForeColor = Theme.Mix(text, back, 0.5); else if (role != "keep") c.ForeColor = text;
            }
            else if (c is TabControl) Tabs((TabControl)c, p);
            else if (c is ToolStrip) Strip((ToolStrip)c, p);
            else if (c is GroupBox || c is TabPage || c is Panel || c is UserControl) { c.BackColor = back; c.ForeColor = text; }
            foreach (Control k in c.Controls) Paint(k, p);
        }

        // ----- boutons -----
        static void Btn(Button b, ThemeDef p)
        {
            if (p.Native) { b.FlatStyle = FlatStyle.Standard; b.UseVisualStyleBackColor = true; b.ForeColor = Theme.C(p.Text); b.Region = null; return; }
            Color bb = Theme.C(p.BtnBack), bt = Theme.C(p.BtnText);
            b.FlatStyle = FlatStyle.Flat; b.BackColor = bb; b.ForeColor = bt; b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderColor = p.Look == LookKind.Bevel ? Theme.C(p.Line) : Theme.Mix(Theme.C(p.Text), bb, 0.6); b.FlatAppearance.BorderSize = p.Look == LookKind.Bevel ? 0 : 1;
            b.FlatAppearance.MouseOverBackColor = Theme.Lift(bb, 0.25f); b.FlatAppearance.MouseDownBackColor = Theme.Lift(bb, 0.4f);
            object o; if (!btnHooked.TryGetValue(b, out o)) { btnHooked.Add(b, b); b.SizeChanged += delegate { Shape(b); }; b.Paint += BevelPaint; }
            Shape(b);
        }
        static readonly ConditionalWeakTable<Control, object> btnHooked = new ConditionalWeakTable<Control, object>();
        static void Shape(Button b)
        {
            Form f = b.FindForm(); ThemeDef p = f != null ? PalFor(f) : Theme.Cur;
            if (p.Native || p.Look != LookKind.Round || b.Width < 12 || b.Height < 12) { if (b.Region != null) b.Region = null; return; }
            using (GraphicsPath gp = new GraphicsPath())
            { int k = Math.Max(4, p.Radius * 2), w = b.Width, h = b.Height; gp.AddArc(0, 0, k, k, 180, 90); gp.AddArc(w - k, 0, k, k, 270, 90); gp.AddArc(w - k, h - k, k, k, 0, 90); gp.AddArc(0, h - k, k, k, 90, 90); gp.CloseFigure(); b.Region = new Region(gp); }
        }
        static void BevelPaint(object s, PaintEventArgs e)
        {
            Button b = (Button)s; Form f = b.FindForm(); if (f == null) return; ThemeDef p = PalFor(f); if (p.Native || p.Look != LookKind.Bevel) return;
            bool down = b.Capture && (Control.MouseButtons & MouseButtons.Left) != 0 && b.ClientRectangle.Contains(b.PointToClient(Cursor.Position));
            DrawBevel(e.Graphics, new Rectangle(0, 0, b.Width - 1, b.Height - 1), down);
        }
        // Relief classique : clair en haut à gauche, sombre en bas à droite (inversé quand on appuie)
        public static void DrawBevel(Graphics g, Rectangle r, bool down)
        {
            Color hi = Color.White, mid = Color.FromArgb(223, 223, 223), lo = Color.FromArgb(128, 128, 128), dk = Color.FromArgb(32, 32, 32);
            Color tl1 = down ? dk : hi, tl2 = down ? lo : mid, br1 = down ? hi : dk, br2 = down ? mid : lo;
            using (Pen a = new Pen(tl1)) using (Pen b = new Pen(tl2)) using (Pen c = new Pen(br1)) using (Pen d = new Pen(br2))
            {
                g.DrawLine(a, r.Left, r.Top, r.Right, r.Top); g.DrawLine(a, r.Left, r.Top, r.Left, r.Bottom); g.DrawLine(b, r.Left + 1, r.Top + 1, r.Right - 1, r.Top + 1); g.DrawLine(b, r.Left + 1, r.Top + 1, r.Left + 1, r.Bottom - 1);
                g.DrawLine(c, r.Left, r.Bottom, r.Right, r.Bottom); g.DrawLine(c, r.Right, r.Top, r.Right, r.Bottom); g.DrawLine(d, r.Left + 1, r.Bottom - 1, r.Right - 1, r.Bottom - 1); g.DrawLine(d, r.Right - 1, r.Top + 1, r.Right - 1, r.Bottom - 1);
            }
        }

        // ----- tableaux -----
        static void Grid(DataGridView g, ThemeDef p)
        {
            g.BackgroundColor = Theme.C(p.GridBack); g.GridColor = Theme.C(p.Line); g.EnableHeadersVisualStyles = false; g.BorderStyle = BorderStyle.None;
            DataGridViewCellStyle d = g.DefaultCellStyle; d.BackColor = Theme.C(p.GridBack); d.ForeColor = Theme.C(p.GridText); d.SelectionBackColor = Theme.C(p.Sel); d.SelectionForeColor = Theme.C(p.SelText);
            DataGridViewCellStyle h = g.ColumnHeadersDefaultCellStyle; h.BackColor = Theme.C(p.HeadBack); h.ForeColor = Theme.C(p.HeadText); h.SelectionBackColor = h.BackColor; h.SelectionForeColor = h.ForeColor;
            g.AlternatingRowsDefaultCellStyle.BackColor = Theme.C(p.GridAlt); g.AlternatingRowsDefaultCellStyle.ForeColor = Theme.C(p.GridText);
            g.DefaultCellStyle.Font = GridFontOf(p, g.Font); g.Invalidate();
        }

        // ----- onglets -----
        static void Tabs(TabControl t, ThemeDef p)
        {
            if (p.Native) { t.DrawMode = TabDrawMode.Normal; return; }
            t.DrawMode = TabDrawMode.OwnerDrawFixed; object o;
            if (!tabHooked.TryGetValue(t, out o)) { tabHooked.Add(t, t); t.DrawItem += TabDraw; }
            t.Invalidate();
        }
        static readonly ConditionalWeakTable<Control, object> tabHooked = new ConditionalWeakTable<Control, object>();
        static void TabDraw(object s, DrawItemEventArgs e)
        {
            TabControl t = (TabControl)s; Form f = t.FindForm(); ThemeDef p = f != null ? PalFor(f) : Theme.Cur; bool sel = e.Index == t.SelectedIndex;
            Color bg = sel ? Theme.C(p.Sel) : Theme.C(p.BtnBack), fg = sel ? Theme.C(p.SelText) : Theme.C(p.BtnText);
            using (SolidBrush b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(e.Graphics, t.TabPages[e.Index].Text, t.Font, e.Bounds, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // ----- menus et barres -----
        static void Strip(ToolStrip ts, ThemeDef p)
        {
            Color back = Theme.C(p.Back), text = Theme.C(p.Text); ts.BackColor = back; ts.ForeColor = text;
            foreach (ToolStripItem i in ts.Items) StripItem(i, p);
        }
        static void StripItem(ToolStripItem i, ThemeDef p)
        {
            if (!(i is ToolStripControlHost)) i.ForeColor = Theme.C(p.Text);
            ToolStripDropDownItem d = i as ToolStripDropDownItem;
            if (d != null) foreach (ToolStripItem k in d.DropDownItems) { k.BackColor = Theme.C(p.EditBack); StripItem(k, p); }
        }

        // ----- barre de titre sombre (Windows 10) -----
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        [DllImport("user32.dll")] static extern bool RedrawWindow(IntPtr h, IntPtr r, IntPtr rgn, uint flags);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        static readonly System.Collections.Generic.HashSet<Form> frameFix = new System.Collections.Generic.HashSet<Form>();
        // Windows 10 + barre de titre sombre : les boutons réduire / agrandir / fermer disparaissent parfois quand on réduit la fenêtre → on force le redessin du cadre
        public static void FrameFix(Form f)
        {
            if (!frameFix.Add(f)) return;
            f.FormClosed += delegate { frameFix.Remove(f); };
            f.Resize += delegate { try { if (f.IsHandleCreated) RedrawWindow(f.Handle, IntPtr.Zero, IntPtr.Zero, 0x401); } catch { } };
            f.ResizeEnd += delegate { try { if (f.IsHandleCreated) SetWindowPos(f.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x37); } catch { } };
            f.SizeChanged += delegate { try { if (f.IsHandleCreated && f.WindowState != FormWindowState.Minimized) f.BeginInvoke((Action)delegate { try { SetWindowPos(f.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x37); } catch { } }); } catch { } };
        }
        static void DarkTitle(Form f, bool dark)
        {
            FrameFix(f);
            try { if (!f.IsHandleCreated) return; int v = dark ? 1 : 0; if (DwmSetWindowAttribute(f.Handle, 20, ref v, 4) != 0) DwmSetWindowAttribute(f.Handle, 19, ref v, 4); } catch { }
        }
    }

    // Menus déroulants et clic droit aux couleurs du thème
    class SkinRenderer : ToolStripProfessionalRenderer
    {
        public SkinRenderer() : base(new SkinColors()) { RoundedEdges = false; }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Get("lb.tool.text") : Theme.Mix(Theme.Get("lb.tool.text"), Theme.Get("lb.back"), 0.55); base.OnRenderItemText(e);
        }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) { e.ArrowColor = Theme.Get("lb.tool.text"); base.OnRenderArrow(e); }
    }
    class SkinColors : ProfessionalColorTable
    {
        static Color B() { return Theme.Get("lb.back"); }
        static Color E() { return Theme.Mix(Theme.Get("lb.back"), Theme.Get("lb.grid.back"), 0.5); }
        static Color S() { return Theme.Mix(Theme.Get("lb.back"), Theme.Get("lb.accent"), 0.45); }
        static Color L() { return Theme.Get("lb.grid.line"); }
        public override Color ToolStripDropDownBackground { get { return E(); } }
        public override Color MenuBorder { get { return L(); } }
        public override Color MenuItemBorder { get { return Theme.Get("lb.accent"); } }
        public override Color MenuItemSelected { get { return S(); } }
        public override Color MenuItemSelectedGradientBegin { get { return S(); } }
        public override Color MenuItemSelectedGradientEnd { get { return S(); } }
        public override Color MenuItemPressedGradientBegin { get { return S(); } }
        public override Color MenuItemPressedGradientMiddle { get { return S(); } }
        public override Color MenuItemPressedGradientEnd { get { return S(); } }
        public override Color MenuStripGradientBegin { get { return B(); } }
        public override Color MenuStripGradientEnd { get { return B(); } }
        public override Color ImageMarginGradientBegin { get { return E(); } }
        public override Color ImageMarginGradientMiddle { get { return E(); } }
        public override Color ImageMarginGradientEnd { get { return E(); } }
        public override Color SeparatorDark { get { return L(); } }
        public override Color SeparatorLight { get { return L(); } }
        public override Color ToolStripGradientBegin { get { return B(); } }
        public override Color ToolStripGradientMiddle { get { return B(); } }
        public override Color ToolStripGradientEnd { get { return B(); } }
        public override Color ToolStripContentPanelGradientBegin { get { return B(); } }
        public override Color ToolStripContentPanelGradientEnd { get { return B(); } }
        public override Color StatusStripGradientBegin { get { return B(); } }
        public override Color StatusStripGradientEnd { get { return B(); } }
        public override Color ButtonSelectedHighlight { get { return S(); } }
        public override Color ButtonSelectedGradientBegin { get { return S(); } }
        public override Color ButtonSelectedGradientMiddle { get { return S(); } }
        public override Color ButtonSelectedGradientEnd { get { return S(); } }
        public override Color ButtonSelectedBorder { get { return Theme.Get("lb.accent"); } }
        public override Color CheckBackground { get { return S(); } }
        public override Color CheckSelectedBackground { get { return S(); } }
    }
}
