using System.Diagnostics;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;
using WASimCommander.CLI.Client;
using System.Xml.Linq;
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
    public delegate void SimComDataEventHandler(SimCom SimCom, SimVal SimVal);
    public delegate void SimComLogEventHandler(SimCom SimCom, LogEventArgs LogData);

    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(SimCom_Log_Level LogLevel, string LogText, int LineNumber, string Caller, string SourceFile)
        {
            this.Log_Level = LogLevel;
            this.Time = DateTime.Now;
            this.LogText = LogText;
            this.LineNumber = LineNumber;
            this.Caller = Caller;
            this.SourceFile = SourceFile;
        }
        public SimCom_Log_Level Log_Level { get; }
        public DateTime Time { get; }
        public string LogText { get; }
        public int LineNumber { get; }
        public string Caller { get; }
        public string SourceFile { get; }
    }

    public enum SimCom_Log_Level : byte
    {
        None=0,       //  Disables logging
        Critical=1,   //  Events which cause termination.
        Error=2,      //  Hard errors preventing function execution.
        Warning=3,    //  Possible anomalies which do not necessarily prevent execution.
        Info=4,       //  Informational messages about key processes like startup and shutdown.
        Debug=5,      //  Verbose debugging information.
        Trace=6       //  Very verbose and frequent debugging data, do not use with "slow" logger outputs.
    }

    public enum SimCom_Connection_Status : byte
    {
        NOT_CONNECTED = 0,
        CONNECTED = 1,
        CONNECTION_FAILED = 2,
    }

    public enum WaSim_ValueTypes : uint
    {
        INT8 = uint.MaxValue,
        INT16 = 4294967294,
        INT32 = 4294967293,
        INT64 = 4294967292,
        FLOAT = 4294967291,
        DOUBLE = 4294967290,
        STRING = 32
    }

    public class WaSim_Version
    {
        public WaSim_Version(UInt32 version)
        {
            Client_Major = version >> 24;
            Client_Minor = (version >> 16) & 0xFF;
            Server_Major = (version >> 8) & 0xFF;
            Server_Minor = version & 0xFF;
        }
        public uint Client_Major { get; } = 0;
        public uint Client_Minor { get; } = 0;
        public uint Server_Major { get; } = 0;
        public uint Server_Minor { get; } = 0;
    }

    public class SimCom_Exception : Exception
    {
        public SimCom_Exception(string message, HR hr) : base(message)  // Enum needs a unknown or none option for a default value
        {
            HR = hr;
        }
        public HR HR { get; }
    }

    public delegate void SimCoMConnectHandler(SimCom simCom, SimCom_Connection_Status Connection_Status);

    public class SimCom
    {
        private WASimClient _client;
        private uint _clientID;
        private int _configIndex;
        private SimConnectEventReceiver _simConnectEventReceiver;
        private uint definitionIndex = 0;
        private Dictionary<uint, SimVal> simValIDs = new Dictionary<uint, SimVal>();
        private Dictionary<string, SimVal> simValNames = new Dictionary<string, SimVal>();
        //private Dictionary<string, SimVal> simValFullNames = new Dictionary<string, SimVal>();
        private SimCom_Connection_Status _connection_Status = SimCom_Connection_Status.NOT_CONNECTED;
        public SimCom_Connection_Status Connection_Status
        {
            get
            {
                return _connection_Status;
            }
        }
        public static SimCom_Log_Level StringToLogLevel(string value) {
            switch (value.ToLower())
            {
                case "none": return SimCom_Log_Level.None;
                case "critical": return SimCom_Log_Level.Critical;
                case "error": return SimCom_Log_Level.Error;
                case "warning": return SimCom_Log_Level.Warning;
                case "info": return SimCom_Log_Level.Info;
                case "debug": return SimCom_Log_Level.Debug;
                case "trace": return SimCom_Log_Level.Trace;
                default: return SimCom_Log_Level.None;
            }
        }
        public static string LogLevelToString(SimCom_Log_Level logLevel)
        {
            switch (logLevel)
            {
                case SimCom_Log_Level.None: return "NON";
                case SimCom_Log_Level.Critical: return "CRI";
                case SimCom_Log_Level.Error: return "ERR";
                case SimCom_Log_Level.Warning: return "WAR";
                case SimCom_Log_Level.Info: return "INF";
                case SimCom_Log_Level.Debug: return "DEB";
                case SimCom_Log_Level.Trace: return "TRA";
                default: return "NON";
            }
        }
        public int ConfigIndex { get { return _configIndex; } }
        public event SimCoMConnectHandler OnConnection;
        private WaSim_Version _version;
        public WaSim_Version Version { get { return _version;} }
        public WASimClient WASimclient { get { return _client; } }
        public SimCom(uint clientID)
        {
            _clientID = clientID;
            _configIndex = FlightSimulatorInstal.getConfigIndex();
            _client = new WASimClient(_clientID);
            _client.OnClientEvent += _client_OnClientEvent;
            _client.OnDataReceived += _client_OnDataReceived;

            _simConnectEventReceiver = new SimConnectEventReceiver(_clientID, _configIndex);
            _simConnectEventReceiver.OnEvent += _eventReceiver_OnEvent;
            _simConnectEventReceiver.OnConnection += _eventReceiver_OnConnection;
            _simConnectEventReceiver.OnLogEvent += _simConnectEventReceiver_OnLogEvent;
        }

        private void _simConnectEventReceiver_OnLogEvent(SimConnectEventReceiver simConnecteventReceiver, LogEventArgs LogData)
        {
            OnLogEvent?.DynamicInvoke(this, LogData);
        }

        private bool setConnectionStatus(SimCom_Connection_Status connection_Status)
        {
            if (connection_Status != _connection_Status)
            {
                _connection_Status = connection_Status;
                OnConnection?.Invoke(this, _connection_Status);
            }
            return connection_Status == SimCom_Connection_Status.CONNECTED;
        }

        private void _eventReceiver_OnConnection(SimConnectEventReceiver EventReceiver, bool connected, EventArgs e)
        {
            if (connected) setConnectionStatus(SimCom_Connection_Status.CONNECTED);
            else
            {
                setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED);
            }
        }

        public bool Connect()
        {
            if (_connection_Status != SimCom_Connection_Status.CONNECTED) {
                HR hr;
                UInt32 version = 0;
                _connection_Status = SimCom_Connection_Status.NOT_CONNECTED;
                if ((hr = _client.connectSimulator()) == HR.OK)
                {
                    if ((version = _client.pingServer()) != 0)
                    {
                        _version = new WaSim_Version(version);
                        if ((hr = _client.connectServer()) == HR.OK)
                        {
                            _simConnectEventReceiver.Connect();
                            
                        } else return setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED);
                    } else return setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED);
                } else return setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED);
            }
            return true;
        }

        public void disconnect()
        {
            _simConnectEventReceiver.Disconnect();
            _client.disconnectServer();
            _client.disconnectSimulator();
            _client.Dispose();
            setConnectionStatus(SimCom_Connection_Status.NOT_CONNECTED);
        }


        //  GetVariable(...)
        //  VarType         Optional. Char    - Defines the variable VarType.
        //  Name            Required. String  - Name of the Variable or Full Reverse Polish Notation calculations.
        //  Index           Optional. Integer - Some variables use an additional index. For example: A:NAV OBS:1 (Nav radio 1)
        //  Units           Optional. String  - Units of the variable. For example: degrees, feet, knots, (See Simconnect SDK). Default units is "NUMBER"
        //  ValueType       Optional. String  - Type of the value returned. INT8, INT16, INT32, INT64, FLOAT, DOUBLE. Default type is DOUBLE
        //  Interval        Optional. Integer - Interval in milliseconds to monitor the variable. Default is 0 (The variable is read once)
        //  deltaEpsilon    Optional. Double  - The minimum change in value to trigger a notification. Default is 0 (Any change in value triggers a notification)
        //  Alias           Optional. String - Alias for the variable. Default is the variableName.Name
        //  Default         Optional. Dynamic - Default value for the variable. Default is null (0 for numbers, "" for strings)
        /*
        public SimVal GetVariable(char VarType, string Name, int Index, string Units, string ValueType, int Interval, double deltaEpsilon, string Alias = "", dynamic Default = null)
        {
            SimVal simVal;
            string fullName = $"{VarType}:{Name}";
            if (Index > 0 && Index < 255) fullName += ":" + Index.ToString();  //  _indexes are never 255 (I think)

            try
            {
                simVal = simValNames[lookupSimVal.FullName];
            }
            catch (KeyNotFoundException)
            {
                simVal = lookupSimVal;
                //Different ways to look up
                simValIDs.Add(definitionIndex, simVal);
                simValNames.Add(simVal.FullName, simVal);
                //simValFullNames.Add(simVal.FullName, simVal);
                init = true;
            }

            return null;
        }
        */

        //  GetVariable The variableName string consist of 7 parts: VarType:Name:Index,Units,ValueType,Interval,deltaEpsilon
        //
        //  Example: "A:NAV OBS:1,degrees,type,1000,0.1"
        //
        //  VarType         Optional. Char    - Defines the variable VarType.
        //  Name            Required. String  - Name of the Variable or Full Reverse Polish Notation calculations.
        //  Index           Optional. Integer - Some variables use an additional index. For example: A:NAV OBS:1 (Nav radio 1)
        //  Units           Optional. String  - Units of the variable. For example: degrees, feet, knots, (See Simconnect SDK). Default units is "NUMBER"
        //  ValueType       Optional. String  - Type of the value returned. INT8, INT16, INT32, INT64, FLOAT, DOUBLE. Default type is DOUBLE
        //  Interval        Optional. Integer - Interval in milliseconds to monitor the variable. Default is 0 (The variable is read once)
        //  deltaEpsilon    Optional. Double  - The minimum change in value to trigger a notification. Default is 0 (Any change in value triggers a notification)
        //
        //  Alias           Optional. String - Alias for the variable. Default is the variableName.Name
        //  Default         Optional. Dynamic - Default value for the variable. Default is null (0 for numbers, "" for strings)
        public SimVal GetVariable(string variableName, string Alias="", dynamic Default=null)
        {
            SimVal simVal;
            bool init = false;
            SimVal lookupSimVal = new SimVal(this, variableName, definitionIndex, Alias, Default);
            lookupSimVal.OnChanged += SimVal_OnChanged;
            try
            {
                simVal = simValNames[lookupSimVal.FullName];
            }
            catch (KeyNotFoundException)
            {
                simVal = lookupSimVal;
                //Different ways to look up
                simValIDs.Add(definitionIndex, simVal);
                simValNames.Add(simVal.FullName, simVal);
                //simValFullNames.Add(simVal.FullName, simVal);
                init = true;
            }

            if (simVal.VarType != 'K' && simVal.Interval != 0)
            {
                UpdatePeriod updatePeriod = UpdatePeriod.Millisecond;
                char VarType = (simVal.VarType == 'A' && simVal.Units != "") ? '\0' : simVal.VarType;

                DataRequest? dataRequest = null;
                HR hr;
                if (simVal.IsRPN)
                {
                    dataRequest = new DataRequest(
                        requestId: definitionIndex,
                        resultType: CalcResultType.Double,
                        calculatorCode: simVal.FullName,
                        valueSize: (uint)WaSim_ValueTypes.DOUBLE,
                        period: updatePeriod,
                        interval: Math.Max(simVal.Interval, 25),
                        deltaEpsilon: (float)Math.Max(0, simVal.DeltaEpsilon)
                    );
                }
                /*
                else if (simVal.Units == "STRING")
                {
                    dataRequest = new DataRequest(
                        requestId: definitionIndex,
                        resultType: CalcResultType.String,
                        calculatorCode: $"({simVal.FullName})",
                        valueSize: 32,
                        period: updatePeriod,
                        interval: Math.Max(simVal.Interval, 25),
                        deltaEpsilon: 0.0f
                    );
                }
                */
                else if (VarType == '\0')
                {
                    dataRequest = new DataRequest(
                        requestId: definitionIndex,
                        simVarName: simVal.Name,
                        unitName: simVal.Units,
                        simVarIndex: simVal.Index == 255 ? (Byte)0 : simVal.Index,
                        valueSize: (uint)simVal.ValueType,
                        period: updatePeriod,
                        interval: Math.Max(simVal.Interval, 25),
                        deltaEpsilon: (float)Math.Max(0, simVal.DeltaEpsilon)
                    );
                }
                else
                {
                    dataRequest = new DataRequest(
                        requestId: definitionIndex,
                        variableType: VarType,
                        variableName: simVal.Name,
                        valueSize: (uint)simVal.ValueType,//WaSim_ValueTypes.DOUBLE,//4294967291,
                        period: updatePeriod,
                        interval: Math.Max(simVal.Interval, 25),
                        deltaEpsilon: (float)Math.Max(0, simVal.DeltaEpsilon)
                    );
                }

                hr = _client.saveDataRequest(dataRequest);
                switch (hr)
                {
                    case HR.OK: break;
                    case HR.NOT_CONNECTED: setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED); break;
                    default: throw new SimCom_Exception($"Failed to subscribe to {VarType}:{simVal.Name} {hr.ToString()}", hr);
                }                  
            }
            definitionIndex++;

            if (simVal.VarType == 'K')
            {
                _simConnectEventReceiver.RegisterSimEvent(simVal);
            }

            getVariableValue(simVal);

            return simVal;
        }

        private void SimVal_OnChanged(SimCom SimCom, SimVal SimVal)
        {
            this.OnDataChanged?.Invoke(this, SimVal);
        }

        public SimVal SetVariable(SimVal simVal, dynamic value)
        {
            HR hr;
            if (simVal.Units == "STRING")
            {
                // WASimCommander can't set strings yet.
            }
            else if (!simVal.IsRPN)
            {
                hr = _client.setVariable(simVal.VariableRequest, value);
                switch (hr)
                {
                    case HR.OK: break;
                    case HR.NOT_CONNECTED: setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED); break;
                    default: throw new SimCom_Exception($"Failed to set variable {simVal.FullName} {hr.ToString()}", hr);
                }
            } else
            {
                this.Calc(simVal.FullName);
            }
            return simVal;
        }

        public event SimComDataEventHandler OnDataChanged;
        public event SimComLogEventHandler OnLogEvent;

        private LookupItemType variableTypeToLookupItemType(char variableType)
        {
            switch (variableType)
            {
                case 'A': return LookupItemType.SimulatorVariable;
                case 'B': return LookupItemType.RegisteredEvent;
                case 'K': return LookupItemType.KeyEventId;
                case 'L': return LookupItemType.LocalVariable;
                    // not sure if this makes sense
            }
            return LookupItemType.None;
        }

        private VariableRequest createVariable(char variableType, string name, string units)
        {
            HR hr = _client.lookup(variableTypeToLookupItemType(variableType), name, out var localVarId);
            switch (hr)
            {
                case HR.OK: break;
                case HR.NOT_CONNECTED: setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED); break;
                default: throw new SimCom_Exception($"Failed to lookup Simulator variable {name}. {hr.ToString()}", hr);
            }
            return new VariableRequest(variableType, name, units);
        }

        public bool getVariableValue(SimVal simVal)
        {
            HR hr;
            if (simVal.IsRPN)
            {
                var answer = Calc(simVal.Name);
                simVal.SetValue(answer);
            }
            else
            {
                if (simVal.VarType != 'K')
                {
                    if (simVal.Units == "STRING")
                    {
                        hr = _client.getVariable(simVal.VariableRequest, out string varResult);
                        switch (hr)
                        {
                            case HR.OK: break;
                            case HR.NOT_CONNECTED: setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED); break;
                            default: throw new SimCom_Exception($"Failed to get variable {simVal.VariableRequest.variableName} {hr.ToString()}", hr);
                        }
                        simVal.SetValue(varResult);
                    }
                    else
                    {
                        hr = _client.getVariable(simVal.VariableRequest, out double varResult);
                        switch (hr)
                        {
                            case HR.OK: break;
                            case HR.NOT_CONNECTED: setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED); break;
                            default: throw new SimCom_Exception($"Failed to get variable {simVal.VariableRequest.variableName} {hr.ToString()}", hr);
                        }

                        switch (simVal.ValueType)
                        {
                            case WaSim_ValueTypes.INT8:  simVal.SetValue((SByte)varResult); break;
                            case WaSim_ValueTypes.INT16: simVal.SetValue((Int16)varResult); break;
                            case WaSim_ValueTypes.INT32: simVal.SetValue((Int32)varResult); break;
                            case WaSim_ValueTypes.INT64: simVal.SetValue((Int64)varResult); break;
                            case WaSim_ValueTypes.FLOAT: simVal.SetValue((float)varResult); break;
                            case WaSim_ValueTypes.DOUBLE: simVal.SetValue((double)varResult); break;
                        }
                    }
                }
            }
            return true;
        }

        public dynamic Calc(string calcCode, bool isString = false)
        {
            if (calcCode != "")
            {
                HR hr = _client.executeCalculatorCode(calcCode, CalcResultType.Double, out double fResult, out string sResult);
                switch (hr)
                {
                    case HR.OK: break;
                    case HR.NOT_CONNECTED: setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED); break;
                    //default: throw new SimCom_Exception($"Failed to execute calcCode {calcCode} {hr.ToString()}", hr);
                }

                if (isString)
                {
                    return sResult;
                }
                else
                {
                    return fResult;
                }
            }
            return null;
        }

        private bool getCalculatedVariable(SimVal simVal)
        {
            dynamic localValue;
            localValue = Calc($"({simVal.FullName})", (simVal.Units == "STRING"));
            simVal.SetValue(localValue);
            return true;
        }

        private void _client_OnDataReceived(DataRequestRecord dr)
        {
            SimVal simVal;
            dynamic value = 0;
            switch (dr.valueSize)
            {
                case (uint)WaSim_ValueTypes.INT8: if (dr.tryConvert(out SByte int8Val)) value = int8Val; break;
                case (uint)WaSim_ValueTypes.INT16: if (dr.tryConvert(out Int16 int16Val)) value = int16Val; break;
                case (uint)WaSim_ValueTypes.INT32: if (dr.tryConvert(out Int32 int32Val)) value = int32Val; break;
                case (uint)WaSim_ValueTypes.INT64: if (dr.tryConvert(out Int64 int64Val)) value = int64Val; break;
                case (uint)WaSim_ValueTypes.FLOAT: if (dr.tryConvert(out float floatVal)) value = floatVal; break;
                case (uint)WaSim_ValueTypes.DOUBLE: if (dr.tryConvert(out double doubleVal)) value = doubleVal; break;

                default: if (dr.tryConvert(out string stringVal)) value = stringVal; break;
            }
            try
            {
                simVal = simValIDs[dr.requestId];
                if (simVal != null)
                {
                    simVal.SetValue(value);
                }
            }
            catch (KeyNotFoundException)
            {
                // Error you can't receive data that was not registered
            }
        }

        public void DoOnDataReceived(SimVal simVal)
        {
            if (simVal != null)
            {
                if (!simVal.Initialised || simVal.Value != simVal.NewValue)
                {
                    OnDataChanged?.Invoke(this, simVal);
                }
                simVal.SetInitialised();
            }
        }

        public void Log(SimCom_Log_Level logLevel, string LogText, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null, [CallerFilePath] string sourceFile = null)
        {
            OnLogEvent?.DynamicInvoke(this, new LogEventArgs(logLevel, LogText, lineNumber, caller, sourceFile));
        }

        private void _client_OnClientEvent(ClientEvent ev)
        {
            Log(SimCom_Log_Level.Info, $"Client event {ev.eventType} - \"{ev.message}\"; Client status: {ev.status}");
        }

        private void _eventReceiver_OnEvent(SimVal SimVal, EventArgs e)
        {
            DoOnDataReceived(SimVal);
        }
    }
}