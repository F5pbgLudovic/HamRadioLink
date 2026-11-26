using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Sonde DDE (lancée par « HamRadioLink.exe --ddeprobe ») : se déclare serveur DDE « HRD », accepte toutes les conversations
    // et note dans ddeprobe.log (à côté de l'exe) tout ce qu'un client comme HDSDR demande. Ne touche à aucun poste.
    public class DdeProbeForm : Form
    {
        delegate IntPtr DdeCb(uint type, uint fmt, IntPtr hconv, IntPtr hsz1, IntPtr hsz2, IntPtr hdata, IntPtr d1, IntPtr d2);
        [DllImport("user32.dll")] static extern uint DdeInitializeW(ref uint idInst, DdeCb cb, uint flags, uint res);
        [DllImport("user32.dll")] static extern IntPtr DdeCreateStringHandleW(uint idInst, string s, int cp);
        [DllImport("user32.dll")] static extern uint DdeQueryStringW(uint idInst, IntPtr hsz, StringBuilder sb, uint max, int cp);
        [DllImport("user32.dll")] static extern IntPtr DdeNameService(uint idInst, IntPtr hsz1, IntPtr hsz2, uint cmd);
        [DllImport("user32.dll")] static extern IntPtr DdeCreateDataHandle(uint idInst, byte[] data, uint cb, uint off, IntPtr hszItem, uint fmt, uint cmd);
        [DllImport("user32.dll")] static extern IntPtr DdeAccessData(IntPtr hdata, out uint size);
        [DllImport("user32.dll")] static extern bool DdeUnaccessData(IntPtr hdata);
        [DllImport("user32.dll")] static extern bool DdeUninitialize(uint idInst);

        uint inst; DdeCb cb; TextBox box; string logFile; string[] names = { "HRD", "HRDRig", "HRD Rig Control", "HRDRC", "HamRadioDeluxe" };

        public DdeProbeForm()
        {
            Text = "HamRadioLink - sonde DDE « HRD » (pour HDSDR)"; ClientSize = new Size(760, 420);
            box = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 9) }; Controls.Add(box);
            logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ddeprobe.log");
            cb = Callback;
            if (DdeInitializeW(ref inst, cb, 0, 0) != 0) { Log("DdeInitialize a échoué"); return; }
            foreach (string n in names) { IntPtr h = DdeCreateStringHandleW(inst, n, 1004); DdeNameService(inst, h, IntPtr.Zero, 1); }
            DdeNameService(inst, IntPtr.Zero, IntPtr.Zero, 8); Log("Serveur DDE déclaré sous les noms : " + string.Join(", ", names) + ". Lancez HDSDR (sync HRD DDE) puis regardez ici / ddeprobe.log.");
        }
        protected override void OnFormClosed(FormClosedEventArgs e) { try { DdeUninitialize(inst); } catch { } base.OnFormClosed(e); }

        void Log(string s) { string l = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + s; box.AppendText(l + "\r\n"); try { File.AppendAllText(logFile, l + "\r\n"); } catch { } }
        string Q(IntPtr h) { if (h == IntPtr.Zero) return ""; StringBuilder sb = new StringBuilder(260); DdeQueryStringW(inst, h, sb, 260, 1004); return sb.ToString(); }

        IntPtr Callback(uint type, uint fmt, IntPtr hconv, IntPtr hsz1, IntPtr hsz2, IntPtr hdata, IntPtr d1, IntPtr d2)
        {
            try
            {
                switch (type)
                {
                    case 0x1062: Log("CONNECT topic=\"" + Q(hsz1) + "\" service=\"" + Q(hsz2) + "\""); return (IntPtr)1;
                    case 0x20E2: Log("WILDCONNECT topic=\"" + Q(hsz1) + "\" service=\"" + Q(hsz2) + "\""); return IntPtr.Zero;
                    case 0x8072: Log("CONNECT_CONFIRM topic=\"" + Q(hsz1) + "\" service=\"" + Q(hsz2) + "\""); return IntPtr.Zero;
                    case 0x80C2: Log("DISCONNECT"); return IntPtr.Zero;
                    case 0x20B0: Log("REQUEST topic=\"" + Q(hsz1) + "\" item=\"" + Q(hsz2) + "\" fmt=" + fmt); return DdeCreateDataHandle(inst, new byte[] { 0 }, 1, 0, hsz2, 1, 0);
                    case 0x1030: Log("ADVSTART topic=\"" + Q(hsz1) + "\" item=\"" + Q(hsz2) + "\""); return (IntPtr)1;
                    case 0x00C0: Log("ADVSTOP topic=\"" + Q(hsz1) + "\" item=\"" + Q(hsz2) + "\""); return IntPtr.Zero;
                    case 0x4090: Log("POKE topic=\"" + Q(hsz1) + "\" item=\"" + Q(hsz2) + "\" data=" + Data(hdata)); return (IntPtr)0x8000;
                    case 0x4050: Log("EXECUTE topic=\"" + Q(hsz1) + "\" commande=" + Data(hdata)); return (IntPtr)0x8000;
                    case 0x80A0: case 0x80D0: return IntPtr.Zero; // REGISTER / UNREGISTER
                    default: Log("type 0x" + type.ToString("X") + " topic=\"" + Q(hsz1) + "\" item=\"" + Q(hsz2) + "\""); return IntPtr.Zero;
                }
            }
            catch (Exception ex) { Log("erreur : " + ex.Message); return IntPtr.Zero; }
        }
        string Data(IntPtr h)
        {
            if (h == IntPtr.Zero) return "(vide)"; uint n; IntPtr p = DdeAccessData(h, out n); if (p == IntPtr.Zero) return "(illisible)";
            byte[] b = new byte[Math.Min(n, 512)]; Marshal.Copy(p, b, 0, b.Length); DdeUnaccessData(h);
            bool u16 = b.Length > 2 && b[1] == 0 && b[0] != 0; string t = u16 ? Encoding.Unicode.GetString(b) : Encoding.Default.GetString(b);
            return "\"" + t.TrimEnd('\0') + "\" [" + BitConverter.ToString(b, 0, Math.Min(b.Length, 48)) + "]";
        }
    }
}


