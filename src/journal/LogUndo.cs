using System;
using System.Collections.Generic;

namespace HamRadioLink.Journal
{
    // Annuler / Refaire (comme Ctrl+Z / Ctrl+Y) pour les contacts : ajout, modification, suppression, changement de bande, modification en bloc.
    // Chaque opération garde l'état complet des lignes concernées avant et après (null = la ligne n'existe pas).
    public static class LogUndo
    {
        class Entry { public string Name; public PeerLog Log; public Dictionary<int, Qso> Before, After; }
        static readonly List<Entry> undo = new List<Entry>(), redo = new List<Entry>();
        public static event Action Changed;

        public static Dictionary<int, Qso> Snap(PeerLog log, IEnumerable<int> ids)
        {
            Dictionary<int, Qso> d = new Dictionary<int, Qso>(); List<int> all = new List<int>(ids); foreach (int i in all) d[i] = null;
            for (int i = 0; i < all.Count; i += 500)
            {
                List<string> l = new List<string>(); for (int j = i; j < Math.Min(i + 500, all.Count); j++) l.Add(all[j].ToString());
                foreach (Qso q in log.Fetch("COL_PRIMARY_KEY IN (" + string.Join(",", l.ToArray()) + ")", "COL_PRIMARY_KEY", 0, 500)) d[q.Id] = q;
            }
            return d;
        }
        // à appeler une fois l'opération faite : before = état pris avant, ids = lignes touchées
        public static void Record(string name, PeerLog log, Dictionary<int, Qso> before, IEnumerable<int> ids)
        {
            try
            {
                Entry e = new Entry { Name = name, Log = log, Before = before, After = Snap(log, ids) };
                undo.Add(e); if (undo.Count > 50) undo.RemoveAt(0); redo.Clear(); Fire();
            }
            catch { }
        }
        public static bool CanUndo { get { return undo.Count > 0; } }
        public static bool CanRedo { get { return redo.Count > 0; } }
        public static string UndoName { get { return undo.Count > 0 ? undo[undo.Count - 1].Name : null; } }
        public static string RedoName { get { return redo.Count > 0 ? redo[redo.Count - 1].Name : null; } }

        // renvoie le nom de l'opération défaite / refaite (null s'il n'y en a pas)
        public static string Undo() { return Step(undo, redo, true); }
        public static string Redo() { return Step(redo, undo, false); }
        static string Step(List<Entry> from, List<Entry> to, bool back)
        {
            if (from.Count == 0) return null; Entry e = from[from.Count - 1];
            Apply(e.Log, back ? e.Before : e.After); from.RemoveAt(from.Count - 1); to.Add(e); Fire(); return e.Name;
        }
        static void Apply(PeerLog log, Dictionary<int, Qso> state)
        {
            List<int> del = new List<int>();
            foreach (KeyValuePair<int, Qso> kv in state)
            {
                if (kv.Value == null) { del.Add(kv.Key); continue; }
                Qso cur = log.Get(kv.Key);
                if (cur != null) log.Update(kv.Key, kv.Value); else log.Restore(kv.Value);
            }
            if (del.Count > 0) log.Delete(del);
        }
        static void Fire() { Action a = Changed; if (a != null) a(); }
    }
}
