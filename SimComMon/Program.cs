using SimComLib;
using System.Threading;

//  SimComMon is a command line demo app to showcase the SimCom library
//  based on WASimCommander https://github.com/mpaperno/WASimCommander
//  
//  
//  
//  SimComMon.exe -A:NAV OBS:1,degrees,25,1 "HEADING INDICATOR,degrees, 25, 0.1" "TITLE,string"
//
//  splitVariableName The variableName string consist of 7 parts: VarType:Name:Index,Units,ValueType,Interval,deltaEpsilon
//
//  Example: "A:NAV OBS:1,degrees,FLOAT,1000,0.1"
//
//  VarType         Optional. Char    - Defines the variable VarType.
//  Name            Required. String  - Name of the Variable or Full Reverse Polish Notation calculations.
//  Index           Optional. Integer - Some variables use an additional index. For example: A:NAV OBS:1 (Nav radio 1)
//  Units           Optional. String  - Units of the variable. For example: degrees, feet, knots, (See Simconnect SDK). Default units is "NUMBER"
//  ValueType       Optional. String  - Type of the value returned. INT8, INT16, INT32, INT64, FLOAT, DOUBLE. Default type is DOUBLE
//  Interval        Optional. Integer - Interval in milliseconds to monitor the variable. Default is 0 (The variable is read once)
//  deltaEpsilon    Optional. Float   - The minimum change in value to trigger a notification. Default is 0 (Any change in value triggers a notification)
//
//  All types of variables can be monitored. such as A: variables, K: variables, L: variables, etc.
//  https://github.com/dinther/SimCom
//  SimCom is written by Paul van Dinther.

//  Ensure WASimcommander module is installed in the community folder
switch (FlightSimulatorInstal.installModule("wasimcommander-module"))
{
    case ModuleInstallResult.CommunityFolderNotFound: Console.WriteLine("Microsoft Flight Simulator Community folder not found."); break;
    case ModuleInstallResult.FlightSimulatorNotFound: Console.WriteLine("Microsoft Flight Simulator not found."); break;
    case ModuleInstallResult.RestartRequired: Console.WriteLine("WASimCommander Module installed. Restart Flight Simulator to activate."); break;
    case ModuleInstallResult.Installed: Console.WriteLine("WASimCommander Module installed."); break;
    case ModuleInstallResult.Failed: Console.WriteLine("WASimCommander Module installation failed."); break;
}

SimCom simCom = new SimCom(1995);
simCom.OnDataChanged += SimCom_OnDataChanged;
simCom.OnConnection += SimCom_OnConnection;
simCom.OnLogEvent += SimCom_OnLogEvent;

void SimCom_OnLogEvent(SimCom SimCom, LogEventArgs LogData)
{
    Console.WriteLine($"{LogData.Time.ToString("MM-dd:HH:mm:ssf")}: {LogData.LogText}");
}

bool monitor = true;
simCom.Connect();


while (simCom.Connection_Status == SimCom_Connection_Status.NOT_CONNECTED || monitor)
{

}

void SimCom_OnConnection(SimCom simCom, SimCom_Connection_Status Connection_Status)
{
    if (Connection_Status == SimCom_Connection_Status.CONNECTED)
    {
        Console.WriteLine($"Connection=OK ConfigIndex={simCom.ConfigIndex}\n");
        string arguments = string.Join(" ", args).Replace("--", "|");
        string[] valueDefs = arguments.Split('|');
        if (valueDefs.Length < 2)
        {
            Console.WriteLine($"Incorrect parameters.\n");
            return;
        }
        bool needMonitor = false;
        for (int i = 1; i < valueDefs.Length; i++)
        {
            string name = "";
            bool asAlias = false;
            bool equals = false;
            string alias = "";
            string value = "";
            string valueDef = valueDefs[i].Trim().Replace(" as ", "|as|").Replace(" AS ", "|as|").Replace("=", "|=|");
            if (valueDef != "")
            {
                equals = valueDef.Contains("|=|");
                asAlias = valueDef.Contains("|as|");
                string[] valueParams = valueDef.Split('|');
                if (valueParams.Length > 0)
                {
                    name = valueParams[0].Trim();
                }

                if (valueParams.Length == 3 && equals && !asAlias)
                {
                    value = valueParams[2].Trim();
                }

                if (valueParams.Length == 3 && !equals && asAlias)
                {
                    alias = valueParams[2].Trim();
                }

                if (valueParams.Length == 5)
                {
                    alias = valueParams[2].Trim(); value = valueParams[4].Trim();
                }

                SimVal simVal = simCom.GetVariable(name, alias, null);
                if (value != "")
                {
                    if (simVal.Units == "STRING") simCom.SetVariable(simVal, value);
                    else simCom.SetVariable(simVal, System.Convert.ToDouble(value));
                }

                if (simVal.Interval > 0)
                {
                    needMonitor = true;
                }
            }
        }
        monitor = needMonitor;
    } else
    {
        Console.WriteLine($"Connection=Fail ConfigIndex={simCom.ConfigIndex}\n");
    }
}

void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
{
    string valName = simVal.Alias == "" ? simVal.Name : simVal.Alias;
    Console.WriteLine($"{valName}={simVal.Value}");
}

