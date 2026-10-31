
using System;
using Eplan.EplApi.RemoteClient;
using Eplan.EplApi.Remoting;

namespace ReadProjectProperties
{
    internal class Program
    {
        static void Main(string[] args)
        {
            EplanRemoteClient client = new EplanRemoteClient();

            try
            {
                string host = "192.168.29.54";
                string port = "49152";

                Console.WriteLine("=================================");
                Console.WriteLine("       EPLAN ACTION TESTER");
                Console.WriteLine("=================================");

                Console.WriteLine("\nConnecting to EPLAN...");

                client.Connect(host, port);
                client.SynchronousMode = true;

                Console.WriteLine("Connected successfully!");

                Console.WriteLine("\nTesting action: selectionset");
                Console.WriteLine("Parameter: TYPE = PROJECT");

                CallingContext context = new CallingContext();
                context.Set("TYPE", "PROJECT");

                bool result = client.ExecuteAction(
                    "selectionset",
                    ref context
                );

                Console.WriteLine("\n========== RESULT ==========");

                Console.WriteLine(
                    "Action returned: " + result
                );

                if (result)
                {
                    Console.WriteLine(
                        "Action executed successfully."
                    );
                }
                else
                {
                    Console.WriteLine(
                        "Action failed or is unavailable."
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR:");
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                try
                {
                    client.Disconnect();
                    Console.WriteLine("\nDisconnected from EPLAN.");
                }
                catch
                {
                    // Connection may not have been established.
                }
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }
    }
}
