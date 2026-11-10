using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Scripting;

public class MacroInserter
{
    // >>> CHANGE THIS to your macro folder <<<
    private const string MacroFolder = @"E:\eplan_files\macros";

    // Macro variant: 0 = variant A, 1 = variant B, ...
    private const string Variant = "0";

    // Leave "" to let EPLAN decide. If insertion fails, try "1" (multi-line schematic).
    private const string RepresentationType = "";

    private static string AskName()
    {
        Form f = new Form();
        f.Text = "Insert macro";
        f.Width = 380;
        f.Height = 150;
        f.StartPosition = FormStartPosition.CenterScreen;
        f.FormBorderStyle = FormBorderStyle.FixedDialog;
        f.MinimizeBox = false;
        f.MaximizeBox = false;
        f.TopMost = true;

        Label l = new Label();
        l.Text = "Macro name (with or without .ema):";
        l.Left = 12; l.Top = 14; l.Width = 340;

        TextBox tb = new TextBox();
        tb.Left = 12; tb.Top = 38; tb.Width = 340;

        Button ok = new Button();
        ok.Text = "OK";
        ok.Left = 196; ok.Top = 72; ok.Width = 75;
        ok.DialogResult = DialogResult.OK;

        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.Left = 277; cancel.Top = 72; cancel.Width = 75;
        cancel.DialogResult = DialogResult.Cancel;

        f.Controls.Add(l);
        f.Controls.Add(tb);
        f.Controls.Add(ok);
        f.Controls.Add(cancel);
        f.AcceptButton = ok;
        f.CancelButton = cancel;

        string result = null;
        if (f.ShowDialog() == DialogResult.OK) result = tb.Text.Trim();
        f.Dispose();
        return result;
    }

    [DeclareAction("FunctionExport")]
    public void InsertMacroByName()
    {
        try
        {
            if (!Directory.Exists(MacroFolder))
            {
                MessageBox.Show("Macro folder not found:\n" + MacroFolder + "\n\nEdit MacroFolder in the script.");
                return;
            }

            string name = AskName();
            if (string.IsNullOrEmpty(name)) return;   // cancelled or empty

            if (name.EndsWith(".ema", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            string[] files = Directory.GetFiles(MacroFolder, "*.ema", SearchOption.AllDirectories);

            List<string> exact = new List<string>();
            List<string> similar = new List<string>();
            foreach (string f in files)
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) exact.Add(f);
                else if (n.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) similar.Add(f);
            }

            if (exact.Count == 0)
            {
                StringBuilder sb = new StringBuilder("No macro named '" + name + "' found in:\n" + MacroFolder + "\n");
                if (similar.Count > 0)
                {
                    sb.AppendLine("\nSimilar names:");
                    int n = 0;
                    foreach (string s in similar)
                    {
                        if (n++ >= 20) { sb.AppendLine("  ..."); break; }
                        sb.AppendLine("  " + Path.GetFileNameWithoutExtension(s));
                    }
                }
                MessageBox.Show(sb.ToString());
                return;
            }

            if (exact.Count > 1)
            {
                StringBuilder sb = new StringBuilder("More than one macro is named '" + name + "':\n\n");
                foreach (string s in exact) sb.AppendLine(s);
                sb.AppendLine("\nRename one of them or type a more specific name. Nothing was inserted.");
                MessageBox.Show(sb.ToString());
                return;
            }

            string path = exact[0];

            CommandLineInterpreter cli = new CommandLineInterpreter();
            ActionCallingContext acc = new ActionCallingContext();
            acc.AddParameter("Name", "XMIaInsertMacro");
            acc.AddParameter("filename", path);
            acc.AddParameter("variant", Variant);
            if (RepresentationType != "") acc.AddParameter("RepresentationType", RepresentationType);

            bool ok = cli.Execute("XGedStartInteractionAction", acc);
            if (!ok)
                MessageBox.Show("EPLAN did not start the macro insertion for:\n" + path +
                                "\n\nCheck that a page is open in the graphical editor.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERROR: " + ex.ToString());
        }
    }
}