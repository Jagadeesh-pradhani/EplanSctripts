using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Scripting;
using System.Windows.Forms;

namespace GetPageInfo
{
    public class FunctionExport
    {
        [DeclareAction("FunctionExport")]
        public void Export()
        {
            CommandLineInterpreter cli = new CommandLineInterpreter();
            ActionCallingContext acc = new ActionCallingContext();

            acc.AddParameter("CompleteProject", "1");
            acc.AddParameter("Language", "en_US");
            acc.AddParameter("Destination", @"E:\Temp\TestFunctions.xlsx");
            acc.AddParameter("ExecutionMode", "0");

            bool result = cli.Execute("XMExportFunctionAction", acc);

            MessageBox.Show("Export action: " + result);
        }
    }
}