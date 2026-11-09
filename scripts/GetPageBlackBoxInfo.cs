
// Code to get the selected parts DT
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Scripting;

namespace GetPageInfo
{
    public class BlackBoxReader
    {
        [DeclareAction("BlackBoxReader")]
        public void CAP()
        {
            CommandLineInterpreter cli =
                new CommandLineInterpreter();

            ActionCallingContext acc =
                new ActionCallingContext();

            string DT = "";

            // Property ID 20006 = DT (full)
            acc.AddParameter("PropertyId", "20006");
            acc.AddParameter("PropertyIndex", "0");

            bool result = cli.Execute(
                "XEsGetPropertyAction", acc);

            acc.GetParameter("PropertyValue", ref DT);

            if (result)
            {
                MessageBox.Show(
                    "Black Box DT: " + DT,
                    "EPLAN Black Box Reader");
            }
            else
            {
                MessageBox.Show("Unable to read DT.");
            }
        }
    }
}
