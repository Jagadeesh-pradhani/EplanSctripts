using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.Scripting;

public class IpLinker
{
    private const string DefaultSourcePage = "=000+S1/320";
    private const string PageFile = @"E:\eplan_files\ip_page.txt";      // optional: other source page
    // 8* -> 801_A, 801_B, 801_C, ...
    // X2* -> X201_911_A, X201_911_B, ...
    private const string PlaceholderTag = "8*";
    private const string PlaceholderPlant = "000";
    private const string PlaceholderPlant2 = "001";
    private const string ReportFile = @"E:\eplan_files\ip_link_report.txt";

    private class Ip
    {
        public object Obj;
        public object Props;
        public string Page = "";
        public string FullName = "";
        public string Tag = "";
        public string Plant = "";
        public string CrossRef = "";
        public int SameNameCount;
        public double X;
        public double Y;
        public bool Referenced;
    }

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

    private static Type GetFunctionType(object o)
    {
        Type t = o.GetType();
        while (t != null)
        {
            if (t.FullName == "Eplan.EplApi.DataModel.Function") return t;
            t = t.BaseType;
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

    private static void SetBoolProp(Type t, object obj, string name, bool value)
    {
        PropertyInfo p = GetPropSafe(t, name);
        if (p != null && p.CanWrite) p.SetValue(obj, value, null);
    }

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

    private static string ExtractTag(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int idx = s.IndexOf('-');
        return idx >= 0 ? s.Substring(idx + 1) : s;
    }

    // "=000-801_A" -> "000"
    private static string PlantOf(string s)
    {
        if (string.IsNullOrEmpty(s) || s[0] != '=') return "";
        int j = s.IndexOfAny(new char[] { '+', '-' }, 1);
        if (j < 0) j = s.Length;
        return s.Substring(1, j - 1);
    }

    private static double ParseD(string s)
    {
        double v;
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
        return 0;
    }

    // natural compare: page "/9" before "/10"
    private static int NatCmp(string a, string b)
    {
        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
            {
                int si = i, sj = j;
                while (i < a.Length && char.IsDigit(a[i])) i++;
                while (j < b.Length && char.IsDigit(b[j])) j++;
                string na = a.Substring(si, i - si).TrimStart('0');
                string nb = b.Substring(sj, j - sj).TrimStart('0');
                if (na.Length != nb.Length) return na.Length < nb.Length ? -1 : 1;
                int c = string.CompareOrdinal(na, nb);
                if (c != 0) return c;
            }
            else
            {
                int c = char.ToUpperInvariant(a[i]).CompareTo(char.ToUpperInvariant(b[j]));
                if (c != 0) return c;
                i++; j++;
            }
        }
        return (a.Length - i).CompareTo(b.Length - j);
    }

    // ---------- loading ----------

    private static List<Ip> LoadAll(Assembly he, Assembly dm, bool lockProject)
    {
        Type selType = FindType("SelectionSet", he);
        object sel = Activator.CreateInstance(selType);
        SetBoolProp(selType, sel, "LockProjectByDefault", lockProject);

        MethodInfo gcp = null;
        foreach (MethodInfo m in selType.GetMethods())
            if (m.Name == "GetCurrentProject" && m.GetParameters().Length == 1) gcp = m;
        if (gcp == null) throw new Exception("GetCurrentProject not found.");
        object project = gcp.Invoke(sel, new object[] { false });
        if (project == null) throw new Exception("No active project found. Open a project first.");

        Type finderType = FindType("DMObjectsFinder", dm);
        object finder = Activator.CreateInstance(finderType, new object[] { project });
        MethodInfo getIp = null;
        foreach (MethodInfo m in finderType.GetMethods())
            if (m.Name == "GetInterruptionPoints" && m.GetParameters().Length == 1) getIp = m;
        if (getIp == null) throw new Exception("GetInterruptionPoints not found.");

        Array all = (Array)getIp.Invoke(finder, new object[] { null });

        List<Ip> list = new List<Ip>();
        Dictionary<string, int> counts = new Dictionary<string, int>();

        foreach (object o in all)
        {
            PropertyInfo pinfo = GetPropSafe(o.GetType(), "Properties");
            if (pinfo == null) continue;
            object props = pinfo.GetValue(o, null);
            if (props == null) continue;

            Ip ip = new Ip();
            ip.Obj = o;
            ip.Props = props;
            ip.Page = GetPV(props, "INSTANCE_PAGEFULLNAME");
            ip.FullName = GetPV(props, "FUNC_FULLNAME");
            if (ip.FullName == "") ip.FullName = GetPV(props, "FUNC_IDENTNAME");
            string vis = GetPV(props, "FUNC_VISIBLENAME");
            if (vis == "") vis = ip.FullName;
            ip.Tag = ExtractTag(vis);
            ip.Plant = PlantOf(ip.FullName);
            ip.CrossRef = GetPV(props, "FUNC_CROSSREFERENCE");
            if (ip.CrossRef == "") ip.CrossRef = GetPV(props, "FUNC_MAINCROSSREFERENCE");
            ip.X = ParseD(GetPV(props, "INSTANCE_XCOORD"));
            ip.Y = ParseD(GetPV(props, "INSTANCE_YCOORD"));

            string key = ip.FullName.ToUpperInvariant();
            int c;
            counts.TryGetValue(key, out c);
            counts[key] = c + 1;
            list.Add(ip);
        }

        foreach (Ip ip in list)
        {
            int c;
            counts.TryGetValue(ip.FullName.ToUpperInvariant(), out c);
            ip.SameNameCount = c;
            ip.Referenced = (c >= 2) || (ip.CrossRef != "");
        }
        return list;
    }

    // ---------- planning ----------

    private static void BuildPlan(List<Ip> all, string srcPage,
                                  out List<Ip> srcList, out List<Ip> free, out List<Ip> targets)
    {
        srcList = new List<Ip>();
        free = new List<Ip>();
        targets = new List<Ip>();

        foreach (Ip ip in all)
        {
            if (string.Equals(ip.Page, srcPage, StringComparison.OrdinalIgnoreCase))
                srcList.Add(ip);
        }
        srcList.Sort(delegate(Ip a, Ip b) { return NatCmp(a.Tag, b.Tag); });

        List<string> usedNames = new List<string>();
        foreach (Ip ip in srcList)
        {
            if (ip.Referenced) continue;
            string key = ip.FullName.ToUpperInvariant();
            if (usedNames.Contains(key)) continue;
            usedNames.Add(key);
            free.Add(ip);
        }

        foreach (Ip ip in all)
        {
            if (ip.Tag != PlaceholderTag) continue;
            // Uncomment below if you want to replace from only specific structure
            // if (ip.Plant != "" && ip.Plant != PlaceholderPlant && ip.Plant != PlaceholderPlant2) continue;
            if (ip.Plant == "") continue;
            targets.Add(ip);
        }
        targets.Sort(delegate(Ip a, Ip b)
        {
            int c = NatCmp(a.Page, b.Page);
            if (c != 0) return c;
            c = a.Y.CompareTo(b.Y);
            if (c != 0) return c;
            return a.X.CompareTo(b.X);
        });
    }

    private static string BuildReport(string srcPage, int total, List<Ip> srcList,
                                      List<Ip> free, List<Ip> targets)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Source page: " + srcPage + "   Interruption points in project: " + total);
        sb.AppendLine();
        sb.AppendLine("Points on the source page: " + srcList.Count);
        foreach (Ip ip in srcList)
        {
            sb.AppendLine("  " + ip.FullName + "   xref='" + ip.CrossRef + "'   same-name count=" + ip.SameNameCount +
                          "   -> " + (ip.Referenced ? "ALREADY cross-referenced" : "FREE"));
        }
        sb.AppendLine();
        sb.AppendLine("Free names available: " + free.Count);
        sb.AppendLine("Placeholders (" + PlaceholderTag + ") found in project: " + targets.Count);
        sb.AppendLine();

        int n = Math.Min(free.Count, targets.Count);
        for (int k = 0; k < n; k++)
        {
            sb.AppendLine("  " + targets[k].Page + "   " + targets[k].FullName + "   ->   " + free[k].FullName);
        }
        if (targets.Count > free.Count)
        {
            sb.AppendLine();
            sb.AppendLine("NOT ENOUGH FREE NAMES. Left over placeholders: " + (targets.Count - free.Count));
            for (int k = n; k < targets.Count; k++)
                sb.AppendLine("  " + targets[k].Page + "   " + targets[k].FullName);
        }
        else if (free.Count > targets.Count)
        {
            sb.AppendLine();
            sb.AppendLine("Free names left unused: " + (free.Count - targets.Count));
            for (int k = n; k < free.Count; k++)
                sb.AppendLine("  " + free[k].FullName);
        }
        return sb.ToString();
    }

    private static void Show(string report)
    {
        File.WriteAllText(ReportFile, report);
        string shown = report;
        if (shown.Length > 1800) shown = shown.Substring(0, 1800) + "\n... (full report: " + ReportFile + ")";
        MessageBox.Show(shown);
    }

    private static string GetSourcePage()
    {
        string p = DefaultSourcePage;
        if (File.Exists(PageFile))
        {
            string t = File.ReadAllText(PageFile).Trim();
            if (t != "") p = t;
        }
        return p;
    }

    // ---------- actions ----------

    [DeclareAction("DumpBefore")]
    public void PlanInterruptionLinks()
    {
        try
        {
            Assembly he = Assembly.Load("Eplan.EplApi.HEServicesu");
            Assembly dm = Assembly.Load("Eplan.EplApi.DataModelu");
            string srcPage = GetSourcePage();

            List<Ip> all = LoadAll(he, dm, false);
            List<Ip> srcList, free, targets;
            BuildPlan(all, srcPage, out srcList, out free, out targets);

            Show("PLAN ONLY - nothing was changed.\n" + BuildReport(srcPage, all.Count, srcList, free, targets));
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERROR: " + ex.ToString());
        }
    }

    [DeclareAction("DumpAfter")]
    public void ApplyInterruptionLinks()
    {
        object sp = null;
        try
        {
            Assembly he = Assembly.Load("Eplan.EplApi.HEServicesu");
            Assembly dm = Assembly.Load("Eplan.EplApi.DataModelu");
            string srcPage = GetSourcePage();

            Type spType = FindType("SafetyPoint", dm, he);
            if (spType == null) { MessageBox.Show("SafetyPoint type not found."); return; }
            MethodInfo create = null;
            foreach (MethodInfo m in spType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (m.Name == "Create" && m.GetParameters().Length == 0) create = m;
            if (create == null) { MessageBox.Show("SafetyPoint.Create not found."); return; }

            // SafetyPoint first, then load with locking on
            sp = create.Invoke(null, null);

            List<Ip> all = LoadAll(he, dm, true);
            List<Ip> srcList, free, targets;
            BuildPlan(all, srcPage, out srcList, out free, out targets);

            int n = Math.Min(free.Count, targets.Count);
            string report = BuildReport(srcPage, all.Count, srcList, free, targets);

            if (n == 0)
            {
                Show("NOTHING TO DO.\n" + report);
                return;
            }

            DialogResult dr = MessageBox.Show(
                "Rename " + n + " interruption point(s)?\n\n(See the plan in " + ReportFile + ")",
                "Apply interruption point links", MessageBoxButtons.YesNo);
            File.WriteAllText(ReportFile, report);
            if (dr != DialogResult.Yes) return;

            StringBuilder log = new StringBuilder();
            bool anyFailure = false;
            int done = 0;

            for (int k = 0; k < n; k++)
            {
                Ip t = targets[k];
                string newName = free[k].FullName;
                string oldName = t.FullName;
                try
                {
                    Type ft = GetFunctionType(t.Obj);
                    PropertyInfo nameProp = GetPropSafe(ft != null ? ft : t.Obj.GetType(), "Name");
                    if (nameProp == null) throw new Exception("Name property not found");
                    nameProp.SetValue(t.Obj, newName, null);
                    log.AppendLine(t.Page + "   " + oldName + "  ->  " + GetPV(t.Props, "FUNC_FULLNAME"));
                    done++;
                }
                catch (Exception ex)
                {
                    Exception e = ex;
                    if (ex is TargetInvocationException && ex.InnerException != null) e = ex.InnerException;
                    anyFailure = true;
                    log.AppendLine(t.Page + "   " + oldName + "  FAILED: " + e.GetType().Name + " - " + e.Message);
                }
            }

            if (!anyFailure)
                spType.GetMethod("Commit", Type.EmptyTypes).Invoke(sp, null);

            Show((anyFailure ? "NOT COMMITTED (a rename failed, changes rolled back)\n" : "COMMITTED.\n")
                 + "Renamed " + done + " of " + n + ":\n" + log.ToString()
                 + (targets.Count > free.Count ? "\nLeft over placeholders: " + (targets.Count - free.Count) : ""));
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERROR: " + ex.ToString());
        }
        finally
        {
            IDisposable d = sp as IDisposable;
            if (d != null) d.Dispose();
        }
    }
}