using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.Scripting;

public class DtActions
{
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

    // Like GetProperty(name) but never throws on ambiguous matches
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

    // lockObjects=false for read-only; true only when called inside a SafetyPoint
    private static object[] GetSelection(Assembly he, bool lockObjects)
    {
        Type selType = FindType("SelectionSet", he);
        object selObj = Activator.CreateInstance(selType);
        SetBoolProp(selType, selObj, "LockSelectionByDefault", lockObjects);
        SetBoolProp(selType, selObj, "LockProjectByDefault", lockObjects);
        Array arr = (Array)GetPropSafe(selType, "Selection").GetValue(selObj, null);
        object[] result = new object[arr.Length];
        arr.CopyTo(result, 0);
        return result;
    }

    // Splits "=000+S1-A801" into eq="000", pos="S1", tag="A801". Missing parts = "".
    private static void ParseName(string s, out string eq, out string pos, out string tag)
    {
        eq = ""; pos = ""; tag = "";
        if (string.IsNullOrEmpty(s)) return;
        int i = 0;

        if (s[0] == '=')
        {
            int j = s.IndexOfAny(new char[] { '+', '-' }, 1);
            if (j < 0) j = s.Length;
            eq = s.Substring(1, j - 1);
            i = j;
        }
        if (i < s.Length && s[i] == '+')
        {
            int j = s.IndexOf('-', i + 1);
            if (j < 0) j = s.Length;
            pos = s.Substring(i + 1, j - i - 1);
            i = j;
        }
        if (i < s.Length)
        {
            tag = (s[i] == '-') ? s.Substring(i + 1) : s.Substring(i);
        }
    }

    private static string GetPV(object props, string propName)
    {
        PropertyInfo pi = GetPropSafe(props.GetType(), propName);
        if (pi == null) return "?";
        object pv = pi.GetValue(props, null);
        return pv == null ? "" : pv.ToString();
    }

    private static bool SetPV(object props, string propName, string value)
    {
        PropertyInfo pi = GetPropSafe(props.GetType(), propName);
        if (pi == null) return false;

        // First try: PropertyValue.Set(string) on the existing value
        object pv = pi.GetValue(props, null);
        if (pv != null)
        {
            MethodInfo set = pv.GetType().GetMethod("Set", new Type[] { typeof(string) });
            if (set != null) { set.Invoke(pv, new object[] { value }); return true; }
        }
        // Second try: assign a new PropertyValue(string)
        ConstructorInfo ci = pi.PropertyType.GetConstructor(new Type[] { typeof(string) });
        if (ci != null && pi.CanWrite)
        {
            pi.SetValue(props, ci.Invoke(new object[] { value }), null);
            return true;
        }
        return false;
    }

        // Returns "" on success, otherwise a description of what failed
    private static string TrySetPV(object props, string propName, string value)
    {
        try
        {
            PropertyInfo pi = GetPropSafe(props.GetType(), propName);
            if (pi == null) return propName + ": property not found";

            object pv = pi.GetValue(props, null);
            if (pv != null)
            {
                MethodInfo set = pv.GetType().GetMethod("Set", new Type[] { typeof(string) });
                if (set != null) { set.Invoke(pv, new object[] { value }); return ""; }
            }
            ConstructorInfo ci = pi.PropertyType.GetConstructor(new Type[] { typeof(string) });
            if (ci != null && pi.CanWrite)
            {
                pi.SetValue(props, ci.Invoke(new object[] { value }), null);
                return "";
            }
            return propName + ": no way to set it found";
        }
        catch (TargetInvocationException tie)
        {
            Exception e = tie.InnerException != null ? tie.InnerException : tie;
            return propName + " = '" + value + "' failed: " + e.GetType().Name + " - " + e.Message;
        }
        catch (Exception ex)
        {
            return propName + " = '" + value + "' failed: " + ex.Message;
        }
    }

    // ---------- actions ----------

    // Read-only: shows what is selected and each function's full name
    [DeclareAction("DumpBefore")]
    public void TestSelection()
    {
        try
        {
            Assembly he = Assembly.Load("Eplan.EplApi.HEServicesu");
            object[] objs = GetSelection(he, false);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("API works. Selected objects: " + objs.Length);
            foreach (object o in objs)
            {
                sb.Append(" - " + o.GetType().Name);
                PropertyInfo propsInfo = GetPropSafe(o.GetType(), "Properties");
                if (propsInfo != null)
                {
                    object props = propsInfo.GetValue(o, null);
                    if (props != null)
                    {
                        sb.Append("  | Full DT: " + GetPV(props, "FUNC_FULLNAME"));
                        sb.Append("  | Letter(code): " + GetPV(props, "FUNC_CODE"));
                        sb.Append("  | Counter: " + GetPV(props, "FUNC_COUNTER"));
                        sb.Append("  | Prefix: " + GetPV(props, "FUNC_PREFIX"));
                    }
                }
                sb.AppendLine();
            }
            MessageBox.Show(sb.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERROR: " + ex.ToString());
        }
    }

    // Reads E:\eplan_files\test.txt (e.g. A801 or =000+S1-A801).
    // First letter -> identifier (FUNC_CODE), the rest -> FUNC_COUNTER, FUNC_PREFIX cleared.
    // Structure (=000, +S1) is not touched; any structure text in the file is ignored.
    [DeclareAction("DumpAfter")]
    public void SetSelectedDT()
    {
        try
        {
            string input = "";
            if (File.Exists(@"E:\eplan_files\test.txt"))
                input = File.ReadAllText(@"E:\eplan_files\test.txt").Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show(@"E:\eplan_files\test.txt is missing or empty.");
                return;
            }

            string nEq, nPos, nTag;
            ParseName(input, out nEq, out nPos, out nTag);
            if (string.IsNullOrEmpty(nTag))
            {
                MessageBox.Show("No tag found in newdt.txt. Example: A801 or =000+S1-A801");
                return;
            }
            if (!char.IsLetter(nTag[0]))
            {
                MessageBox.Show("Tag must start with a letter (found: '" + nTag + "'). Nothing changed.");
                return;
            }

            Assembly he = Assembly.Load("Eplan.EplApi.HEServicesu");
            Assembly dm = Assembly.Load("Eplan.EplApi.DataModelu");
            Type spType = FindType("SafetyPoint", dm, he);
            if (spType == null) { MessageBox.Show("SafetyPoint type not found."); return; }

            MethodInfo create = null;
            foreach (MethodInfo m in spType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (m.Name == "Create" && m.GetParameters().Length == 0) create = m;
            if (create == null) { MessageBox.Show("SafetyPoint.Create not found."); return; }

            StringBuilder log = new StringBuilder();
            bool anyFailure = false;
            int changed = 0;

            object sp = create.Invoke(null, null);
            try
            {
                object[] objs = GetSelection(he, true);
                foreach (object o in objs)
                {
                    Type ft = GetFunctionType(o);
                    if (ft == null) continue;

                    PropertyInfo propsInfo = GetPropSafe(o.GetType(), "Properties");
                    if (propsInfo == null) continue;
                    object props = propsInfo.GetValue(o, null);
                    if (props == null) continue;

                    string before = GetPV(props, "FUNC_FULLNAME");

                    // structure from the object itself, e.g. "=000+S1"
                    string oEq, oPos, oTagDummy;
                    ParseName(GetPV(props, "FUNC_DEVICETAG_ONLYSTRUCTURES"), out oEq, out oPos, out oTagDummy);
                    string eq = nEq != "" ? nEq : oEq;
                    string pos = nPos != "" ? nPos : oPos;

                    string newName =
                        (eq != "" ? "=" + eq : "") +
                        (pos != "" ? "+" + pos : "") +
                        "-" + nTag;

                    try
                    {
                        PropertyInfo nameProp = GetPropSafe(ft, "Name");
                        nameProp.SetValue(o, newName, null);
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception e = tie.InnerException != null ? tie.InnerException : tie;
                        anyFailure = true;
                        log.AppendLine("Name set failed: " + e.GetType().Name + " - " + e.Message);
                        continue;
                    }

                    // try to make the structure inherited again (may be refused)
                    string c1 = TrySetPV(props, "DESIGNATION_PLANT_VISIBLE", "");
                    string c2 = TrySetPV(props, "DESIGNATION_LOCATION_VISIBLE", "");

                    log.AppendLine(before + "  ->  " + GetPV(props, "FUNC_FULLNAME"));
                    log.AppendLine("   Code='" + GetPV(props, "FUNC_CODE") +
                                   "' Counter='" + GetPV(props, "FUNC_COUNTER") +
                                   "' Prefix='" + GetPV(props, "FUNC_PREFIX") + "'");
                    log.AppendLine("   Clear structure-visible: " +
                                   ((c1 == "" && c2 == "") ? "OK" : "refused (" + c1 + " " + c2 + ")"));
                    changed++;
                }

                if (!anyFailure)
                    spType.GetMethod("Commit", Type.EmptyTypes).Invoke(sp, null);
            }
            finally
            {
                IDisposable d = sp as IDisposable;
                if (d != null) d.Dispose();
            }

            MessageBox.Show((anyFailure ? "NOT COMMITTED\n" : "Committed.\n")
                            + "Processed " + changed + " object(s):\n" + log.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERROR: " + ex.ToString());
        }
    }


    // Writes all non-empty properties of the selected function to E:\eplan_files\props_dump.txt
    [DeclareAction("FunctionExport")]
    public void DumpProps()
    {
        try
        {
            Assembly he = Assembly.Load("Eplan.EplApi.HEServicesu");
            object[] objs = GetSelection(he, false);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("==== DUMP " + DateTime.Now.ToString("HH:mm:ss") + " ====");

            foreach (object o in objs)
            {
                PropertyInfo propsInfo = GetPropSafe(o.GetType(), "Properties");
                if (propsInfo == null) { sb.AppendLine("No Properties member on " + o.GetType().Name); continue; }
                object props = propsInfo.GetValue(o, null);
                if (props == null) continue;

                foreach (PropertyInfo p in props.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    try
                    {
                        object pv = p.GetValue(props, null);
                        if (pv == null) continue;

                        PropertyInfo ie = GetPropSafe(pv.GetType(), "IsEmpty");
                        if (ie != null)
                        {
                            object e = ie.GetValue(pv, null);
                            if (e is bool && (bool)e) continue;
                        }
                        sb.AppendLine(p.Name + " = " + pv.ToString());
                    }
                    catch { }
                }
            }
            File.AppendAllText(@"E:\eplan_files\props_dump.txt", sb.ToString() + "\r\n");
            MessageBox.Show("Dump written to E:\\eplan_files\\props_dump.txt");
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERROR: " + ex.ToString());
        }
    }
}