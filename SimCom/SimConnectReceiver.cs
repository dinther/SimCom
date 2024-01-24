using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using System.Diagnostics;
using WASimCommander.CLI.Enums;

namespace SimComLib
{
    public delegate void SimEventDataHandler(SimVal SimVal, EventArgs e);
    public delegate void SimConnectHandler(SimConnectEventReceiver EventReceiver, bool Connected, EventArgs e);
    public delegate void SimConnectLogEventHandler(SimConnectEventReceiver simConnecteventReceiver, LogEventArgs LogData);


    public enum NOTIFICATION_GROUPS
    {
        GROUP0,
    }

    public enum DEFINITION
    {
        Dummy = 0
    }

    public class SimConnectEventReceiver // : IDisposable
    {
        private int _configIndex;
        private uint _clientID;

        private Task? _messageWaitTask;
        private bool _connected = false;
        private bool _connecting = false;

        private SimConnect? _simConnect;

        private readonly EventWaitHandle _scReady = new EventWaitHandle(false, EventResetMode.AutoReset);

        readonly AutoResetEvent _scQuit = new AutoResetEvent(false);
        private List<SimVal> _simEventsRegistered = new List<SimVal>();
        private Dictionary<uint, SimVal> simEventVals = new Dictionary<uint, SimVal>();
        private const int WM_USER_SIMCONNECT = 0x0406; //Any value in the WM_USER range can be used (0x0400 - 0x7FFF. Recommend: 0x0406)
        private const int MSG_RCV_WAIT_TIME_MS = 5000;   // SimConnect.ReceiveMessage() wait time

        public SimConnectEventReceiver(uint clientID, int configIndex = 0)
        {
            _clientID = clientID;
            _configIndex = configIndex;
        }

        public void RegisterSimEvent(SimVal simVal)
        {
            if (_simConnect == null) { return; }
            if (_simEventsRegistered.Contains(simVal)) { return; }

            _simConnect.MapClientEventToSimEvent((DEFINITION)simVal.ValIndex, simVal.NameIndex);
            _simConnect.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, (DEFINITION)simVal.ValIndex, false);
            _simEventsRegistered.Add(simVal);
            if (!simEventVals.ContainsKey(simVal.ValIndex))
            {
                simEventVals[simVal.ValIndex] = simVal;
            }
        }

        public bool Connect()
        {
            Debug.WriteLine(_simConnect);
            try
            {
                if (_simConnect == null)
                {
                    Log(SimCom_Log_Level.Info ,$"Connecting SimConnectEventReceiver to configIndex {_configIndex} ...");
                    _simConnect = new SimConnect("SimComEventReceiver_" + _clientID.ToString(), IntPtr.Zero, WM_USER_SIMCONNECT, _scReady, (uint)Math.Max(0,_configIndex));
                    _simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                    _simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simConnect_OnRecvQuit);
                    _simConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(simConnect_OnRecvEvent);
                    _connecting = true;
                    this._messageWaitTask = Task.Run(this.ReceiveMessages);
                    Log(SimCom_Log_Level.Error, "messageWaitTask Timeout.");
                    return true;
                }
            }
            catch (Exception e)
            {
                _connecting = false;
                if (e is System.Runtime.InteropServices.COMException)
                {
                    //  Ignore. Assume this to be because MSFS wasn't running.
                    _simConnect = null;
                }
                else
                {
                    throw;
                }
            }
            return false;
        }

        public void Disconnect()
        {
            if (_messageWaitTask != null)
            {
                _scQuit.Set();  // trigger message wait task to exit
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (_messageWaitTask.Status == TaskStatus.Running && sw.ElapsedMilliseconds <= MSG_RCV_WAIT_TIME_MS)
                    Thread.Sleep(2);
                if (_messageWaitTask.Status == TaskStatus.Running)
                    Log(SimCom_Log_Level.Error, "Message wait task timed out while stopping.");
                try { _messageWaitTask.Dispose(); }
                catch { }// ignore in case it hung
            }
            if (_simConnect != null)
            {
                try
                {
                    _simConnect.Dispose();
                    Debug.WriteLine("SimConnect disposed");
                }
                catch (Exception e)
                {
                    Log(SimCom_Log_Level.Error, "Exception while trying to dispose SimConnect client.");
                }
            }
            _simConnect = null;
            _messageWaitTask = null;
            _connected = false;
            _connecting = false;
        }

        public bool Connected { get { return _connected; } }
        public int ConfigIndex { get { return _configIndex; } }
        public event SimConnectHandler OnConnection;
        public event SimEventDataHandler OnEvent;
        public event SimConnectLogEventHandler OnLogEvent;

        private void ReceiveMessages()
        {
            Log(SimCom_Log_Level.Info, "ReceiveMessages task started.");
            int sig;
            var waitHandles = new WaitHandle[] { _scReady, _scQuit };
            try
            {
                while (_connected || _connecting)
                {
                    sig = WaitHandle.WaitAny(waitHandles, MSG_RCV_WAIT_TIME_MS);
                    if (sig == 0 && _simConnect != null)
                        _simConnect.ReceiveMessage();    // note that this calls our event handlers synchronously on this same thread.
                    else if (sig != WaitHandle.WaitTimeout) break;
                }
            }
            catch (ObjectDisposedException) { 
            }  // ignore but exit
            catch (Exception e)
            {
                Log(SimCom_Log_Level.Error, $"ReceiveMessages task exception {e.HResult}, disconnecting.");
                Task.Run(Disconnect);  // async to avoid deadlock
                                       // COMException (0xC000014B) = broken pipe (sim crashed/network loss on a Pipe type connection)
            }
            Log(SimCom_Log_Level.Info, "ReceiveMessages task stopped.");
        }

        private void Log(SimCom_Log_Level logLevel ,string LogText)
        {
            OnLogEvent?.DynamicInvoke(this, new LogEventArgs(logLevel, LogText));
        }

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Log(SimCom_Log_Level.Info, "OnRecvOpen");
            _connected = true;
            OnConnection?.Invoke(this, _connected, new EventArgs());
            registerSimEvents();
        }

        private void simConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Log(SimCom_Log_Level.Info, "OnRecvquit");
            _connected = false;
            OnConnection?.Invoke(this, _connected, new EventArgs());
        }

        private void simConnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            SimVal simVal = simEventVals[(uint)recEvent.uEventID];
            Log(SimCom_Log_Level.Info, $"OnRecvEvent: {simVal.FullName} ( {simVal.Value} )");
            simVal.SetValue(recEvent.dwData);
            OnEvent?.Invoke(simVal, new EventArgs());
        }

        private void registerSimEvents()
        {
            foreach (KeyValuePair<uint, SimVal> entry in simEventVals)
            {
                RegisterSimEvent(entry.Value);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

