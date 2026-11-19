using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HamRadioLink.Journal
{
    // Un QSO = une ligne de la table du journal (colonnes COL_*). Valeur null = NULL en base.
    public class Qso : Dictionary<string, string>
    {
        public string Get(string col) { string v; return TryGetValue(col, out v) ? v : null; }
        public string S(string col) { return Get(col) ?? ""; }
        public int Id { get { int i; return int.TryParse(S("COL_PRIMARY_KEY"), out i) ? i : 0; } }
    }

    public class ColInfo { public string Name; public string Type; public bool Numeric; public bool DateTime; }

    // Accès au journal de l'autre logiciel (MariaDB), table table_hrd_contacts_v01, SANS modifier sa structure.
    public class PeerLog
    {
        public const string Table = "table_hrd_contacts_v01";
        public readonly MySqlClient Db;
        public readonly List<ColInfo> Cols = new List<ColInfo>();
        readonly HashSet<string> names = new HashSet<string>();
        public readonly string Schema;

        public PeerLog(MySqlClient db, string schema)
        {
            Db = db; Schema = schema;
            MySqlResult r = db.Query("SHOW COLUMNS FROM " + TableRef());
            foreach (string[] row in r.Rows)
            {
                string t = row[1].ToLowerInvariant();
                Cols.Add(new ColInfo { Name = row[0], Type = t,
                    Numeric = t.StartsWith("int") || t.StartsWith("double") || t.StartsWith("float") || t.StartsWith("bigint") || t.StartsWith("decimal") || t.StartsWith("smallint") || t.StartsWith("tinyint"),
                    DateTime = t.StartsWith("datetime") || t.StartsWith("timestamp") || t == "date" });
                names.Add(row[0]);
            }
            if (!names.Contains("COL_PRIMARY_KEY")) throw new Exception("Cette table n'est pas un journal reconnu (COL_PRIMARY_KEY absent).");
        }

        public string TableRef() { return MySqlClient.Ident(Schema) + "." + MySqlClient.Ident(Table); }
        public bool Has(string col) { return names.Contains(col); }
        public ColInfo Col(string name) { foreach (ColInfo c in Cols) if (c.Name == name) return c; return null; }

        public long Count(string where)
        {
            string s = Db.Scalar("SELECT COUNT(*) FROM " + TableRef() + (string.IsNullOrEmpty(where) ? "" : " WHERE " + where));
            long n; return long.TryParse(s, out n) ? n : 0;
        }

        public List<Qso> Fetch(string where, string orderBy, int offset, int limit)
        {
            string sql = "SELECT * FROM " + TableRef() + (string.IsNullOrEmpty(where) ? "" : " WHERE " + where)
                + " ORDER BY " + (string.IsNullOrEmpty(orderBy) ? "COL_TIME_ON DESC, COL_PRIMARY_KEY DESC" : orderBy)
                + " LIMIT " + limit + " OFFSET " + offset;
            MySqlResult r = Db.Query(sql);
            List<Qso> list = new List<Qso>(r.Rows.Count);
            foreach (string[] row in r.Rows)
            {
                Qso q = new Qso();
                for (int i = 0; i < r.Columns.Length; i++) q[r.Columns[i]] = row[i];
                list.Add(q);
            }
            return list;
        }

        public Qso Get(int id)
        {
            List<Qso> l = Fetch("COL_PRIMARY_KEY=" + id, null, 0, 1);
            return l.Count > 0 ? l[0] : null;
        }

        // Valeurs par défaut comme l'autre logiciel : champs numériques à 0, QSL à N, QSO complet à Y.
        public void ApplyDefaults(Qso q)
        {
            foreach (ColInfo c in Cols)
                if (c.Numeric && c.Name != "COL_PRIMARY_KEY" && !q.ContainsKey(c.Name)) q[c.Name] = "0";
            string[] n = { "COL_QSL_RCVD", "COL_QSL_SENT", "COL_LOTW_QSL_RCVD", "COL_LOTW_QSL_SENT", "COL_EQSL_QSL_RCVD", "COL_EQSL_QSL_SENT" };
            foreach (string k in n) if (Has(k) && !q.ContainsKey(k)) q[k] = "N";
            if (Has("COL_QSO_COMPLETE") && !q.ContainsKey("COL_QSO_COMPLETE")) q["COL_QSO_COMPLETE"] = "Y";
            // l'autre logiciel écrit toujours un numéro DXCC (0 si inconnu) ; les autres textes restent NULL
            if (Has("COL_DXCC") && string.IsNullOrEmpty(q.Get("COL_DXCC"))) q["COL_DXCC"] = "0";
        }

        string ValueSql(string col, string v)
        {
            ColInfo c = Col(col);
            if (v == null) return "NULL";
            if (c != null && c.Numeric)
            {
                double d;
                if (v.Length == 0) return c.Name == "COL_PRIMARY_KEY" ? "NULL" : "0";
                if (double.TryParse(v.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    return d.ToString("R", CultureInfo.InvariantCulture);
                return "0";
            }
            if (c != null && c.DateTime && v.Length == 0) return "NULL";
            // texte trop long pour la colonne varchar(n)/char(n) (ex. credit_granted de 87 caracteres) : on coupe au lieu de refuser le QSO
            if (c != null && c.Type.EndsWith(")") && (c.Type.StartsWith("varchar(") || c.Type.StartsWith("char(")))
            {
                int a = c.Type.IndexOf('('), n;
                if (int.TryParse(c.Type.Substring(a + 1, c.Type.Length - a - 2), out n) && v.Length > n) v = v.Substring(0, n);
            }
            return MySqlClient.Quote(v);
        }

        public int Insert(Qso q)
        {
            ApplyDefaults(q);
            List<string> cols = new List<string>(), vals = new List<string>();
            foreach (KeyValuePair<string, string> kv in q)
            {
                if (kv.Key == "COL_PRIMARY_KEY" || !Has(kv.Key)) continue;
                cols.Add(MySqlClient.Ident(kv.Key)); vals.Add(ValueSql(kv.Key, kv.Value));
            }
            MySqlResult r = Db.Query("INSERT INTO " + TableRef() + " (" + string.Join(",", cols) + ") VALUES (" + string.Join(",", vals) + ")");
            return (int)r.InsertId;
        }

        public void Update(int id, Qso changes)
        {
            List<string> sets = new List<string>();
            foreach (KeyValuePair<string, string> kv in changes)
            {
                if (kv.Key == "COL_PRIMARY_KEY" || !Has(kv.Key)) continue;
                sets.Add(MySqlClient.Ident(kv.Key) + "=" + ValueSql(kv.Key, kv.Value));
            }
            if (sets.Count == 0) return;
            Db.Exec("UPDATE " + TableRef() + " SET " + string.Join(",", sets) + " WHERE COL_PRIMARY_KEY=" + id);
        }

        public void Delete(IEnumerable<int> ids)
        {
            List<string> l = new List<string>(); foreach (int i in ids) l.Add(i.ToString());
            if (l.Count > 0) Db.Exec("DELETE FROM " + TableRef() + " WHERE COL_PRIMARY_KEY IN (" + string.Join(",", l) + ")");
        }

        // Recherche rapide : indicatif, nom, QTH, pays, commentaire
        public static string QuickWhere(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string t = text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            string like = MySqlClient.Quote("%" + t + "%");
            string pre = MySqlClient.Quote(t + "%");
            return "(COL_CALL LIKE " + pre + " OR COL_NAME LIKE " + like + " OR COL_QTH LIKE " + like + " OR COL_COUNTRY LIKE " + like + " OR COL_COMMENT LIKE " + like + " OR COL_GRIDSQUARE LIKE " + pre + ")";
        }

        public List<string> Distinct(string col)
        {
            List<string> l = new List<string>();
            foreach (string[] r in Db.Query("SELECT DISTINCT " + MySqlClient.Ident(col) + " FROM " + TableRef() + " WHERE " + MySqlClient.Ident(col) + " IS NOT NULL AND " + MySqlClient.Ident(col) + "<>'' ORDER BY 1").Rows) l.Add(r[0]);
            return l;
        }
    }
}
