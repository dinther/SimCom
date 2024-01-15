using System.Diagnostics;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;
using WASimCommander.CLI.Client;

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

    public struct ValueTypes1
    {
        public const uint DATA_TYPE_INT8 = uint.MaxValue;
        public const uint DATA_TYPE_INT16 = 4294967294;
        public const uint DATA_TYPE_INT32 = 4294967293;
        public const uint DATA_TYPE_INT64 = 4294967292;
        public const uint DATA_TYPE_FLOAT = 4294967291;
        public const uint DATA_TYPE_DOUBLE = 4294967290;
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
        private uint _configIndex;
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
        public event SimCoMConnectHandler? OnConnection;
        private WaSim_Version? _version;
        public WaSim_Version Version { get { return _version;} }
        public WASimClient WASimclient { get { return _client; } }
        public SimCom(uint clientID, uint configIndex = 0)
        {
            _clientID = clientID;
            _configIndex = configIndex;
            _client = new WASimClient(_clientID);
            _client.OnClientEvent += _client_OnClientEvent;
            _client.OnDataReceived += _client_OnDataReceived;

            _simConnectEventReceiver = new SimConnectEventReceiver(_clientID, _configIndex);
            _simConnectEventReceiver.OnEvent += _eventReceiver_OnEvent;
            _simConnectEventReceiver.OnConnection += _eventReceiver_OnConnection;
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
        //  deltaEpsilon    Optional. Float   - The minimum change in value to trigger a notification. Default is 0 (Any change in value triggers a notification)
        //
        //  Alias           Optional. String - Alias for the variable. Default is the variableName.Name
        //  Default         Optional. Dynamic - Default value for the variable. Default is null (0 for numbers, "" for strings)
        public SimVal GetVariable(string variableName, string Alias="", dynamic Default=null)
        {
            SimVal simVal;
            bool init = false;
            SimVal lookupSimVal = new SimVal(this, variableName, definitionIndex, Alias, Default);
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
                else if (VarType == '\0')
                {
                    dataRequest = new DataRequest(
                        requestId: definitionIndex,
                        simVarName: simVal.Name,
                        unitName: simVal.Units,
                        simVarIndex: simVal.Index == 255 ? (Byte)0 : simVal.Index,
                        valueSize: (uint)simVal.ValueType, //WaSim_ValueTypes.FLOAT,// (uint)4294967291,
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
                        valueSize: (uint)4294967291,
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
            if (init)
            {
                simVal.OldValue = simVal.Value;
                DoOnDataReceived(simVal);
            }
            return simVal;
        }

        public SimVal SetVariable(SimVal simVal, dynamic value)
        {
            HR hr;
            if (simVal.Units == "STRING")
            {
                // WASimCommander can't set strings yet.
            }
            else
            {
                hr = _client.setVariable(simVal.VariableRequest, value);
                switch (hr)
                {
                    case HR.OK: break;
                    case HR.NOT_CONNECTED: setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED); break;
                    default: throw new SimCom_Exception($"Failed to set variable {simVal.FullName} {hr.ToString()}", hr);
                }
            }
            return simVal;
        }

        public event SimComDataEventHandler OnDataChanged;

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

        private bool getVariableValue(SimVal simVal)
        {
            HR hr;
            if (simVal.IsRPN)
            {
                var answer = Calc(simVal.Name);
                simVal.setValue(answer);
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
                        simVal.setValue(varResult);
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
                            case WaSim_ValueTypes.INT8:  simVal.setValue((SByte)varResult); break;
                            case WaSim_ValueTypes.INT16: simVal.setValue((Int16)varResult); break;
                            case WaSim_ValueTypes.INT32: simVal.setValue((Int32)varResult); break;
                            case WaSim_ValueTypes.INT64: simVal.setValue((Int64)varResult); break;
                            case WaSim_ValueTypes.FLOAT: simVal.setValue((float)varResult); break;
                            case WaSim_ValueTypes.DOUBLE: simVal.setValue((double)varResult); break;
                            default: simVal.setValue(varResult); break;
                        }
                    }
                }
            }
            return true;
        }

        public dynamic Calc(string calcCode, bool isString = false)
        {
            HR hr = _client.executeCalculatorCode(calcCode, CalcResultType.Double, out double fResult, out string sResult);
            switch (hr)
            {
                case HR.OK: break;
                case HR.NOT_CONNECTED: setConnectionStatus(SimCom_Connection_Status.CONNECTION_FAILED); break;
                default: throw new SimCom_Exception($"Failed to execute calcCode {calcCode} {hr.ToString()}", hr);
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

        private bool getCalculatedVariable(SimVal simVal)
        {
            dynamic localValue;
            localValue = Calc($"({simVal.FullName})", (simVal.Units == "STRING"));
            simVal.OldValue = simVal.Value;
            simVal.setValue(localValue);
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
                    simVal.OldValue = simVal.Value;
                    simVal.setValue(value);
                    simVal.DoOnChanged();
                    DoOnDataReceived(simVal);
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
                //Console.WriteLine(simVal.FullName);
                if (!simVal.Initialised || simVal.OldValue != simVal.Value)
                {
                    OnDataChanged?.Invoke(this, simVal);
                }
                simVal.SetInitialised();
            }
        }

        private void _client_OnClientEvent(ClientEvent ev)
        {
            Debug.WriteLine($"Client event {ev.eventType} - \"{ev.message}\"; Client status: {ev.status}");
        }

        private void _eventReceiver_OnEvent(SimVal SimVal, EventArgs e)
        {
            DoOnDataReceived(SimVal);
        }
    }
}