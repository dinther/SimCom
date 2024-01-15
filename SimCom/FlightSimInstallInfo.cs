using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;


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
    public enum FlightSimulatorOrigin
    {
        Steam,
        XBox,
        Retail,
        Custom
    }

    public enum ModuleInstallResult
    {
        Present,
        Installed,
        RestartRequired,
        CommunityFolderNotFound,
        FlightSimulatorNotFound,
        Failed,
    }

    public class FlightSimulatorInstallInfo
    {
        public FlightSimulatorOrigin flightSimulatorOrigin;
        public string installLocation = "";
        public string communityFolder = "";
        public bool isRunning = false;
    }
    
    //  FlightSimulatorInstal is a static helper class to determine the location of the Flight Simulator installation.
    public static class FlightSimulatorInstal
    {
        //  getInfo returns a FlightSimulatorInstalInfo object containing details of the Flight Simulator installation.
        //  If x86Platform is true, the 32-bit registry is searched, otherwise the 64-bit registry is searched.
        //  If the Flight Simulator installation is not found, null is returned.
        public static FlightSimulatorInstallInfo getInfo(bool x86Platform = true)
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
                            FlightSimulatorInstallInfo flightSimulatorInstallInfo = new FlightSimulatorInstallInfo();
                            flightSimulatorInstallInfo.installLocation = sk.GetValue("installLocation").ToString();

                            //  Let's also see if the sim is currently running
                            flightSimulatorInstallInfo.isRunning = isRunning();//  (System.Diagnostics.Process.GetProcessesByName("FlightSimulator").Length > 0);

                            if (flightSimulatorInstallInfo.installLocation.IndexOf("steamapps") > -1)
                            {
                                string steamCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Roaming\Microsoft Flight Simulator\Packages\Community");
                                if (Directory.Exists(steamCommunityPath))
                                {
                                    flightSimulatorInstallInfo.communityFolder = steamCommunityPath;
                                    flightSimulatorInstallInfo.flightSimulatorOrigin = FlightSimulatorOrigin.Steam;
                                    return flightSimulatorInstallInfo;
                                }
                            }

                            string xboxCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Local\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\Packages\Community");
                            if (Directory.Exists(xboxCommunityPath))
                            {
                                flightSimulatorInstallInfo.communityFolder = xboxCommunityPath;
                                flightSimulatorInstallInfo.flightSimulatorOrigin = FlightSimulatorOrigin.XBox;
                                return flightSimulatorInstallInfo;
                            }
                                
                            string retailCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Local\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\Packages\Community");
                            if (Directory.Exists(retailCommunityPath))
                            {
                                flightSimulatorInstallInfo.communityFolder = retailCommunityPath;
                                flightSimulatorInstallInfo.flightSimulatorOrigin = FlightSimulatorOrigin.Retail;
                                return flightSimulatorInstallInfo;
                            }

                            flightSimulatorInstallInfo.communityFolder = Path.Combine(flightSimulatorInstallInfo.installLocation, "/Community");
                            flightSimulatorInstallInfo.flightSimulatorOrigin = FlightSimulatorOrigin.Custom;
                            return flightSimulatorInstallInfo;
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

        //  installModule installs a module into the Community folder of the Flight Simulator installation.
        //  moduleName is assumed to be a folder name in the same folder as the executable.
        //  All files in the folder and subfolders are copied to the Community folder if newer.
        public static ModuleInstallResult installModule(string moduleName)
        {
            FlightSimulatorInstallInfo fsInfo = getInfo(true);
            if (fsInfo == null) getInfo(false);
            if (fsInfo == null)
            {
                Console.WriteLine("Registry search failed.");
                return ModuleInstallResult.Failed;
            }
            else
            {
                if (fsInfo.installLocation == "")
                {
                    Console.WriteLine("Flight Simulator installation not found.");
                    return ModuleInstallResult.FlightSimulatorNotFound;
                }
                if (fsInfo.communityFolder == "")
                {
                    Console.WriteLine("Community folder not found.");
                    return ModuleInstallResult.CommunityFolderNotFound;
                }
                string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, moduleName);
                string destinationPath = Path.Combine(fsInfo.communityFolder, moduleName);
                bool copiedFiles = CopyFolder(sourcePath, destinationPath);
                return copiedFiles && isRunning() ? ModuleInstallResult.RestartRequired : ModuleInstallResult.Installed;
            }
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
        public static bool isRunning()
        {
            return (System.Diagnostics.Process.GetProcessesByName("FlightSimulator").Length > 0);
        }
    }
}