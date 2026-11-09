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

            // Replace with your exact scheme name
            acc.AddParameter("ConfigScheme", "Cables");

            acc.AddParameter("Language", "en_US");

            acc.AddParameter(
                "Destination",
                @"C:\Temp\FunctionExport_Test.xlsx"
            );

            acc.AddParameter("ExecutionMode", "0");

            bool result = cli.Execute("XMExportFunctionAction", acc);

            MessageBox.Show(
                result
                    ? "Export successful. Check C:\\Temp\\FunctionExport_Test.xlsx"
                    : "Export failed. Check the EPLAN messages."
            );
        }
    }
}