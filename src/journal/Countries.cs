using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HamRadioLink.Journal
{
    public class Country
    {
        public int Dxcc; public string Prefix, Name, Cont; public double Lat, Lon; public int Cq, Itu; public bool Deleted; public List<string> Prefixes = new List<string>();
    }

    // Table des pays DXCC : fichier Countries.txt (dans data\journal, copié de l'autre logiciel de journal s'il est installé) ; préfixe le plus long gagne.
    public static class Countries
    {
        public static List<Country> All = new List<Country>();
        static Dictionary<string, Country> byPrefix = new Dictionary<string, Country>();
        static Dictionary<string, Country> byExact = new Dictionary<string, Country>();
        static int maxLen;

        public static bool Loaded { get { return All.Count > 0; } }

        public static void Load()
        {
            All.Clear(); byPrefix.Clear(); byExact.Clear(); maxLen = 0;
            string f = Path.Combine(App.DataDir, "Countries.txt");
            if (!File.Exists(f)) { string s = PeerProfile.CountriesFile(); if (s != null) { try { File.Copy(s, f); } catch { } } }   // première utilisation : table des pays de l'autre logiciel de journal, s'il est installé
            if (!File.Exists(f)) return;
            foreach (string raw in File.ReadAllLines(f, Encoding.Default))
            {
                if (raw.StartsWith("#") || raw.Trim().Length == 0) continue;
                string[] p = raw.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (p.Length < 11) continue;
                for (int i = 0; i < p.Length; i++) p[i] = p[i].Trim('"');
                Country c = new Country { Prefix = p[1], Name = p[2], Cont = p[3], Lat = Coord(p[4]), Lon = Coord(p[5]), Deleted = p[8] == "D" };
                int.TryParse(p[0], out c.Dxcc); int.TryParse(p[6], out c.Cq); int.TryParse(p[7], out c.Itu);
                foreach (string pr in p[10].Split(','))
                {
                    string x = pr.Trim(); int g = x.IndexOf(' '); if (g > 0) { if (x.IndexOf('>') > 0) continue; x = x.Substring(0, g); }   // « KL > date » = ancien préfixe : ignoré
                    if (x.Length > 0) c.Prefixes.Add(x);
                }
                All.Add(c);
                foreach (string x in c.Prefixes)
                {
                    if (c.Deleted && byPrefix.ContainsKey(x)) continue;
                    byPrefix[x] = c; if (x.Length > maxLen) maxLen = x.Length;
                }
            }
        }

        // « 49.54N » / « 97.06W » (degrés.minutes) -> degrés décimaux, ouest négatif
        static double Coord(string s)
        {
            if (s.Length < 2) return 0;
            char h = s[s.Length - 1]; string n = s.Substring(0, s.Length - 1); double v;
            if (!double.TryParse(n, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return 0;
            double deg = Math.Floor(v), min = (v - deg) * 100; v = deg + min / 60;
            return (h == 'S' || h == 'W') ? -v : v;
        }

        public static Country Find(string call)
        {
            if (string.IsNullOrEmpty(call)) return null;
            call = call.ToUpperInvariant().Trim();
            // portable : A/B -> on prend la partie qui ressemble au préfixe (la plus courte, ou celle avant /)
            string main = call; int sl = call.IndexOf('/');
            if (sl > 0)
            {
                string a = call.Substring(0, sl), b = call.Substring(sl + 1);
                if (b == "P" || b == "M" || b == "MM" || b == "AM" || b == "QRP" || b == "A" || (b.Length == 1 && char.IsDigit(b[0]))) main = a;
                else main = a.Length <= b.Length ? a : b;
            }
            for (int l = Math.Min(maxLen, main.Length); l >= 1; l--)
            {
                Country c; if (byPrefix.TryGetValue(main.Substring(0, l), out c)) return c;
            }
            return null;
        }

        // Préfixe WPX : lettres + chiffres jusqu'au dernier chiffre de la première partie (SP3VZY -> SP3, F5PBG -> F5)
        public static string Wpx(string call)
        {
            if (string.IsNullOrEmpty(call)) return "";
            call = call.ToUpperInvariant(); int sl = call.IndexOf('/');
            if (sl > 0) { string a = call.Substring(0, sl), b = call.Substring(sl + 1); if (b.Length == 1 && char.IsDigit(b[0])) return a.TrimEnd("0123456789".ToCharArray()) + b; if (b == "P" || b == "M" || b == "MM" || b == "AM" || b == "QRP" || b == "A") call = a; else call = a.Length <= b.Length ? a : b; }
            int last = -1; for (int i = 0; i < call.Length; i++) if (char.IsDigit(call[i])) last = i;
            return last >= 0 ? call.Substring(0, last + 1) : call.Substring(0, Math.Min(2, call.Length)) + "0";
        }
    }

    // Localisateur Maidenhead, distance et azimut
    public static class Geo
    {
        public static bool GridToLatLon(string g, out double lat, out double lon)
        {
            lat = lon = 0; if (g == null) return false; g = g.Trim().ToUpperInvariant();
            if (g.Length < 4 || g.Length % 2 != 0) return false;
            if (g[0] < 'A' || g[0] > 'R' || g[1] < 'A' || g[1] > 'R' || !char.IsDigit(g[2]) || !char.IsDigit(g[3])) return false;
            lon = (g[0] - 'A') * 20 - 180 + (g[2] - '0') * 2; lat = (g[1] - 'A') * 10 - 90 + (g[3] - '0');
            if (g.Length >= 6) { lon += (g[4] - 'A') / 12.0; lat += (g[5] - 'A') / 24.0; lon += 1 / 24.0; lat += 1 / 48.0; }
            else { lon += 1; lat += 0.5; }
            return true;
        }

        public static string LatLonToGrid(double lat, double lon)
        {
            lon += 180; lat += 90;
            StringBuilder s = new StringBuilder();
            s.Append((char)('A' + (int)(lon / 20))).Append((char)('A' + (int)(lat / 10)));
            s.Append((int)((lon % 20) / 2)).Append((int)(lat % 10));
            s.Append((char)('a' + (int)((lon % 2) * 12))).Append((char)('a' + (int)((lat % 1) * 24)));
            return s.ToString().ToUpperInvariant();
        }

        public static double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            double r = 6371, p1 = lat1 * Math.PI / 180, p2 = lat2 * Math.PI / 180, dp = p2 - p1, dl = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dp / 2) * Math.Sin(dp / 2) + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
            return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        public static double Bearing(double lat1, double lon1, double lat2, double lon2)
        {
            double p1 = lat1 * Math.PI / 180, p2 = lat2 * Math.PI / 180, dl = (lon2 - lon1) * Math.PI / 180;
            double y = Math.Sin(dl) * Math.Cos(p2), x = Math.Cos(p1) * Math.Sin(p2) - Math.Sin(p1) * Math.Cos(p2) * Math.Cos(dl);
            return (Math.Atan2(y, x) * 180 / Math.PI + 360) % 360;
        }

        public static bool Valid(string g) { double a, b; return GridToLatLon(g, out a, out b); }

        // Renseigne COL_DISTANCE (km) si absente et si mon locator et celui du contact sont connus. Vrai si calculée.
        public static bool FillDistance(Qso q)
        {
            double d; if (double.TryParse(q.S("COL_DISTANCE"), NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d > 0) return false;
            double la1, lo1, la2, lo2;
            if (!MyPos(out la1, out lo1) || !GridToLatLon(q.S("COL_GRIDSQUARE"), out la2, out lo2)) return false;
            q["COL_DISTANCE"] = Math.Round(Distance(la1, lo1, la2, lo2)).ToString(CultureInfo.InvariantCulture); return true;
        }

        public static bool MyPos(out double lat, out double lon)
        {
            if (GridToLatLon(App.Cfg.Get("me.grid", ""), out lat, out lon)) return true;
            return false;
        }
    }
}

