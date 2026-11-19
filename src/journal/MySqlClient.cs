using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace HamRadioLink.Journal
{
    // Client MySQL / MariaDB minimal (protocole texte, authentification mysql_native_password).
    // Intégré à l'exe pour que le dossier reste portable, sans DLL à côté.
    public class MySqlResult
    {
        public string[] Columns = new string[0];
        public List<string[]> Rows = new List<string[]>();
        public long Affected;
        public long InsertId;
    }

    public class MySqlException : Exception
    {
        public int Code;
        public MySqlException(int code, string msg) : base(msg) { Code = code; }
    }

    public class MySqlClient : IDisposable
    {
        TcpClient tcp;
        NetworkStream ns;
        byte seq;
        readonly object gate = new object();
        public string Host = "127.0.0.1", User = "root", Password = "", Database = "";
        public int Port = 3306;
        public string ServerVersion = "";
        public bool Connected { get { return tcp != null && tcp.Connected; } }

        public MySqlClient(string host, int port, string user, string password, string database)
        {
            Host = host; Port = port; User = user; Password = password; Database = database ?? "";
        }

        public void Open()
        {
            lock (gate)
            {
                CloseSocket();
                tcp = new TcpClient();
                tcp.NoDelay = true;
                IAsyncResult ar = tcp.BeginConnect(Host, Port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(6000)) { CloseSocket(); throw new MySqlException(0, "Pas de réponse de " + Host + ":" + Port); }
                tcp.EndConnect(ar);
                tcp.ReceiveTimeout = 120000;
                tcp.SendTimeout = 30000;
                ns = tcp.GetStream();
                Handshake();
            }
        }

        public void Dispose() { lock (gate) { try { if (ns != null) { seq = 0; WritePacket(new byte[] { 1 }); } } catch { } CloseSocket(); } }

        void CloseSocket()
        {
            try { if (tcp != null) tcp.Close(); } catch { }
            tcp = null; ns = null;
        }

        // ---------- paquets ----------
        byte[] ReadPacket()
        {
            MemoryStream all = new MemoryStream();
            while (true)
            {
                byte[] h = ReadN(4);
                int len = h[0] | (h[1] << 8) | (h[2] << 16);
                seq = (byte)(h[3] + 1);
                if (len > 0) all.Write(ReadN(len), 0, len);
                if (len < 0xFFFFFF) break;
            }
            return all.ToArray();
        }

        byte[] ReadN(int n)
        {
            byte[] b = new byte[n]; int got = 0;
            while (got < n)
            {
                int r = ns.Read(b, got, n - got);
                if (r <= 0) throw new IOException("Connexion MariaDB fermée");
                got += r;
            }
            return b;
        }

        void WritePacket(byte[] payload)
        {
            int off = 0;
            do
            {
                int len = Math.Min(payload.Length - off, 0xFFFFFF);
                byte[] pk = new byte[4 + len];
                pk[0] = (byte)len; pk[1] = (byte)(len >> 8); pk[2] = (byte)(len >> 16); pk[3] = seq++;
                Buffer.BlockCopy(payload, off, pk, 4, len);
                ns.Write(pk, 0, pk.Length);
                off += len;
                if (len == 0xFFFFFF && off == payload.Length) { byte[] e = { 0, 0, 0, seq++ }; ns.Write(e, 0, 4); }
            } while (off < payload.Length);
        }

        static byte[] Scramble(string password, byte[] salt)
        {
            if (string.IsNullOrEmpty(password)) return new byte[0];
            using (SHA1 sha = SHA1.Create())
            {
                byte[] p1 = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] p2 = sha.ComputeHash(p1);
                byte[] cat = new byte[salt.Length + p2.Length];
                Buffer.BlockCopy(salt, 0, cat, 0, salt.Length);
                Buffer.BlockCopy(p2, 0, cat, salt.Length, p2.Length);
                byte[] p3 = sha.ComputeHash(cat);
                for (int i = 0; i < p1.Length; i++) p1[i] ^= p3[i];
                return p1;
            }
        }

        void Handshake()
        {
            seq = 0;
            byte[] hs = ReadPacket();
            if (hs[0] == 0xFF) throw ParseError(hs);
            int p = 1;
            int z = Array.IndexOf(hs, (byte)0, p);
            ServerVersion = Encoding.ASCII.GetString(hs, p, z - p);
            p = z + 1 + 4;
            byte[] salt1 = new byte[8]; Buffer.BlockCopy(hs, p, salt1, 0, 8); p += 8 + 1;
            p += 2 + 1 + 2 + 2;                  // capacités basses, charset, statut, capacités hautes
            int authLen = hs[p]; p += 1 + 10;
            int len2 = Math.Max(13, authLen - 8);
            byte[] salt2 = new byte[len2]; Buffer.BlockCopy(hs, p, salt2, 0, Math.Min(len2, hs.Length - p));
            byte[] salt = new byte[20]; Buffer.BlockCopy(salt1, 0, salt, 0, 8); Buffer.BlockCopy(salt2, 0, salt, 8, 12);

            uint caps = 0x1 | 0x4 | 0x8 | 0x200 | 0x2000 | 0x8000 | 0x80000;
            if (Database.Length == 0) caps &= ~0x8u;
            MemoryStream m = new MemoryStream();
            WriteU32(m, caps); WriteU32(m, 0x1000000); m.WriteByte(45);
            m.Write(new byte[23], 0, 23);
            WriteZ(m, User);
            byte[] auth = Scramble(Password, salt);
            m.WriteByte((byte)auth.Length); m.Write(auth, 0, auth.Length);
            if (Database.Length > 0) WriteZ(m, Database);
            WriteZ(m, "mysql_native_password");
            WritePacket(m.ToArray());

            byte[] r = ReadPacket();
            if (r[0] == 0xFE)   // changement de méthode d'authentification
            {
                int e = Array.IndexOf(r, (byte)0, 1);
                string plugin = Encoding.ASCII.GetString(r, 1, e - 1);
                if (plugin != "mysql_native_password") throw new MySqlException(0, "Méthode d'authentification non prise en charge : " + plugin);
                byte[] ns2 = new byte[20]; Buffer.BlockCopy(r, e + 1, ns2, 0, 20);
                WritePacket(Scramble(Password, ns2));
                r = ReadPacket();
            }
            if (r[0] == 0xFF) throw ParseError(r);
            // SQL tolérant comme l'autre logiciel (ODBC) : pas de mode strict qui refuserait les valeurs de l'autre logiciel
            Exec("SET NAMES utf8mb4");
        }

        static void WriteU32(Stream s, uint v) { s.WriteByte((byte)v); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 24)); }
        static void WriteZ(Stream s, string t) { byte[] b = Encoding.UTF8.GetBytes(t); s.Write(b, 0, b.Length); s.WriteByte(0); }

        static MySqlException ParseError(byte[] p)
        {
            int code = p[1] | (p[2] << 8);
            int off = (p.Length > 3 && p[3] == '#') ? 9 : 3;
            return new MySqlException(code, Encoding.UTF8.GetString(p, off, p.Length - off));
        }

        static long LenEnc(byte[] b, ref int p, out bool isNull)
        {
            isNull = false;
            int f = b[p++];
            if (f < 0xFB) return f;
            if (f == 0xFB) { isNull = true; return 0; }
            if (f == 0xFC) { long v = b[p] | (b[p + 1] << 8); p += 2; return v; }
            if (f == 0xFD) { long v = b[p] | (b[p + 1] << 8) | (b[p + 2] << 16); p += 3; return v; }
            long x = 0; for (int i = 7; i >= 0; i--) x = (x << 8) | b[p + i]; p += 8; return x;
        }

        static string LenEncStr(byte[] b, ref int p)
        {
            bool n; long l = LenEnc(b, ref p, out n);
            if (n) return null;
            string s = Encoding.UTF8.GetString(b, p, (int)l); p += (int)l; return s;
        }

        // ---------- requêtes ----------
        public MySqlResult Query(string sql)
        {
            lock (gate)
            {
                try { return QueryInner(sql); }
                catch (IOException) { Open(); return QueryInner(sql); }
                catch (SocketException) { Open(); return QueryInner(sql); }
                catch (NullReferenceException) { Open(); return QueryInner(sql); }
            }
        }

        public long Exec(string sql) { return Query(sql).Affected; }

        public string Scalar(string sql)
        {
            MySqlResult r = Query(sql);
            return r.Rows.Count > 0 && r.Rows[0].Length > 0 ? r.Rows[0][0] : null;
        }

        MySqlResult QueryInner(string sql)
        {
            if (ns == null) throw new IOException("Non connecté");
            seq = 0;
            byte[] q = Encoding.UTF8.GetBytes(sql);
            byte[] pk = new byte[q.Length + 1]; pk[0] = 3; Buffer.BlockCopy(q, 0, pk, 1, q.Length);
            WritePacket(pk);
            byte[] r = ReadPacket();
            MySqlResult res = new MySqlResult();
            if (r[0] == 0xFF) throw ParseError(r);
            if (r[0] == 0x00)
            {
                int p = 1; bool n;
                res.Affected = LenEnc(r, ref p, out n);
                res.InsertId = LenEnc(r, ref p, out n);
                return res;
            }
            int pp = 0; bool nn;
            int cols = (int)LenEnc(r, ref pp, out nn);
            res.Columns = new string[cols];
            for (int i = 0; i < cols; i++)
            {
                byte[] c = ReadPacket(); int p = 0;
                LenEncStr(c, ref p); LenEncStr(c, ref p); LenEncStr(c, ref p); LenEncStr(c, ref p);
                res.Columns[i] = LenEncStr(c, ref p);
            }
            ReadPacket();   // EOF après les colonnes
            while (true)
            {
                byte[] row = ReadPacket();
                if (row[0] == 0xFE && row.Length < 9) break;
                if (row[0] == 0xFF) throw ParseError(row);
                string[] v = new string[cols]; int p = 0;
                for (int i = 0; i < cols; i++) v[i] = LenEncStr(row, ref p);
                res.Rows.Add(v);
            }
            return res;
        }

        // ---------- aides SQL ----------
        public static string Quote(string s)
        {
            if (s == null) return "NULL";
            StringBuilder b = new StringBuilder(s.Length + 2);
            b.Append('\'');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\0': b.Append("\\0"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\'': b.Append("\\'"); break;
                    case '\x1a': b.Append("\\Z"); break;
                    default: b.Append(c); break;
                }
            }
            b.Append('\'');
            return b.ToString();
        }

        public static string Ident(string s) { return "`" + s.Replace("`", "``") + "`"; }
    }
}
