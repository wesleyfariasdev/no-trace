namespace zrobts.Models;

/// <summary>
/// Modelo que representa informações de um drive do sistema
/// </summary>
public class DriveInfoModel
{
    public string Letter { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VolumeLabel { get; set; } = string.Empty;
    public bool IsSSD { get; set; }
    public string DriveType => IsSSD ? "SSD" : "HDD";
    public long TotalSize { get; set; }
    public long FreeSpace { get; set; }
    public long UsedSpace => TotalSize - FreeSpace;
    public double FreeSpacePercent => TotalSize > 0 ? (double)FreeSpace / TotalSize * 100 : 0;
    public double UsedSpacePercent => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;
    
    public string TotalSizeFormatted => FormatBytes(TotalSize);
    public string FreeSpaceFormatted => FormatBytes(FreeSpace);
    public string UsedSpaceFormatted => FormatBytes(UsedSpace);
    
    public string DisplayName => string.IsNullOrEmpty(VolumeLabel) 
        ? $"Disco Local ({Letter}:)" 
        : $"{VolumeLabel} ({Letter}:)";

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int counter = 0;
        decimal number = bytes;
        
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        
        return $"{number:n1} {suffixes[counter]}";
    }
}
