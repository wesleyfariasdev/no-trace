using System.IO;
using System.Management;
using System.Security.Principal;
using zrobts.Models;

namespace zrobts.Helpers;

/// <summary>
/// Helper para operações relacionadas a drives do sistema
/// </summary>
public static class DriveHelper
{
    /// <summary>
    /// Obtém todos os drives fixos do sistema com informações detalhadas
    /// </summary>
    public static List<DriveInfoModel> GetAllDrives()
    {
        var drives = new List<DriveInfoModel>();
        var ssdDrives = GetSSDDriveLetters();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                continue;

            var driveModel = new DriveInfoModel
            {
                Letter = drive.Name.Replace(":\\", "").Replace(":/", ""),
                Name = drive.Name,
                VolumeLabel = drive.VolumeLabel,
                TotalSize = drive.TotalSize,
                FreeSpace = drive.TotalFreeSpace,
                IsSSD = ssdDrives.Contains(drive.Name[0].ToString().ToUpper())
            };

            drives.Add(driveModel);
        }

        return drives;
    }

    /// <summary>
    /// Obtém as letras dos drives que são SSD usando WMI
    /// </summary>
    private static HashSet<string> GetSSDDriveLetters()
    {
        var ssdLetters = new HashSet<string>();

        try
        {
            // Primeiro, obter mapeamento de DiskNumber para MediaType
            var diskMediaTypes = new Dictionary<uint, bool>();

            using (var searcher = new ManagementObjectSearcher(
                @"\\.\ROOT\Microsoft\Windows\Storage",
                "SELECT DeviceId, MediaType FROM MSFT_PhysicalDisk"))
            {
                foreach (ManagementObject disk in searcher.Get())
                {
                    var deviceId = disk["DeviceId"]?.ToString();
                    var mediaType = disk["MediaType"];

                    if (deviceId != null && mediaType != null)
                    {
                        // MediaType: 3 = HDD, 4 = SSD
                        bool isSSD = Convert.ToUInt16(mediaType) == 4;
                        if (uint.TryParse(deviceId, out uint diskNumber))
                        {
                            diskMediaTypes[diskNumber] = isSSD;
                        }
                    }
                }
            }

            // Agora, mapear partições para letras de drive
            using (var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_DiskDrive"))
            {
                foreach (ManagementObject disk in searcher.Get())
                {
                    var diskIndex = Convert.ToUInt32(disk["Index"]);
                    bool isSSD = diskMediaTypes.ContainsKey(diskIndex) && diskMediaTypes[diskIndex];

                    // Obter partições deste disco
                    using (var partitionSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{disk["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition"))
                    {
                        foreach (ManagementObject partition in partitionSearcher.Get())
                        {
                            // Obter volumes lógicos desta partição
                            using (var volumeSearcher = new ManagementObjectSearcher(
                                $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition"))
                            {
                                foreach (ManagementObject volume in volumeSearcher.Get())
                                {
                                    var deviceId = volume["DeviceID"]?.ToString();
                                    if (!string.IsNullOrEmpty(deviceId) && isSSD)
                                    {
                                        ssdLetters.Add(deviceId.Replace(":", "").ToUpper());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Se falhar (ex: WMI não disponível), retorna vazio
            // O app continuará funcionando, apenas não detectará SSDs
        }

        return ssdLetters;
    }

    /// <summary>
    /// Verifica se o aplicativo está rodando como Administrador
    /// </summary>
    public static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
