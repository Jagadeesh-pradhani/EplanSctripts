
// Code to get the selected connection point's cross reference point
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

            // Read full DT
            ActionCallingContext accDT =
                new ActionCallingContext();

            string DT = "";

            accDT.AddParameter("PropertyId", "20006");
            accDT.AddParameter("PropertyIndex", "0");

            bool dtResult = cli.Execute(
                "XEsGetPropertyAction", accDT);

            accDT.GetParameter("PropertyValue", ref DT);


            // Read cross-reference
            ActionCallingContext accCross =
                new ActionCallingContext();

            string crossReference = "";

            accCross.AddParameter("PropertyId", "24300");
            accCross.AddParameter("PropertyIndex", "0");

            bool crossResult = cli.Execute(
                "XEsGetPropertyAction", accCross);

            accCross.GetParameter(
                "PropertyValue", ref crossReference);


            // Prepare message
            string status = "";

            if (!crossResult)
            {
                status = "Unable to read cross-reference.";
            }
            else if (string.IsNullOrWhiteSpace(crossReference))
            {
                status = "Cross-reference: NOT FOUND";
            }
            else
            {
                status = "Cross-reference: FOUND\n"
                       + crossReference;
            }

            if (dtResult)
            {
                MessageBox.Show(
                    "Black Box DT: " + DT +
                    "\n\n" + status,
                    "EPLAN Black Box Reader");
            }
            else
            {
                MessageBox.Show("Unable to read DT.");
            }
        }
    }
}
