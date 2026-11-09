
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Scripting;

namespace GetPageInfo
{
    public class DTSearch
    {
        [DeclareAction("DTSearch")]
        public void CAP()
        {
            CommandLineInterpreter cli =
                new CommandLineInterpreter();

            ActionCallingContext acc =
                new ActionCallingContext();

            string pageName = "";

            // Property ID 11009 = Page name (full)
            acc.AddParameter("PropertyId", "11009");
            acc.AddParameter("PropertyIndex", "0");

            bool result = cli.Execute(
                "XEsGetPagePropertyAction", acc);

            acc.GetParameter(
                "PropertyValue", ref pageName);

            if (result)
            {
                MessageBox.Show(
                    "Selected Page:\n" + pageName,
                    "EPLAN Page Reader");
            }
            else
            {
                MessageBox.Show(
                    "Unable to read selected page.");
            }
        }
    }
}
