using System;
using System.Collections.Generic;
using System.Drawing;

namespace HamRadioLink
{
    // « Aligner » (vue config) : dans chaque section, les boutons de même nature et de taille très proche prennent la même taille
    // (la plus fréquente du groupe), de même pour les potentiomètres ; le centre de chaque élément ne bouge pas.
    public static class Align
    {
        const double Tol = 0.35;                     // écart relatif maximal (largeur et hauteur) pour que deux éléments soient « de même forme »
        static int Cls(Ctl c)
        {
            if (c.K == Kind.Knob) return 2;
            if (c.K == Kind.Toggle || c.K == Kind.Action || (c.K == Kind.Choice && !c.Row)) return 1;
            return 0;
        }
        static bool Near(Ctl a, int w, int h) { return Math.Abs(a.W - w) <= Tol * w && Math.Abs(a.H - h) <= Tol * h; }
        // retourne le nombre d'éléments dont la taille a changé
        public static int Run(RadioDef d)
        {
            Dictionary<Ctl, int[]> orig = new Dictionary<Ctl, int[]>(); foreach (Ctl c in d.C) orig[c] = new int[] { c.W, c.H };
            HashSet<Ctl> moved = new HashSet<Ctl>(); int n, it = 0;
            do { n = Pass(d, orig, moved); } while (n > 0 && ++it < 8);     // plusieurs passes jusqu'à stabilité (un groupe fusionné peut en rejoindre un autre)
            return moved.Count;
        }
        static int Pass(RadioDef d, Dictionary<Ctl, int[]> orig, HashSet<Ctl> moved)
        {
            List<Ctl> frames = new List<Ctl>(); foreach (Ctl c in d.C) if (c.K == Kind.Frame) frames.Add(c);
            Dictionary<string, List<Ctl>> groups = new Dictionary<string, List<Ctl>>();
            foreach (Ctl c in d.C)
            {
                int k = Cls(c); if (k == 0 || c.Hidden || c.W < 8 || c.H < 8) continue;
                int fi = -1; long best = long.MaxValue; double cx = c.X + c.W / 2.0, cy = c.Y + c.H / 2.0;
                for (int i = 0; i < frames.Count; i++) { Ctl f = frames[i]; if (cx >= f.X && cx <= f.X + f.W && cy >= f.Y && cy <= f.Y + f.H) { long a = (long)f.W * f.H; if (a < best) { best = a; fi = i; } } }
                string key = fi + "/" + k; List<Ctl> l; if (!groups.TryGetValue(key, out l)) groups[key] = l = new List<Ctl>(); l.Add(c);
            }
            int changed = 0;
            foreach (List<Ctl> g in groups.Values)
            {
                List<Ctl> rest = new List<Ctl>(g);
                while (rest.Count >= 2)
                {
                    // élément autour duquel se regroupent le plus de voisins de taille proche
                    Ctl seed = null; int bestN = 0;
                    foreach (Ctl s in rest) { int n = 0; foreach (Ctl o in rest) if (Near(o, s.W, s.H)) n++; if (n > bestN) { bestN = n; seed = s; } }
                    if (seed == null || bestN < 2) break;
                    List<Ctl> cl = rest.FindAll(delegate(Ctl o) { return Near(o, seed.W, seed.H); }); rest.RemoveAll(delegate(Ctl o) { return cl.Contains(o); });
                    // taille cible : la plus fréquente, à égalité la plus grande
                    int tw = 0, th = 0, cnt = 0;
                    foreach (Ctl a in cl) { int n = 0; foreach (Ctl b in cl) if (b.W == a.W && b.H == a.H) n++; if (n > cnt || (n == cnt && (long)a.W * a.H > (long)tw * th)) { cnt = n; tw = a.W; th = a.H; } }
                    foreach (Ctl c in cl)
                    {
                        if (c.W == tw && c.H == th) continue;
                        int[] o0 = orig[c]; if (Math.Abs(o0[0] - tw) > Tol * tw || Math.Abs(o0[1] - th) > Tol * th) continue;   // jamais plus de 35 % d'écart avec la taille d'origine
                        c.X += (c.W - tw) / 2; c.Y += (c.H - th) / 2; c.W = tw; c.H = th; changed++; moved.Add(c);
                    }
                }
            }
            return changed;
        }
    }
}
