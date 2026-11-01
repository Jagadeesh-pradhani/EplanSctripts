// XFgEvaluateProjectAction
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Scripting;

namespace Eplan_scripts
{
    public class reportGeneration
    {
        [DeclareAction("reportGeneration")]
        public void CAP()
        {
            CommandLineInterpreter oCli = new CommandLineInterpreter();

            oCli.Execute("XFgEvaluateProjectAction");
            MessageBox.Show("Done");

        }
    }
}