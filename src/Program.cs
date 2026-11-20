using System;
using System.IO;
using System.Windows.Forms;

namespace HamRadioLink
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Cfg.Load(); Theme.Load(); HW.WheelPct = Math.Max(1, Math.Min(10, Cfg.GetInt("wheelpct", 1)));
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e) { Log(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { Log(e.ExceptionObject as Exception); };
            foreach (string a in args) if (a.StartsWith("--dump=") || a.StartsWith("--selftest=")) Journal.WsjtListener.Disabled = true;
            foreach (string a in args) if (a.StartsWith("--dump=") || a.StartsWith("--selftest=") || a.StartsWith("--shot=")) Updater.Disabled = true;
            foreach (string a in args) if (a.StartsWith("--dump=")) { SelfTest.Dump(a.Substring(7)); return; }
            foreach (string a in args) if (a.StartsWith("--selftest=")) { Environment.Exit(SelfTest.Run(a.Substring(11))); return; }
            string radio = null; bool virt = false; string shot = null;
            foreach (string a in args)
            {
                if (a == "--virtual") virt = true;
                else if (a.StartsWith("--radio=")) radio = a.Substring(8);
                else if (a.StartsWith("--shot=")) { shot = a.Substring(7); if (!Array.Exists(args, delegate(string x) { return x == "--nosim"; })) virt = true; }
                else if (a.StartsWith("--data=")) { Cfg.Dir = a.Substring(7); try { Directory.CreateDirectory(Cfg.Dir); } catch { } }
            }
            HW.VeilPct = Math.Max(10, Math.Min(100, Cfg.GetInt("veil.pct", 67))); CatLog.Init(Path.Combine(Cfg.Dir ?? ".", "logs"));
            // Une seule instance : un second lancement rappelle simplement la télécommande de la première (même si seul le journal restait ouvert).
            bool first = true; System.Threading.EventWaitHandle wake = null; System.Threading.Mutex single = null;
            if (shot == null && !virt && radio == null && !Array.Exists(args, delegate(string x) { return x.StartsWith("--data="); }))
            {
                single = new System.Threading.Mutex(true, "HamRadioLink-single-instance", out first);
                wake = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, "HamRadioLink-show-main");
                if (!first) { try { wake.Set(); } catch { } return; }
            }
            MainForm f = new MainForm();
            if (wake != null) { System.Threading.Thread th = new System.Threading.Thread(delegate() { while (true) { wake.WaitOne(); try { f.BeginInvoke((Action)delegate { f.BringBack(); }); } catch { } } }) { IsBackground = true }; th.Start(); }
            if (virt || radio != null) f.Shown += delegate { f.AutoStart(virt ? "virtual" : null, radio); };
            if (shot != null)
            {
                string[] p = shot.Split(','); string path = p[0]; int ms = p.Length > 1 ? int.Parse(p[1]) : 6000; string act = p.Length > 2 ? p[2] : "";
                f.Shown += delegate { f.ShotAndExit(path, ms, act); };
            }
            Application.Run(f);
            GC.KeepAlive(single); GC.KeepAlive(wake);   // le verrou « une seule instance » doit vivre jusqu'à la fin
        }
        static void Log(Exception ex)
        {
            try { File.AppendAllText(Path.Combine(Cfg.Dir ?? ".", "crash.log"), DateTime.Now + "\r\n" + ex + "\r\n\r\n"); } catch { }
        }
    }
}
