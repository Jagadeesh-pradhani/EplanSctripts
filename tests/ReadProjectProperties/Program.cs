
using System;
using System.IO;
using Eplan.EplApi.System;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.Starter;

namespace ReadProjectProperties
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            EplApplication eplanApp = null;
            bool initialized = false;

            string platformBin =
                @"E:\Eplan\Platform\2026.0.3\Bin";

            string educationBin =
                @"E:\Eplan\Education\2026.0.3\Bin";

            try
            {
                Console.WriteLine("Configuring EPLAN assembly resolver...");

                Environment.CurrentDirectory = platformBin;

                AssemblyResolver resolver = new AssemblyResolver();

                resolver.SetEplanBinPath(educationBin);
                resolver.PinToEplan();

                Console.WriteLine("Platform Bin: " + resolver.GetPlatformBinPath());
                Console.WriteLine("Variant Bin: " + resolver.GetEplanBinPath());

                Console.WriteLine();
                Console.WriteLine("Initializing EPLAN API...");

                eplanApp = new EplApplication();

                eplanApp.EplanBinFolder = educationBin;

                eplanApp.Init("");

                initialized = true;

                Console.WriteLine("EPLAN API initialized!");
                Console.WriteLine("Version: " + eplanApp.Version);
                Console.WriteLine("Variant: " + eplanApp.Variant);

                if (args.Length == 0)
                {
                    Console.WriteLine("Please provide an EPLAN project path.");
                    return;
                }

                string projectPath = args[0];

                Console.WriteLine();
                Console.WriteLine("Project path: " + projectPath);

                using (LockingStep lockingStep = new LockingStep())
                {
                    ProjectManager manager = new ProjectManager();

                    Console.WriteLine("Opening project...");

                    Project project = manager.OpenProject(projectPath);

                    if (project == null)
                        throw new Exception("Unable to open project.");

                    Console.WriteLine("Project opened!");

                    Console.WriteLine();
                    Console.WriteLine("PROJECT INFORMATION");
                    Console.WriteLine("===================");

                    Console.WriteLine("Project Name: " + project.ProjectName);
                    Console.WriteLine("Project Full Name: " + project.ProjectFullName);
                    Console.WriteLine("Project Directory: " + project.ProjectDirectoryPath);

                    Page[] pages = project.Pages;

                    Console.WriteLine();
                    Console.WriteLine("TOTAL PAGES: " + pages.Length);

                    for (int i = 0; i < pages.Length; i++)
                    {
                        Console.WriteLine(
                            (i + 1) + ". " + pages[i].Name);
                    }

                    string reportPath = Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.Desktop),
                        "EplanProjectReport.txt");

                    File.WriteAllText(
                        reportPath,
                        "Project Name: " + project.ProjectName +
                        Environment.NewLine +
                        "Project Full Name: " + project.ProjectFullName +
                        Environment.NewLine +
                        "Project Directory: " + project.ProjectDirectoryPath +
                        Environment.NewLine +
                        "Total Pages: " + pages.Length +
                        Environment.NewLine +
                        string.Join(
                            Environment.NewLine,
                            Array.ConvertAll(pages, p => p.Name)));

                    Console.WriteLine();
                    Console.WriteLine("Report saved: " + reportPath);

                    project.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("ERROR:");
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (initialized && eplanApp != null)
                {
                    Console.WriteLine("Shutting down EPLAN...");
                    eplanApp.Exit();
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
