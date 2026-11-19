using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HamRadioLink.Journal
{
    // Import d'un fichier ADIF (.adi) dans un journal : ignore les doublons (meme indicatif, meme heure, meme bande).
    public static class AdifImport
    {
        public static void Run(PeerLog log, string file, Action<string> say, out int added, out int dup)
        {
            Text(log, File.ReadAllText(file, Encoding.UTF8), say, Path.GetFileName(file), out added, out dup);
        }
        public static void Text(PeerLog log, string text, out int added, out int dup) { Text(log, text, null, "", out added, out dup); }
        static void Text(PeerLog log, string text, Action<string> say, string file, out int added, out int dup)
        {
            List<Dictionary<string, string>> recs = Adif.Parse(text); added = 0; dup = 0; int k = 0;
            foreach (Dictionary<string, string> r in recs)
            {
                Qso q = Adif.ToQso(r, log); if (!q.ContainsKey("COL_CALL")) continue;
                string dupSql = "COL_CALL=" + MySqlClient.Quote(q.S("COL_CALL")) + " AND COL_TIME_ON=" + MySqlClient.Quote(q.S("COL_TIME_ON")) + (q.ContainsKey("COL_BAND") ? " AND COL_BAND=" + MySqlClient.Quote(q.S("COL_BAND")) : "");
                if (log.Count(dupSql) > 0) { dup++; continue; }
                if (!q.ContainsKey("COL_STATION_CALLSIGN") && App.Cfg.Get("me.call", "").Length > 0) q["COL_STATION_CALLSIGN"] = App.Cfg.Get("me.call", "");
                Country c = Countries.Find(q.S("COL_CALL")); if (c != null) { if (!q.ContainsKey("COL_COUNTRY")) q["COL_COUNTRY"] = c.Name; if (!q.ContainsKey("COL_DXCC")) q["COL_DXCC"] = c.Dxcc.ToString(); if (!q.ContainsKey("COL_CONT")) q["COL_CONT"] = c.Cont; }
                Geo.FillDistance(q);
                log.Insert(q); added++;
                if (say != null && ++k % 500 == 0) say("  " + file + " : " + k + " / " + recs.Count + " QSO");
            }
        }
    }
}
