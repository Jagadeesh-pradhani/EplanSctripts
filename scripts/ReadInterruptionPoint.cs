using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.Scripting;

public class IpActions
{
    private const string DefaultPage = "=000+S1/320";
    private const string PageFile = @"E:\eplan_files\ip_page.txt";   // optional: put another page name here
    private const string OutFile = @"E:\eplan_files\ip_list.txt";

    // ---------- helpers ----------

    private static Type FindType(string simpleName, params Assembly[] asms)
    {
        foreach (Assembly a in asms)
        {
            Type[] types;
            try { types = a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }
            foreach (Type t in types)
            {
                if (t != null && t.Name == simpleName) return t;
            }
        }
        return null;
    }

    private static PropertyInfo GetPropSafe(Type t, string name)
    {
        foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.Name == name && p.GetIndexParameters().Length == 0) return p;
        }
        return null;
    }

    // Returns "" if the property is missing or empty
    private static string GetPV(object props, string propName)
    {
        try
        {
            PropertyInfo pi = GetPropSafe(props.GetType(), propName);
            if (pi == null) return "";
            object pv = pi.GetValue(props, null);
            return pv == null ? "" : pv.ToString();
        }
        catch { return ""; }
    }

    // "-801_A" or "=000+S1-801_A" -> "801_A"
    private static string ExtractTag(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int idx = s.IndexOf('-');
        return idx >= 0 ? s.Substring(idx + 1) : s;
    }

    // Your rules:
    //  only digits            -> counter
    //  starts with digit(s)   -> prefix = leading digits, identifier = following non-digits, counter = rest
    //  starts with non-digit  -> identifier = leading non-digits, counter = rest
    private static void SplitIpTag(string tag, out string prefix, out string identifier, out string counter)
    {
        prefix = ""; identifier = ""; counter = "";
        if (string.IsNullOrEmpty(tag)) return;

        bool allDigits = true;
        foreach (char c in tag) if (!char.IsDigit(c)) { allDigits = false; break; }
        if (allDigits) { counter = tag; return; }

        int i = 0;
        if (char.IsDigit(tag[0]))
        {
            while (i < tag.Length && char.IsDigit(tag[i])) i++;
            prefix = tag.Substring(0, i);
        }
        int j = i;
        while (j < tag.Length && !char.IsDigit(tag[j])) j++;
        identifier = tag.Substring(i, j - i);
        counter = tag.Substring(j);
    }

    // ---------- action ----------

    [DeclareAction("FunctionExport")]
    public void ReadInterruptionPoints()
    {
        try
        {
            string pageName = DefaultPage;
            if (File.Exists(PageFile))
            {
                string t = File.ReadAllText(PageFile).Trim();
                if (t != "") pageName = t;
            }

            Assembly he = Assembly.Load("Eplan.EplApi.HEServicesu");
            Assembly dm = Assembly.Load("Eplan.EplApi.DataModelu");

            // current project (no selection needed)
            Type selType = FindType("SelectionSet", he);
            object sel = Activator.CreateInstance(selType);
            MethodInfo gcp = null;
            foreach (MethodInfo m in selType.GetMethods())
                if (m.Name == "GetCurrentProject" && m.GetParameters().Length == 1) gcp = m;
            if (gcp == null) { MessageBox.Show("GetCurrentProject not found."); return; }
            object project = gcp.Invoke(sel, new object[] { false });
            if (project == null) { MessageBox.Show("No active project found. Open a project first."); return; }

            // finder
            Type finderType = FindType("DMObjectsFinder", dm);
            object finder = Activator.CreateInstance(finderType, new object[] { project });
            MethodInfo getIp = null;
            foreach (MethodInfo m in finderType.GetMethods())
                if (m.Name == "GetInterruptionPoints" && m.GetParameters().Length == 1) getIp = m;
            if (getIp == null) { MessageBox.Show("GetInterruptionPoints not found."); return; }

            Array all = (Array)getIp.Invoke(finder, new object[] { null });

            StringBuilder sb = new StringBuilder();
            StringBuilder otherPages = new StringBuilder();
            List<string> seenPages = new List<string>();
            int found = 0;

            foreach (object o in all)
            {
                PropertyInfo propsInfo = GetPropSafe(o.GetType(), "Properties");
                if (propsInfo == null) continue;
                object props = propsInfo.GetValue(o, null);
                if (props == null) continue;

                string pg = GetPV(props, "INSTANCE_PAGEFULLNAME");
                if (pg != "" && !seenPages.Contains(pg)) seenPages.Add(pg);
                if (!string.Equals(pg, pageName, StringComparison.OrdinalIgnoreCase)) continue;

                string raw = GetPV(props, "FUNC_VISIBLENAME");
                if (raw == "") raw = GetPV(props, "FUNC_FULLNAME");
                if (raw == "") raw = GetPV(props, "FUNC_IDENTNAME");

                string tag = ExtractTag(raw);
                string prefix, ident, counter;
                SplitIpTag(tag, out prefix, out ident, out counter);

                sb.AppendLine(raw + "   |  Prefix: '" + prefix + "'  Identifier: '" + ident + "'  Counter: '" + counter + "'");
                found++;
            }

            string header = "Page: " + pageName + "   Interruption points in project: " + all.Length +
                            "   On this page: " + found;

            if (found == 0)
            {
                int n = 0;
                foreach (string s in seenPages)
                {
                    if (n++ >= 15) break;
                    otherPages.AppendLine("   " + s);
                }
                sb.AppendLine("No interruption points matched this page name.");
                sb.AppendLine("Page names seen on interruption points (first 15):");
                sb.Append(otherPages.ToString());
            }

            File.WriteAllText(OutFile, header + "\r\n" + sb.ToString());

            string shown = sb.ToString();
            if (shown.Length > 1500) shown = shown.Substring(0, 1500) + "\n... (see " + OutFile + ")";
            MessageBox.Show(header + "\n\n" + shown);
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERROR: " + ex.ToString());
        }
    }
}