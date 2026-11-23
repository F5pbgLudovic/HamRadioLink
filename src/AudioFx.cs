using System;

namespace HamRadioLink
{
    // Réglages de l'égaliseur 10 bandes (émission « tx » et réception « rx ») et du compresseur / limiteur d'émission.
    // Mémorisés dans settings.ini (fx.tx.* / fx.rx.*) ; le fil audio lit un instantané immuable (Get), remplacé à chaque changement.
    public class FxSet
    {
        public int Ver; public bool EqOn, CompOn;
        public double[] G = new double[10];            // gain de chaque bande, dB
        public double Thr = -18, Ratio = 3, Makeup = 3, Ceil = -1, Att = 5, Rel = 150;   // compresseur : seuil dB, rapport, gain de sortie dB, plafond du limiteur dB, attaque ms, relâche ms
    }

    public static class FxCfg
    {
        public static readonly int[] Freq = { 31, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        public const double Q = 1.41;
        static readonly FxSet[] cur = new FxSet[2]; static int ver;
        public static volatile float GrTx;   // réduction de gain actuelle du compresseur (dB, positive)

        static string K(bool tx, string s) { return "fx." + (tx ? "tx" : "rx") + "." + s; }
        static double D(bool tx, string s, double def) { int v; return int.TryParse(Cfg.Get(K(tx, s), ""), out v) ? v / 10.0 : def; }
        static void SD(bool tx, string s, double v) { Cfg.Set(K(tx, s), ((int)Math.Round(v * 10)).ToString()); }

        public static FxSet Get(bool tx) { FxSet s = cur[tx ? 0 : 1]; if (s == null) s = Load(tx); return s; }
        public static FxSet Load(bool tx)
        {
            FxSet s = new FxSet(); s.EqOn = Cfg.GetBool(K(tx, "eq"), false); s.CompOn = tx && Cfg.GetBool(K(tx, "comp"), false);
            for (int i = 0; i < 10; i++) s.G[i] = D(tx, "g" + i, 0);
            s.Thr = D(tx, "thr", -18); s.Ratio = D(tx, "ratio", 3); s.Makeup = D(tx, "makeup", 3); s.Ceil = D(tx, "ceil", -1); s.Att = D(tx, "att", 5); s.Rel = D(tx, "rel", 150);
            s.Ver = ++ver; cur[tx ? 0 : 1] = s; return s;
        }
        // enregistre un instantané modifié (Save différé par l'appelant)
        public static void Store(bool tx, FxSet s)
        {
            Cfg.SetBool(K(tx, "eq"), s.EqOn); if (tx) Cfg.SetBool(K(tx, "comp"), s.CompOn);
            for (int i = 0; i < 10; i++) SD(tx, "g" + i, s.G[i]);
            if (tx) { SD(tx, "thr", s.Thr); SD(tx, "ratio", s.Ratio); SD(tx, "makeup", s.Makeup); SD(tx, "ceil", s.Ceil); SD(tx, "att", s.Att); SD(tx, "rel", s.Rel); }
            s.Ver = ++ver; cur[tx ? 0 : 1] = s;
        }
        public static FxSet Clone(FxSet o) { FxSet s = new FxSet(); s.EqOn = o.EqOn; s.CompOn = o.CompOn; Array.Copy(o.G, s.G, 10); s.Thr = o.Thr; s.Ratio = o.Ratio; s.Makeup = o.Makeup; s.Ceil = o.Ceil; s.Att = o.Att; s.Rel = o.Rel; return s; }
        public static bool Active(bool tx) { FxSet s = Get(tx); return s.EqOn || s.CompOn; }

        // coefficients d'un filtre en cloche (RBJ) : b0 b1 b2 a1 a2, normalisés
        public static void Peak(double f, double gdb, double fs, double[] c)
        {
            double A = Math.Pow(10, gdb / 40), w = 2 * Math.PI * f / fs, al = Math.Sin(w) / (2 * Q), cs = Math.Cos(w), a0 = 1 + al / A;
            c[0] = (1 + al * A) / a0; c[1] = -2 * cs / a0; c[2] = (1 - al * A) / a0; c[3] = -2 * cs / a0; c[4] = (1 - al / A) / a0;
        }
        // réponse de l'égaliseur en dB à la fréquence f (courbe de la fenêtre)
        public static double Response(FxSet s, double f, double fs)
        {
            double tot = 0, w = 2 * Math.PI * f / fs; double[] c = new double[5];
            double cw = Math.Cos(w), sw = Math.Sin(w), c2 = Math.Cos(2 * w), s2 = Math.Sin(2 * w);
            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(s.G[i]) < 0.05 || Freq[i] >= 0.45 * fs) continue; Peak(Freq[i], s.G[i], fs, c);
                double nr = c[0] + c[1] * cw + c[2] * c2, ni = -(c[1] * sw + c[2] * s2), dr = 1 + c[3] * cw + c[4] * c2, di = -(c[3] * sw + c[4] * s2);
                tot += 10 * Math.Log10((nr * nr + ni * ni) / (dr * dr + di * di));
            }
            return tot;
        }
    }

    // Chaîne audio d'un flux : égaliseur 10 bandes (stéréo) puis, à l'émission, compresseur et limiteur. Un exemplaire par flux, utilisé par le seul fil audio.
    public class FxChain
    {
        readonly bool tx; int ver = -1, rate; FxSet s;
        readonly double[][] co = new double[10][]; readonly bool[] used = new bool[10];
        readonly double[] zl1 = new double[10], zl2 = new double[10], zr1 = new double[10], zr2 = new double[10];
        double red, lim = 1, ka, kr, kl;
        public FxChain(bool tx) { this.tx = tx; for (int i = 0; i < 10; i++) co[i] = new double[5]; }

        void Update(int fs)
        {
            s = FxCfg.Get(tx); ver = s.Ver; rate = fs;
            for (int i = 0; i < 10; i++)
            {
                used[i] = Math.Abs(s.G[i]) >= 0.05 && FxCfg.Freq[i] < 0.45 * fs;
                if (used[i]) FxCfg.Peak(FxCfg.Freq[i], s.G[i], fs, co[i]);
            }
            ka = 1 - Math.Exp(-1 / (Math.Max(0.5, s.Att) * 0.001 * fs)); kr = 1 - Math.Exp(-1 / (Math.Max(5, s.Rel) * 0.001 * fs)); kl = 1 - Math.Exp(-1 / (0.05 * fs));
        }

        public void Process(int fs, ref double l, ref double r)
        {
            FxSet cs = FxCfg.Get(tx); if (cs.Ver != ver || fs != rate) Update(fs);
            if (s.EqOn)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (!used[i]) continue; double[] c = co[i];
                    double y = c[0] * l + zl1[i]; zl1[i] = c[1] * l - c[3] * y + zl2[i]; zl2[i] = c[2] * l - c[4] * y; l = y;
                    y = c[0] * r + zr1[i]; zr1[i] = c[1] * r - c[3] * y + zr2[i]; zr2[i] = c[2] * r - c[4] * y; r = y;
                }
            }
            if (tx && s.CompOn)
            {
                double pk = Math.Max(Math.Abs(l), Math.Abs(r)), pdb = 20 * Math.Log10(pk + 1e-9), over = pdb - s.Thr, target = over > 0 ? over * (1 - 1 / Math.Max(1, s.Ratio)) : 0;
                red += (target - red) * (target > red ? ka : kr); FxCfg.GrTx = (float)red;
                double g = Math.Pow(10, (s.Makeup - red) / 20); l *= g; r *= g;
                double ceil = Math.Pow(10, s.Ceil / 20), p2 = Math.Max(Math.Abs(l), Math.Abs(r)), need = p2 > ceil ? ceil / p2 : 1;
                if (need < lim) lim = need; else lim += (1 - lim) * kl;
                l *= lim; r *= lim;
            }
        }
    }
}
