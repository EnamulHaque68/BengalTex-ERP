namespace BengalTex.ERP.Infrastructure.Services;

public class FileStorageSettings
{
    /// <summary>
    /// Storage root. If relative, anchored at the app's current directory (content root).
    /// </summary>
    public string RootPath { get; set; } = "uploads";
}
