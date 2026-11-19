using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HamRadioLink
{
    // com0com (paires de ports COM virtuels reliés entre eux, licence GPL, https://sourceforge.net/projects/com0com/) :
    // détection, liste des paires libres, création d'une paire, et installation silencieuse à partir de l'installateur signé
    // embarqué dans HamRadioLink (vendor\com0com). Windows demande seulement l'autorisation d'administrateur.
    public static class Com0Com
    {
        public class Pair
        {
            public int N; public string A, B; public int Status; public string Owner;      // Status : 0 libre, 1 tenue par CE poste, 2 occupée par un autre programme, 3 tenue par un AUTRE poste de HamRadioLink
            public override string ToString() { return A + "  \u2194  " + B + (Status == 1 ? "    — tenue par ce poste (partage en cours)" : (Status == 3 ? "    — utilisée par un autre poste (" + Owner + ")" : (Status == 2 ? "    — occupée par un autre programme" : ""))); }
        }
        // toutes les paires COMn / COMm présentes, avec leur état (aucune n'est cachée) ; radioPorts = ports COM des postes (jamais proposés)
        public static List<Pair> AllComPairs(string[] radioPorts, IDictionary<string, string> held, string me)
        {
            List<Pair> l = new List<Pair>(); HashSet<string> present = new HashSet<string>(PortsNow(), StringComparer.OrdinalIgnoreCase);
            foreach (Pair p in Pairs())
            {
                if (!IsComName(p.A) || !IsComName(p.B) || !present.Contains(p.A) || !present.Contains(p.B)) continue;
                bool radio = false; if (radioPorts != null) foreach (string r in radioPorts) if (r != null && (r.Equals(p.A, StringComparison.OrdinalIgnoreCase) || r.Equals(p.B, StringComparison.OrdinalIgnoreCase))) radio = true; if (radio) continue;
                string o; lock (held) { if (!held.TryGetValue(p.A, out o) && !held.TryGetValue(p.B, out o)) o = null; }
                if (o != null) { p.Status = o == me ? 1 : 3; p.Owner = o; } else if (!CanOpen(p.A)) p.Status = 2;
                l.Add(p);
            }
            return l;
        }
        const string Hash64 = "26486b28604b49a9008c54feb11b9ece0008a8287ee5caf0bcf2a62f4317128f", Hash86 = "de917425e022839f6374b509e3d34f3f9e9773915a44996194442337eadf4a5a";

        public static string SetupC()
        {
            foreach (string pf in new string[] { Environment.GetEnvironmentVariable("ProgramFiles(x86)"), Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramW6432") })
            { if (string.IsNullOrEmpty(pf)) continue; string p = Path.Combine(pf, "com0com\\setupc.exe"); if (File.Exists(p)) return p; }
            return null;
        }
        public static bool Installed { get { return SetupC() != null; } }

        // paires déclarées dans le registre (lecture seule, sans droits d'administrateur)
        public static List<Pair> Pairs()
        {
            List<Pair> l = new List<Pair>();
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\COM0COM\PORT"))
                {
                    if (k == null) return l;
                    foreach (string n in k.GetSubKeyNames())
                    {
                        Match m = Regex.Match(n, @"^CNCA(\d+)$", RegexOptions.IgnoreCase); if (!m.Success) continue;
                        string a = PortName(k, n), b = PortName(k, "CNCB" + m.Groups[1].Value);
                        if (a != null && b != null) { Pair p = new Pair(); p.N = int.Parse(m.Groups[1].Value); p.A = a; p.B = b; l.Add(p); }
                    }
                }
            }
            catch { }
            l.Sort(delegate(Pair x, Pair y) { return x.N.CompareTo(y.N); }); return l;
        }
        static string PortName(RegistryKey k, string sub)
        {
            try { using (RegistryKey d = k.OpenSubKey(sub + @"\Device Parameters")) { return d == null ? null : d.GetValue("PortName") as string; } } catch { return null; }
        }
        public static bool IsComName(string s) { return s != null && Regex.IsMatch(s, @"^COM\d+$", RegexOptions.IgnoreCase); }

        // Paires utilisables : deux vrais noms COMn (les paires COMn / HUBn de hub4com ou CNCxn ne le sont pas), présentes dans Windows,
        // sans rapport avec le port du poste, et dont l'extrémité prise par HamRadioLink n'est pas déjà ouverte par un autre programme.
        public static List<Pair> Usable(string radioPort, bool testOpen)
        {
            List<Pair> l = new List<Pair>(); HashSet<string> present = new HashSet<string>(PortsNow(), StringComparer.OrdinalIgnoreCase);
            foreach (Pair p in Pairs())
            {
                if (!IsComName(p.A) || !IsComName(p.B) || !present.Contains(p.A) || !present.Contains(p.B)) continue;
                if (radioPort != null && (radioPort.Equals(p.A, StringComparison.OrdinalIgnoreCase) || radioPort.Equals(p.B, StringComparison.OrdinalIgnoreCase))) continue;
                if (testOpen && !CanOpen(p.A)) continue;
                l.Add(p);
            }
            return l;
        }
        public static string[] PortsNow() { try { return SerialPort.GetPortNames(); } catch { return new string[0]; } }
        // essai d'ouverture d'une extrémité d'une paire virtuelle (jamais le port du poste)
        static bool CanOpen(string port) { try { using (SerialPort s = new SerialPort(port)) { s.Open(); s.Close(); } return true; } catch { return false; } }

        // deux numéros de port consécutifs libres (ni dans Windows, ni dans une paire com0com)
        public static int[] FreeNumbers()
        {
            HashSet<int> used = new HashSet<int>(); foreach (string s in PortsNow()) { Match m = Regex.Match(s, @"^COM(\d+)$", RegexOptions.IgnoreCase); if (m.Success) used.Add(int.Parse(m.Groups[1].Value)); }
            foreach (Pair p in Pairs()) foreach (string s in new string[] { p.A, p.B }) { Match m = Regex.Match(s, @"(\d+)$"); if (m.Success) used.Add(int.Parse(m.Groups[1].Value)); }
            for (int n = 20; n < 250; n++) if (!used.Contains(n) && !used.Contains(n + 1)) return new int[] { n, n + 1 };
            return new int[] { 100, 101 };
        }

        // Installation silencieuse (installateur signé embarqué). Windows demande l'autorisation d'administrateur.
        public static bool Install(out string err)
        {
            err = null;
            try
            {
                bool x64 = Environment.Is64BitOperatingSystem; string res = x64 ? "com0com-x64.exe" : "com0com-x86.exe", want = x64 ? Hash64 : Hash86;
                string dir = Path.Combine(Path.GetTempPath(), "HamRadioLink-com0com"); Directory.CreateDirectory(dir); string exe = Path.Combine(dir, res);
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
                {
                    if (s == null) { err = "installateur com0com absent de cette version"; return false; }
                    using (FileStream f = File.Create(exe)) s.CopyTo(f);
                }
                string h; using (SHA256 sha = SHA256.Create()) using (FileStream f = File.OpenRead(exe)) h = BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "").ToLowerInvariant();
                if (h != want) { err = "installateur com0com altéré (empreinte différente)"; try { File.Delete(exe); } catch { } return false; }
                CatLog.Event("com0com", "installation de com0com demandée");
                if (!RunAdmin(exe, "/S", dir, 300000, out err)) { CatLog.Event("com0com", "installation refusée : " + err); return false; }
                for (int i = 0; i < 40 && !Installed; i++) System.Threading.Thread.Sleep(500);
                if (!Installed) { err = "l'installation n'a pas abouti (setupc.exe introuvable)"; return false; }
                return true;
            }
            catch (Exception ex) { err = ex.Message; return false; }
        }
        // crée la paire a ↔ b (noms COMn)
        public static bool CreatePair(string a, string b, out string err)
        {
            err = null; string sc = SetupC(); if (sc == null) { err = "com0com n'est pas installé"; return false; }
            if (!IsComName(a) || !IsComName(b) || a.Equals(b, StringComparison.OrdinalIgnoreCase)) { err = "noms de ports invalides"; return false; }
            CatLog.Event("com0com", "création demandée de la paire " + a + " <-> " + b);
            if (!RunAdmin(sc, "--silent install PortName=" + a + ",EmuBR=yes PortName=" + b + ",EmuBR=yes", Path.GetDirectoryName(sc), 60000, out err)) { CatLog.Event("com0com", "création refusée : " + err); return false; }
            for (int i = 0; i < 50; i++)
            {
                foreach (Pair p in Pairs()) if (p.A.Equals(a, StringComparison.OrdinalIgnoreCase) && p.B.Equals(b, StringComparison.OrdinalIgnoreCase)) { for (int j = 0; j < 20; j++) { bool ok = false; foreach (string s in PortsNow()) if (s.Equals(a, StringComparison.OrdinalIgnoreCase)) ok = true; if (ok) return true; System.Threading.Thread.Sleep(300); } return true; }
                System.Threading.Thread.Sleep(500);
            }
            err = "la paire n'est pas apparue (autorisation refusée ou pilote non confirmé ?)"; return false;
        }
        // supprime la paire n (CNCAn / CNCBn) : seulement une paire que la fenêtre a listée
        public static bool RemovePair(Pair p, out string err)
        {
            err = null; string sc = SetupC(); if (sc == null) { err = "com0com n'est pas installé"; return false; }
            CatLog.Event("com0com", "suppression demandée de la paire " + p.A + " <-> " + p.B + " (n° " + p.N + ")");
            if (!RunAdmin(sc, "--silent remove " + p.N, Path.GetDirectoryName(sc), 60000, out err)) { CatLog.Event("com0com", "suppression refusée : " + err); return false; }
            for (int i = 0; i < 50; i++)
            {
                bool still = false; foreach (Pair q in Pairs()) if (q.N == p.N) still = true;
                if (!still) return true; System.Threading.Thread.Sleep(500);
            }
            err = "la paire est toujours présente (autorisation refusée, ou port encore utilisé par un logiciel ?)"; return false;
        }
        static bool RunAdmin(string exe, string args, string workDir, int timeoutMs, out string err)
        {
            err = null;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args); psi.Verb = "runas"; psi.UseShellExecute = true; psi.WorkingDirectory = workDir; psi.WindowStyle = ProcessWindowStyle.Hidden;
                using (Process p = Process.Start(psi)) { if (p == null) { err = "démarrage impossible"; return false; } if (!p.WaitForExit(timeoutMs)) { err = "délai dépassé"; return false; } }
                return true;
            }
            catch (Win32Exception ex) { err = ex.NativeErrorCode == 1223 ? "autorisation administrateur refusée" : ex.Message; return false; }
            catch (Exception ex) { err = ex.Message; return false; }
        }
    }
}
