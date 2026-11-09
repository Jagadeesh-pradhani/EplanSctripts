using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Scripting;

namespace PropDump
{
    public class PropDumper
    {
        // Widen this range if the diff shows nothing useful
        const int StartId = 20000;
        const int EndId   = 20700;

        static string Docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static string BeforeFile = Path.Combine(Docs, "EPLAN_dump_before.txt");
        static string AfterFile  = Path.Combine(Docs, "EPLAN_dump_after.txt");
        static string DiffFile   = Path.Combine(Docs, "EPLAN_dump_diff.txt");

        [DeclareAction("DumpBefore")]
        public void DumpBefore()
        {
            int n = DumpTo(BeforeFile);
            MessageBox.Show("Saved " + n + " properties to:\n" + BeforeFile +
                "\n\nNow rename the SAME object by hand (e.g. to 802_A), keep it selected, then run DumpAfter.");
        }

        [DeclareAction("DumpAfter")]
        public void DumpAfter()
        {
            if (!File.Exists(BeforeFile))
            {
                MessageBox.Show("Run DumpBefore first.");
                return;
            }

            DumpTo(AfterFile);

            Dictionary<string, string> before = Load(BeforeFile);
            Dictionary<string, string> after  = Load(AfterFile);
            var sb = new StringBuilder();

            foreach (var kv in after)
            {
                string old;
                if (!before.TryGetValue(kv.Key, out old))
                    sb.AppendLine(kv.Key + " : (not readable before) -> " + kv.Value);
                else if (old != kv.Value)
                    sb.AppendLine(kv.Key + " : " + old + " -> " + kv.Value);
            }
            foreach (var kv in before)
            {
                if (!after.ContainsKey(kv.Key))
                    sb.AppendLine(kv.Key + " : " + kv.Value + " -> (not readable after)");
            }

            File.WriteAllText(DiffFile, sb.Length == 0 ? "No differences found." : sb.ToString());
            MessageBox.Show((sb.Length == 0 ? "No differences found." : "Differences written to:\n" + DiffFile));
        }

        int DumpTo(string path)
        {
            var sb = new StringBuilder();
            int count = 0;
            for (int id = StartId; id <= EndId; id++)
            {
                string v = Get(id.ToString());
                if (!string.IsNullOrEmpty(v))
                {
                    sb.AppendLine(id + "=" + v.Replace("\r", " ").Replace("\n", " "));
                    count++;
                }
            }
            File.WriteAllText(path, sb.ToString());
            return count;
        }

        Dictionary<string, string> Load(string path)
        {
            var d = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(path))
            {
                int p = line.IndexOf('=');
                if (p > 0) d[line.Substring(0, p)] = line.Substring(p + 1);
            }
            return d;
        }

        string Get(string id)
        {
            try
            {
                var cli = new CommandLineInterpreter();
                var acc = new ActionCallingContext();
                acc.AddParameter("PropertyId", id);
                acc.AddParameter("PropertyIndex", "0");
                string v = "";
                if (!cli.Execute("XEsGetPropertyAction", acc)) return null;
                acc.GetParameter("PropertyValue", ref v);
                return v;
            }
            catch
            {
                return null;
            }
        }
    }
}