using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

    public class FlightSimulatorInstalInfo
    {
        public FlightSimulatorOrigin flightSimulatorOrigin;
        public string instalLocation = "";
        public string communityFolder = "";
        public bool isRunning = false;
    }
    
    //  FlightSimulatorInstal is a static helper class to determine the location of the Flight Simulator installation.
    public static class FlightSimulatorInstal
    {
        //  getInfo returns a FlightSimulatorInstalInfo object containing details of the Flight Simulator installation.
        //  If x86Platform is true, the 32-bit registry is searched, otherwise the 64-bit registry is searched.
        //  If the Flight Simulator installation is not found, null is returned.
        public static FlightSimulatorInstalInfo getInfo(bool x86Platform = false)
        {
            string matchDisplayName = "MICROSOFT FLIGHT SIMULATOR";
            string uninstallKey = string.Empty;
            if (x86Platform) uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            else uninstallKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(uninstallKey))
            {
                foreach (string skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        if (sk != null && sk.GetValue("DisplayName") != null && sk.GetValue("DisplayName").ToString().ToUpper().Equals(matchDisplayName))
                        {
                            if (sk.GetValue("installLocation") != null)
                            {
                                FlightSimulatorInstalInfo flightSimulatorInstalInfo = new FlightSimulatorInstalInfo();
                                flightSimulatorInstalInfo.instalLocation = sk.GetValue("installLocation").ToString();

                                //  Let's also see if the sim is currently running
                                flightSimulatorInstalInfo.isRunning = isRunning();//  (System.Diagnostics.Process.GetProcessesByName("FlightSimulator").Length > 0);

                                if (flightSimulatorInstalInfo.instalLocation.IndexOf("steamapps") > -1)
                                {
                                    string steamCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Roaming\Microsoft Flight Simulator\Packages\Community");
                                    if (Directory.Exists(steamCommunityPath))
                                    {
                                        flightSimulatorInstalInfo.communityFolder = steamCommunityPath;
                                        flightSimulatorInstalInfo.flightSimulatorOrigin = FlightSimulatorOrigin.Steam;
                                        return flightSimulatorInstalInfo;
                                    }
                                }

                                string xboxCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Local\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\Packages\Community");
                                if (Directory.Exists(xboxCommunityPath))
                                {
                                    flightSimulatorInstalInfo.communityFolder = xboxCommunityPath;
                                    flightSimulatorInstalInfo.flightSimulatorOrigin = FlightSimulatorOrigin.XBox;
                                    return flightSimulatorInstalInfo;
                                }
                                
                                string retailCommunityPath = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Local\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\Packages\Community");
                                if (Directory.Exists(retailCommunityPath))
                                {
                                    flightSimulatorInstalInfo.communityFolder = retailCommunityPath;
                                    flightSimulatorInstalInfo.flightSimulatorOrigin = FlightSimulatorOrigin.Retail;
                                    return flightSimulatorInstalInfo;
                                }

                                flightSimulatorInstalInfo.communityFolder = Path.Combine(flightSimulatorInstalInfo.instalLocation, "/Community");
                                flightSimulatorInstalInfo.flightSimulatorOrigin = FlightSimulatorOrigin.Custom;
                                return flightSimulatorInstalInfo;
                            }
                        }
                    }
                }
            }
            return null;
        }

        //  isRunning returns true if Flight Simulator is currently running as a process named FlightSimulator.
        public static bool isRunning()
        {
            return (System.Diagnostics.Process.GetProcessesByName("FlightSimulator").Length > 0);
        }
    }
}