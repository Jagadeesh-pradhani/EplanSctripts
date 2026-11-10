
using System;
using Eplan.EplApi.RemoteClient;

namespace EplanRemoteTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = "192.168.29.54";
            string port = "49152";

            EplanRemoteClient client = null;
            bool connected = false;

            try
            {
                Console.WriteLine("==================================");
                Console.WriteLine("     EPLAN REMOTE ACTION TEST");
                Console.WriteLine("==================================");

                // Create Remote Client
                client = new EplanRemoteClient();

                // Connect to EPLAN
                Console.WriteLine("\nConnecting to EPLAN...");

                client.Connect(host, port);
                connected = true;

                Console.WriteLine("Connected successfully!");

                // Enable synchronous action execution
                client.SynchronousMode = true;

                // Check server
                if (!client.Ping())
                {
                    Console.WriteLine("EPLAN server is not responding.");
                    return;
                }

                Console.WriteLine("EPLAN server is responding.");

                // Execute EPLAN action
                string actionName = "reportGeneration";

                Console.WriteLine("\nExecuting action: " + actionName);
                string newDT = "=GEN+ST-A1";   // example new full device tag
                string cmd = "XEsSetPropertyAction /PropertyId:20002 /PropertyIndex:0 /PropertyValue:\"" + newDT + "\"";

                bool result = client.ExecuteAction(cmd);

                // bool result = client.ExecuteAction(actionName);

                if (result)
                {
                    Console.WriteLine("\nACTION EXECUTED SUCCESSFULLY!");
                    Console.WriteLine("Check the EPLAN window.");
                }
                else
                {
                    Console.WriteLine("\nACTION EXECUTION FAILED!");
                    Console.WriteLine("Check the action name and availability.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR:");
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (client != null)
                {
                    if (connected)
                    {
                        client.Disconnect();
                        Console.WriteLine("\nDisconnected from EPLAN.");
                    }

                    client.Dispose();
                }
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }
    }
}
