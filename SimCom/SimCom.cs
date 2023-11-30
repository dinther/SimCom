using System;
using System.Collections.Generic;
using System.Diagnostics;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;
using WASimCommander.CLI.Client;

namespace SimComLib
{
    public delegate void SimComDataEventHandler(SimCom SimCom, SimVal SimVal);

    public struct ValueTypes
    {
        public const uint DATA_TYPE_INT8 = uint.MaxValue;
        public const uint DATA_TYPE_INT16 = 4294967294;
        public const uint DATA_TYPE_INT32 = 4294967293;
        public const uint DATA_TYPE_INT64 = 4294967292;
        public const uint DATA_TYPE_FLOAT = 4294967291;
        public const uint DATA_TYPE_DOUBLE = 4294967290;
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
        private byte _connected = 0;
        public bool Connected
        {
            get
            {
                return (_connected > 0);
            }
        }
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

        private void _eventReceiver_OnConnection(SimConnectEventReceiver EventReceiver, bool connected, EventArgs e)
        {
            if (connected)
            {
                _connected++;
            }
            else
            {
                _connected--;
            }
        }

        public WaSim_Version Connect()
        {
            HR hr;
            UInt32 version;
            if ((hr = _client.connectSimulator()) != HR.OK)
                throw new SimCom_Exception($"Failed to connect to simulator {hr.ToString()}", hr);
            _connected++;
            if ((version = _client.pingServer()) == 0)
                throw new Exception($"Failed to communicate with server {hr.ToString()}");
            _connected++;
            if ((hr = _client.connectServer()) != HR.OK)
                throw new SimCom_Exception($"Failed to connect to server {hr.ToString()}", hr);
            _connected++;
            _simConnectEventReceiver.Connect();

            return new WaSim_Version(version);
        }

        public void disconnect()
        {
            _simConnectEventReceiver.Disconnect();
            _client.disconnectServer();
            _client.disconnectSimulator();
            _client.Dispose();
            _connected = 0;
        }

        public SimVal GetVariable(string variableName, uint interval = 0, double deltaEpsilon = 0.0)
        {
            SimVal simVal;
            bool init = false;
            SimVal lookupSimVal = new SimVal(this, variableName, definitionIndex);
            try
            {
                //simVal = simValFullNames[lookupSimVal.FullName];
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

            if (simVal.Type != 'K' && interval != 0)
            {
                UpdatePeriod updatePeriod = UpdatePeriod.Millisecond;
                char type = (simVal.Type == 'A' && simVal.Units != "") ? '\0' : simVal.Type;

                DataRequest dataRequest = null;
                HR hr;
                if (simVal.IsRPN)
                {
                    dataRequest = new DataRequest(
                        requestId: definitionIndex,
                        resultType: CalcResultType.Double,
                        calculatorCode: simVal.FullName,
                        valueSize: (uint)WaSim_ValueTypes.FLOAT,
                        period: updatePeriod,
                        interval: Math.Max(interval, 25),
                        deltaEpsilon: (float)Math.Max(0, deltaEpsilon)
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
                        interval: Math.Max(interval, 25),
                        deltaEpsilon: 0.0f
                    );
                }
                else if (type == '\0')
                {
                    dataRequest = new DataRequest(
                        requestId: definitionIndex,
                        simVarName: simVal.Name,
                        unitName: simVal.Units,
                        simVarIndex: simVal.Index,
                        valueSize: (uint)WaSim_ValueTypes.FLOAT,// (uint)4294967291,
                        period: updatePeriod,
                        interval: Math.Max(interval, 25),
                        deltaEpsilon: (float)Math.Max(0, deltaEpsilon)
                    );
                }
                else
                {
                    dataRequest = new DataRequest(
                        requestId: definitionIndex,
                        variableType: type,
                        variableName: simVal.Name,
                        valueSize: (uint)4294967291,
                        period: updatePeriod,
                        interval: Math.Max(interval, 25),
                        deltaEpsilon: (float)Math.Max(0, deltaEpsilon)
                    );
                }

                if ((hr = _client.saveDataRequest(dataRequest)) != HR.OK)
                    throw new SimCom_Exception($"Failed to subscribe to {type}:{simVal.Name} {hr.ToString()}", hr);
            }
            definitionIndex++;

            if (simVal.Type == 'K')
            {
                _simConnectEventReceiver.RegisterSimEvent(simVal);
            }

            getVariableValue(simVal);
            if (init)
            {
                simVal.OldValue = simVal.Value;
            }
            return simVal;
        }

        public void setVariable(SimVal simVal, dynamic value)
        {
            HR hr;
            if (simVal.Units == "STRING")
            {
                // WASimCommander can't set strings yet.
            }
            else
            {
                if ((hr = _client.setVariable(simVal.VariableRequest, value)) != HR.OK)
                    throw new SimCom_Exception($"Failed to set variable {simVal.FullName} {hr.ToString()}", hr);
            }
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
            HR hr;
            if ((hr = _client.lookup(variableTypeToLookupItemType(variableType), name, out var localVarId)) != HR.OK)
                throw new SimCom_Exception($"Failed to lookup Simulator variable {name}. {hr.ToString()}", hr);
            return new VariableRequest(variableType, name, units);
        }

        private bool getVariableValue(SimVal simVal)
        {
            HR hr;
            if (simVal.IsRPN)
            {
                var answer = Calc(simVal.Name);
                simVal.Value = answer;
            } /*else if (simVal.Units == "STRING")
            {
                getCalculatedVariable(simVal);
            }*/
            else
            {
                if (simVal.Type != 'K')
                {
                    if (simVal.Units == "STRING")
                    {
                        if ((hr = _client.getVariable(simVal.VariableRequest, out string varResult)) != HR.OK)
                            throw new SimCom_Exception($"Failed to get variable {simVal.VariableRequest.variableName} {hr.ToString()}", hr);
                        simVal.Value = varResult;
                    }
                    else
                    {
                        if ((hr = _client.getVariable(simVal.VariableRequest, out double varResult)) != HR.OK)
                            throw new SimCom_Exception($"Failed to get variable {simVal.VariableRequest.variableName} {hr.ToString()}", hr);
                        simVal.Value = varResult;
                    }
                }
            }
            return true;
        }

        public dynamic Calc(string calcCode, bool isString = false)
        {
            HR hr;
            if ((hr = _client.executeCalculatorCode(calcCode, CalcResultType.Double, out double fResult, out string sResult)) != HR.OK)
            {
                throw new SimCom_Exception($"Failed to execute calcCode {calcCode} {hr.ToString()}", hr);
            }
            Debug.WriteLine($"Calculator code '{calcCode}' returned: {fResult} and '{sResult}'");
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
            simVal.Value = localValue;
            return true;
        }

        private void _client_OnDataReceived(DataRequestRecord dr)
        {
            SimVal simVal;
            dynamic value = 0;
            if (dr.tryConvert(out float fVal))
            {
                value = fVal;
            }
            else if (dr.tryConvert(out string sVal))
            {
                value = sVal;
            }

            try
            {
                simVal = simValIDs[dr.requestId];
                if (simVal != null)
                {
                    simVal.OldValue = simVal.Value;
                    simVal.Value = value;
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
                Console.WriteLine(simVal.FullName);
                OnDataChanged?.Invoke(this, simVal);
            }
        }

        private void _client_OnLogRecordReceived(LogRecord A_0, LogSource A_1)
        {
            throw new NotImplementedException();
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