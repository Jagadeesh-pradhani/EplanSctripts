
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
                Console.WriteLine("Connecting to EPLAN...");

                client.Connect("192.168.29.54", "49152");
                client.SynchronousMode = true;

                Console.WriteLine("Connected!\n");

                // Select the test page
                CallingContext selectionContext = new CallingContext();
                selectionContext.Set("TYPE", "PAGES");

                bool selectionResult = client.ExecuteAction(
                    "selectionset", ref selectionContext);

                Console.WriteLine(
                    "Selection action: " + selectionResult);

                // Read page description
                Console.WriteLine("\nReading page description...");

                CallingContext propertyContext = new CallingContext();

                propertyContext.Set("PropertyId", "11048");
                propertyContext.Set("PropertyIndex", "0");

                bool propertyResult = client.ExecuteAction(
                    "XEsGetPagePropertyAction",
                    ref propertyContext
                );

                Console.WriteLine(
                    "Property action: " + propertyResult);

                object propertyValue;

                if (propertyContext.Get(
                    "PropertyValue", out propertyValue))
                {
                    Console.WriteLine(
                        "Page description: " + propertyValue);
                }
                else
                {
                    Console.WriteLine(
                        "PropertyValue was not returned.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR:");
                Console.WriteLine(ex);
            }
            finally
            {
                try
                {
                    client.Disconnect();
                    Console.WriteLine("\nDisconnected.");
                }
                catch { }
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }
    }
}
