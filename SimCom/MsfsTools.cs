using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;
using Newtonsoft.Json;


//  SimCom is a wrapper around WASimCommander and SimConnect designed to make the API easier to use.
//  Variables and events are interacted with using the SimVal class.
//  SimCom is a work in progress and is not yet ready for production use.
//  SimCom is released under the MIT license.
//
//  https://github.com/dinther/SimCom
//  SimCom is written by Paul van Dinther.

namespace SimComLib
{
    //  Enumeration for the various possible Flight Simulator installation origins.
    //  This information is important to determine the location of the Community folder
    public enum MSFSOrigin
    {
        Steam,
        XBox,
        Retail,
        Custom
    }

    public enum MSFSCheckResult
    {
        Present,
        Installed,
        RestartRequired,
        CommunityFolderNotFound,
        RegistryEntryNotFound,
        MSFSNotFound,
        ModuleNoFound,
        SourceModuleNotFound,
        ModuleUpdateRequired,
        ManifestNotFound,
        LayoutNotFound,
        LayoutIntegrityCheckFailed,
        InstallFailed,
        ModulePresent
    }

    public class MSFSInfo
    {
        public MSFSOrigin msfsOrigin;
        public string installLocation = "";
        public string communityFolder = "";
    }

    //  MsfsTools is a static helper class that provides tools to verify and install community modules.
    public static class MSFSTools
    {
        //  getInfo returns a FlightSimulatorInstalInfo object containing details of the Flight Simulator installation.
        //  If x86Platform is true, the 32-bit registry is searched, otherwise the 64-bit registry is searched.
        //  If the Flight Simulator installation is not found, null is returned.
        public static MSFSInfo getInfo(bool x86Platform = true)
        {
            string matchDisplayName = "MICROSOFT FLIGHT SIMULATOR";
            string uninstallKey = string.Empty;
            if (x86Platform) uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            else uninstallKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

            RegistryKey rk = Registry.LocalMachine.OpenSubKey(uninstallKey);
            
            foreach (string skName in rk.GetSubKeyNames())
            {
                using (RegistryKey sk = rk.OpenSubKey(skName))
                {
                    if (sk != null && sk.GetValue("DisplayName") != null && sk.GetValue("DisplayName").ToString().ToUpper().Equals(matchDisplayName))
                    {
                        if (sk.GetValue("installLocation") != null)
                        {
                            MSFSInfo msfsInstallInfo = new MSFSInfo();
                            msfsInstallInfo.installLocation = sk.GetValue("installLocation").ToString();

                            if (msfsInstallInfo.installLocation.IndexOf("steamapps") > -1)
                            {
                                string steamCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Roaming\Microsoft Flight Simulator\Packages\Community");
                                if (Directory.Exists(steamCommunityPath))
                                {
                                    msfsInstallInfo.communityFolder = steamCommunityPath;
                                    msfsInstallInfo.msfsOrigin = MSFSOrigin.Steam;
                                    return msfsInstallInfo;
                                }
                            }

                            string xboxCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Local\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\Packages\Community");
                            if (Directory.Exists(xboxCommunityPath))
                            {
                                msfsInstallInfo.communityFolder = xboxCommunityPath;
                                msfsInstallInfo.msfsOrigin = MSFSOrigin.XBox;
                                return msfsInstallInfo;
                            }
                                
                            string retailCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Local\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\Packages\Community");
                            if (Directory.Exists(retailCommunityPath))
                            {
                                msfsInstallInfo.communityFolder = retailCommunityPath;
                                msfsInstallInfo.msfsOrigin = MSFSOrigin.Retail;
                                return msfsInstallInfo;
                            }

                            msfsInstallInfo.communityFolder = Path.Combine(msfsInstallInfo.installLocation, "/Community");
                            msfsInstallInfo.msfsOrigin = MSFSOrigin.Custom;
                            return msfsInstallInfo;
                        }
                    }
                }
            }
            return null;
        }

        //  Obtains the networkConfigId from the client_conf.ini file in the this application's folder.
        public static int getConfigIndex()
        {
            string clientConfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_conf.ini");
            if (File.Exists(clientConfPath))
            {
                string[] lines = File.ReadAllLines(clientConfPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("networkConfigId"))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            return int.Parse(parts[1].Trim());
                        }
                    }
                }
            }
            return -1;
        }

        //  Obtains the manifest from the given file as a ModuleManifest object.
        //  The major_Version, minor_Version and revision fields are also set from the versionData field for easier access.
        public static ModuleManifest loadManifestFromFile(string filePath)
        {
            ModuleManifest manifest = null;
            if (File.Exists(filePath))
            {
                manifest = JsonConvert.DeserializeObject<ModuleManifest>(File.ReadAllText(@filePath), new JsonSerializerSettings
                {
                    MaxDepth = 8
                });
                string[] versionData = manifest.package_version.Split('.');
                if (versionData.Length == 2)
                {
                    manifest.major_Version = int.Parse(versionData[0]);
                    manifest.minor_Version = int.Parse(versionData[1]);
                    manifest.revision = int.Parse(versionData[2]);
                }
            }
            return manifest;
        }

        //  Obtains the manifest from the given file as a ModuleManifest object.
        //  The major_Version, minor_Version and revision fields are also set from the versionData field for easier access.
        public static ModuleLayout loadLayoutFromFile(string filePath)
        {
            ModuleLayout moduleLayout = null;
            if (File.Exists(filePath))
            {
                moduleLayout = JsonConvert.DeserializeObject<ModuleLayout>(File.ReadAllText(@filePath), new JsonSerializerSettings
                {
                    MaxDepth = 8
                });
            }
            return moduleLayout;
        }

        //  checks to confirm that all files listed in the layout are present in the module folder.
        public static bool CheckLayoutIntegrity(ModuleLayout layout, string moduleFolder)
        {
            foreach (ModuleLayoutContentItem item in layout.content)
            {
                string itemPath = Path.Combine(moduleFolder, item.path);
                if (File.Exists(itemPath))
                {
                    FileInfo fileInfo = new FileInfo(itemPath);
                    if (fileInfo.Length != item.size)
                    {
                        return false;
                    }
                    if (fileInfo.LastWriteTime != new System.DateTime(item.date))
                    {
                        return false;
                    }
                } else
                {
                    return false;
                }
            }
            return true;
        }

        //  installModule installs a module into the Community folder of the Flight Simulator installation.
        //  moduleName is assumed to be a folder name in the same folder as the executable.
        //  All files in the folder and subfolders are copied to the Community folder if newer.
        public static MSFSCheckResult installModule(string moduleName)
        {
            MSFSInfo fsInfo = getInfo(true);
            if (fsInfo == null) getInfo(false);
            if (fsInfo == null)
            {
                Console.WriteLine("Registry search failed.");
                return MSFSCheckResult.InstallFailed;
            }
            else
            {
                if (fsInfo.installLocation == "")
                {
                    Console.WriteLine("Flight Simulator installation not found.");
                    return MSFSCheckResult.MSFSNotFound;
                }
                if (fsInfo.communityFolder == "")
                {
                    Console.WriteLine("Community folder not found.");
                    return MSFSCheckResult.CommunityFolderNotFound;
                }
                string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, moduleName);
                string destinationPath = Path.Combine(fsInfo.communityFolder, moduleName);
                bool copiedFiles = CopyFolder(sourcePath, destinationPath);
                return copiedFiles && SimIsRunning() ? MSFSCheckResult.RestartRequired : MSFSCheckResult.Installed;
            }
        }

        //  Compares the version of the existing module with the version of the new module.
        //  Returns 0 if the versions are the same, 1 if the existing module is newer, -1 if the new module is newer.
        public static int CompareManifestVersion(ModuleManifest existingModule, ModuleManifest newModule)
        {
            if (existingModule == null || newModule == null) return 0;
            if (existingModule.major_Version > newModule.major_Version) return 1;
            if (existingModule.major_Version < newModule.major_Version) return -1;
            if (existingModule.minor_Version > newModule.minor_Version) return 1;
            if (existingModule.minor_Version < newModule.minor_Version) return -1;
            if (existingModule.revision > newModule.revision) return 1;
            if (existingModule.revision < newModule.revision) return -1;
            return 0;
        }

        private static bool CopyFolder(string sourceFolder, string destinationFolder)
        {
            bool copiedFiles = false;
            if (!Directory.Exists(sourceFolder))
            {
                throw new DirectoryNotFoundException($"Source folder not found: {sourceFolder}");
            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            DirectoryInfo sourceDir = new DirectoryInfo(sourceFolder);
            DirectoryInfo destinationDir = new DirectoryInfo(destinationFolder);

            foreach (FileInfo file in sourceDir.GetFiles())
            {
                string destinationFilePath = Path.Combine(destinationDir.FullName, file.Name);
                if (File.Exists(destinationFilePath))
                {
                    FileInfo existingFile = new FileInfo(destinationFilePath);
                    if (file.LastWriteTime > existingFile.LastWriteTime)
                    {
                        file.CopyTo(destinationFilePath, true);
                        copiedFiles = true;
                    }
                }
                else
                {
                    file.CopyTo(destinationFilePath, true);
                    copiedFiles = true;
                }
            }

            foreach (DirectoryInfo subDir in sourceDir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir.FullName, subDir.Name);
                copiedFiles = CopyFolder(subDir.FullName, newDestinationDir);
            }
            return copiedFiles;
        }

        //  isRunning returns true if Flight Simulator is currently running as a process named FlightSimulator.
        public static bool SimIsRunning()
        {
            return (System.Diagnostics.Process.GetProcessesByName("FlightSimulator").Length > 0);
        }
    }
}