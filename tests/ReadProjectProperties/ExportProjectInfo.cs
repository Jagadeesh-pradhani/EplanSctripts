
public class ExportProjectInfo
{
    [Start]
    public bool Run()
    {
        System.IO.Directory.CreateDirectory(@"E:\Temp");

        System.IO.File.WriteAllText(
            @"E:\Temp\EplanProjectReport.txt",
            "SUCCESS: EPLAN script executed successfully."
        );

        return true;
    }
}
