
using System;
using System.IO;
using Eplan.EplApi.RemoteClient;
using Eplan.EplApi.Remoting;

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
                Console.WriteLine("================================");
                Console.WriteLine("       EPLAN PROJECT READER");
                Console.WriteLine("================================");

                // Connect to EPLAN
                client = new EplanRemoteClient();

                Console.WriteLine("\nConnecting to EPLAN...");

                client.Connect(host, port);
                connected = true;

                client.SynchronousMode = true;

                Console.WriteLine("Connection established!");

                // Check server
                if (!client.Ping())
                {
                    Console.WriteLine("EPLAN server not responding.");
                    return;
                }

                // Create EPLAN calling context
                CallingContext context = new CallingContext();

                // Set action parameter
                context.Set("TYPE", "PROJECT");

                Console.WriteLine("\nReading active project...");

                // Execute selectionset action
                bool result = client.ExecuteAction(
                    "selectionset",
                    ref context
                );

                if (result)
                {
                    // Retrieve returned project path
                    object projectValue;

                    bool found = context.Get(
                        "PROJECT",
                        out projectValue
                    );

                    if (found && projectValue != null)
                    {
                        string projectPath = projectValue.ToString();

                        string projectName =
                            Path.GetFileNameWithoutExtension(projectPath);

                        Console.WriteLine("\nPROJECT FOUND!");
                        Console.WriteLine("-------------------------------");
                        Console.WriteLine("Project Name : " + projectName);
                        Console.WriteLine("Project Path : " + projectPath);
                        Console.WriteLine("-------------------------------");
                    }
                    else
                    {
                        Console.WriteLine(
                            "\nNo active project returned."
                        );

                        Console.WriteLine(
                            "Open a project in EPLAN and try again."
                        );

                        Console.WriteLine(
                            "Server message: " + context.Message
                        );
                    }
                }
                else
                {
                    Console.WriteLine("\nAction execution failed!");
                    Console.WriteLine("Message: " + context.Message);
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
