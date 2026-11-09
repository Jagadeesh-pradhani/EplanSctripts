
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Scripting;

namespace MCB_DT_Test
{
    public class MCBWriter
    {
        static string Docs =
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);

        static string LogFile =
            Path.Combine(Docs, "EPLAN_MCB_DT_test.txt");

        // Target DT = D112
        const string TargetPrefix = "";
        const string TargetIdentifier = "D";
        const string TargetCounter = "112";
        const string ExpectedDT = "D112";

        [DeclareAction("FunctionExport")]
        public void Run()
        {
            string before = Get("20010");

            if (before == null)
            {
                MessageBox.Show(
                    "Could not read DT.\nSelect exactly one MCB function.");
                return;
            }

            if (MessageBox.Show(
                "Selected MCB DT: " + before +
                "\n\nNew DT: " + ExpectedDT +
                "\n\nPrefix: (empty)" +
                "\nIdentifier: D" +
                "\nCounter: 112" +
                "\n\nContinue?",
                "MCB DT Write Test",
                MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("================================");
            sb.AppendLine(DateTime.Now.ToString());

            // BEFORE
            sb.AppendLine("\nBEFORE WRITE");
            sb.AppendLine("20006 = " + Get("20006"));
            sb.AppendLine("20010 = " + Get("20010"));
            sb.AppendLine("20012 = " + Get("20012"));
            sb.AppendLine("20013 = " + Get("20013"));
            sb.AppendLine("20014 = " + Get("20014"));

            // STEP 1: PREFIX
            bool r1 = Set("20027", TargetPrefix);

            sb.AppendLine("\nSTEP 1: PREFIX");
            sb.AppendLine("Result = " + r1);
            sb.AppendLine("20027 = " + Get("20012"));

            // STEP 2: IDENTIFIER
            bool r2 = Set("20013", TargetIdentifier);

            sb.AppendLine("\nSTEP 2: IDENTIFIER");
            sb.AppendLine("Result = " + r2);
            sb.AppendLine("20013 = " + Get("20013"));

            // STEP 3: COUNTER
            bool r3 = Set("20014", TargetCounter);

            sb.AppendLine("\nSTEP 3: COUNTER");
            sb.AppendLine("Result = " + r3);
            sb.AppendLine("20014 = " + Get("20014"));

            // FINAL READBACK
            string finalDT = Get("20010");
            string finalPrefix = Get("20012");
            string finalIdentifier = Get("20013");
            string finalCounter = Get("20014");

            bool success = finalDT == ExpectedDT;

            sb.AppendLine("\nAFTER WRITE");
            sb.AppendLine("20006 = " + Get("20006"));
            sb.AppendLine("20010 = " + finalDT);
            sb.AppendLine("20012 = " + finalPrefix);
            sb.AppendLine("20013 = " + finalIdentifier);
            sb.AppendLine("20014 = " + finalCounter);

            sb.AppendLine("\nFINAL RESULT = " +
                (success ? "SUCCESS" : "FAILED"));

            try
            {
                File.AppendAllText(
                    LogFile, sb.ToString() + "\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Log error: " + ex.Message);
            }

            MessageBox.Show(
                "MCB DT WRITE TEST\n\n" +
                "Prefix write: " + r1 +
                "\nIdentifier write: " + r2 +
                "\nCounter write: " + r3 +
                "\n\nFinal DT: " + finalDT +
                "\nExpected DT: " + ExpectedDT +
                "\n\nResult: " +
                (success ? "SUCCESS" : "FAILED") +
                "\n\nLog: " + LogFile);
        }

        string Get(string id)
        {
            try
            {
                var cli = new CommandLineInterpreter();
                var acc = new ActionCallingContext();

                acc.AddParameter("PropertyId", id);
                acc.AddParameter("PropertyIndex", "0");

                string value = "";

                if (!cli.Execute(
                    "XEsGetPropertyAction", acc))
                    return null;

                acc.GetParameter(
                    "PropertyValue", ref value);

                return value;
            }
            catch
            {
                return null;
            }
        }

        bool Set(string id, string value)
        {
            try
            {
                var cli = new CommandLineInterpreter();
                var acc = new ActionCallingContext();

                acc.AddParameter("PropertyId", id);
                acc.AddParameter("PropertyIndex", "0");
                acc.AddParameter("PropertyValue", value);

                return cli.Execute(
                    "XEsSetPropertyAction", acc);
            }
            catch
            {
                return false;
            }
        }
    }
}
