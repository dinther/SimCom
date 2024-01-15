using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;
using System.Text.RegularExpressions;

namespace SimComLib
{
    public class SimVal
    {
        private SimCom _simCom = null;
        private uint _valIndex;
        private char _varType;
        private string _name;
        private string _alias;
        private byte _index;
        private string _units;
        private WaSim_ValueTypes _valueType;
        private uint _interval;
        private double _deltaEpsilon;
        private string _nameIndex;
        private string _fullName;
        private bool _isRPN;
        private dynamic _value;
        private bool _initialised = false;
        private double _lastHighSpeedAdjustValue = 0;
        private double _lastHighSpeedTime;
        private string[] _valueTypeStrings = { "INT8", "INT16", "INT32", "INT64", "FLOAT", "DOUBLE" };
        private DateTime dStartTime = DateTime.Now;
        private VariableRequest _variableRequest;
        public double GetTimestampInSeconds()
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSpan = now - dStartTime;
            return (double)timeSpan.TotalSeconds;
        }
        public SimVal(SimCom simCom, string variableName, uint valIndex, string alias="", dynamic Default=null)
        {
            _simCom = simCom;
            _valIndex = valIndex;
            _isRPN = isRPN(variableName);
            if (_isRPN)
            {
                string pattern = "(?<=[,])";
                char[] charsToTrim = { ',' };
                string[] values = Regex.Split(variableName.ToUpper(), pattern);
                uint i = 0;
                _name = "";
                while (i < values.Length && !double.TryParse(values[i].TrimEnd(charsToTrim), out double testNumber))
                {
                    _name += values[i];
                    i++;
                }
                _name = _name.TrimEnd(charsToTrim);
                _nameIndex = _name;
                _fullName = _name;
                if (values.Length > i && uint.TryParse(values[i].TrimEnd(charsToTrim), out _interval)) { i++; }
                if (values.Length > i && double.TryParse(values[i].TrimEnd(charsToTrim), out _deltaEpsilon)) { i++; }
            }
            else
            {
                splitVariableName(variableName, out _varType, out _name, out _index, out _units, out _valueType, out _interval, out _deltaEpsilon);
                if (_units == "STRING")
                {
                    OldValue = new string(Default != null? Default : "");
                    _value = new string(Default != null ? Default : "");
                }
                else
                {
                    double val = Default != null ? Default : 0;
                    OldValue = val;
                    _value = val;
                }
                if (_units == "") _units = "number";
                _nameIndex = (_index > 0 && _index < 255) ? _name + ':' + _index.ToString() : _name;
                _variableRequest = new VariableRequest(_varType, _nameIndex, _units);
                _fullName = $"{_varType}:{_name}";
                if (_index > 0 && _index < 255) _fullName += ":" + _index.ToString();  //  _indexes are never 255 (I think)
                if (_units.Length > 0) _fullName += "," + _units;
            }
            _alias = alias;
            //_alias = alias=="" ? _name : alias;
        }
        public string FullName { get { return _fullName; } }
        public VariableRequest VariableRequest { get { return _variableRequest; } }
        public uint ValIndex { get { return _valIndex; } }
        public char VarType { get { return _varType; } }
        public string Name { get { return _name; } }
        public string Alias { get { return _alias; } }
        public string NameIndex { get { return _nameIndex; } }
        public byte Index { get { return _index; } }
        public string Units { get { return _units; } }
        public WaSim_ValueTypes ValueType { get { return _valueType; } }
        public uint Interval { get { return _interval; } }
        public double DeltaEpsilon { get { return _deltaEpsilon; } }
        public bool IsRPN { get { return _isRPN; } }
        public void setValue(dynamic value) //temp test
        {
            _value = value;
        }
        public dynamic Value { get { return _value; } }
        public dynamic OldValue;
        public string Text {  get { return Value.ToString(); } }
        public string Format(string format = "", double displayScaler = 1)
        {
            return String.Format(format, Value * displayScaler);
        }
        public bool Initialised { get { return _initialised; } }

        public dynamic Set()
        {
            return Set(Value);
        }

        public dynamic Set(dynamic Value)
        {
            _simCom.SetVariable(this, Value);
            OldValue = _value;
            _value = Value;
            return Value;
        }

        public dynamic Adj(dynamic Value)
        {
            return Set(_value += Value);
        }

        //  This is a rather powerful value adjust function designed to be used with rotary encoders that return relative change.
        //  The Rotary encoder is expected to send an integer representing the number of steps it turned since the last report.
        //  this value is either negative (CCW) or positive (CW). The magnitude of the value represents how fast the knob was turned.
        //  
        //  This function makes use of this speed indication and allows for a slow and fast mode.
        //  Depending on the mode slow or fast multipliers and rounding targets are used.
        //  You can also define the treshold between slow and fast.
        //
        //  Dropping back from fast to slow mode happens when no input has been received fopr a set amount of time. This is 0.6 seconds by default.
        //  It is also possible to use this function in absolute mode. In this mode the adjustvalue is either -1, 0 or 1.
        //  this means that the Value will be adjusted by the appropriate multiplier.
        //
        //  The slowNearest and fastNearest parameters allow you to define the rounding target for slow and fast mode.
        //  Typically you want to round to the the same values as the multiplier.

        public double Adj(double adjustValue, double slowFastTreshold, double slowMultiplier, double fastMultiplier, double slowNearest, double fastNearest, double min, double max, bool loopRange = false, bool absoluteMode=false, double fastFallBackTime = 0.6f)
        {
            double multiplier;
            double nearest;
            double newValue = _value;

            if (Math.Abs(adjustValue) > slowFastTreshold)
            {
                _lastHighSpeedAdjustValue = Math.Abs(adjustValue);
                _lastHighSpeedTime = GetTimestampInSeconds();
                multiplier = fastMultiplier;
                nearest = fastNearest;
            }
            else
            {
                double nowTime = GetTimestampInSeconds();
                double timeSinceLastHighSpeedMode = nowTime - _lastHighSpeedTime;
                if (timeSinceLastHighSpeedMode < fastFallBackTime)
                {
                    adjustValue = (adjustValue < 0 ? -1 : adjustValue > 0 ? 1 : 0) * Math.Abs(_lastHighSpeedAdjustValue);
                    multiplier = fastMultiplier;
                    nearest = fastNearest;
                    _lastHighSpeedTime = nowTime;
                }
                else
                {
                    multiplier = slowMultiplier;
                    nearest = slowNearest;
                }
            }
            if (absoluteMode && adjustValue != 0) adjustValue = adjustValue < 0 ? -1 : 1;
            newValue += adjustValue * multiplier;
            if (min != 0 || max != 0)
            {
                {
                    if (newValue < min)
                    {
                        if (loopRange) newValue = max + (newValue - min);
                        else newValue = min;
                    }
                    else if (newValue > max)
                    {
                        if (loopRange) newValue = min + (newValue - max);
                        else newValue = max;
                    }
                }
            }
            if (nearest != 0)
            {
                newValue = Math.Round(newValue / nearest, MidpointRounding.AwayFromZero) * nearest;
            }
            return Set(newValue);
        }


        private static bool isRPN(string variableName)
        {
            return variableName.Contains('(') && variableName.Contains(')');
        }

        private WaSim_ValueTypes stringToValueType(string ValueType)
        {
            switch (ValueType.ToUpper())
            {
                case "INT8": return WaSim_ValueTypes.INT8;
                case "INT16": return WaSim_ValueTypes.INT16;
                case "INT32": return WaSim_ValueTypes.INT32;
                case "INT64": return WaSim_ValueTypes.INT64;
                case "FLOAT": return WaSim_ValueTypes.FLOAT;
                case "DOUBLE": return WaSim_ValueTypes.DOUBLE;
                default: throw new SimCom_Exception($"Unknown ValueType {ValueType}", HR.FAIL);
            }
        }

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

        private void splitVariableName(string variableName, out char VarType, out string Name, out byte Index, out string Units, out WaSim_ValueTypes ValueType, out uint Interval, out double DeltaEpsilon)
        {

            //string userString = "A:NAV OBS:1,degrees,25,0.01";
            string pattern = "(?<=[:,])";
            //string[] sentencesList = Regex.Split(userString, pattern);
            char[] charsToTrim = { ':', ','};

            //Char[] delimiters = { ':', ',' };
            string[] values = Regex.Split(variableName.ToUpper() , pattern);
            VarType = '\0';
            Name = "";
            Index = 255;
            Units = "";
            ValueType = WaSim_ValueTypes.DOUBLE;
            Interval = 0;
            DeltaEpsilon = 0;

            int i = 0;

            //  Try to find the type
            //  Type is always the first parameter and is a single character and not a byte. This is optional.
            if (values.Length > i && values[i].Length == 2 && values[i].EndsWith(':') && !byte.TryParse(values[i][0].ToString(), out byte TestInt))
            {
                VarType = values[i][0];
                i++;
            }   else
            {
                VarType = (values.Length > i && (values[i].Contains(' ') || values[i].TrimEnd(charsToTrim) == "TITLE")) ? 'A' : 'K';
            }

            //  Try to find the name
            //  Name is always the first or second parameter and must be present
            if (values.Length > i)
            {
                Name = values[i].TrimEnd(charsToTrim);
                i++;
            }
            else throw new SimCom_Exception($"No variable name found in {variableName}", HR.FAIL);

            //  Try to find the index
            //  Index is always the second or third parameter and is a int and optional. The preceeding value must end with ':'
            if (values.Length > i && values[i-1].EndsWith(':') && byte.TryParse(values[i].TrimEnd(charsToTrim), out Index))
            {
                i++;
            }
            
            //  Try to find the units
            //  Units is either the second, third or fourth parameter and is not a number and is not a Type and is optional.
            decimal numberTest = 0;
            if (values.Length > i && !decimal.TryParse(values[i].TrimEnd(charsToTrim), out numberTest) && !_valueTypeStrings.Contains(values[i].ToUpper()))
            {
                Units = values[i].TrimEnd(charsToTrim);
                i++;
            }

            //  Try to find the type
            //  Type is either the second, third, fourth or fifth parameter and is not a number and is optional.
            //decimal numberTest = 0;

            if (values.Length > i && _valueTypeStrings.Contains(values[i].TrimEnd(charsToTrim).ToUpper()))
            {
                ValueType = stringToValueType(values[i].TrimEnd(charsToTrim));
                i++;
            }

            //  Try to find the interval
            //  Interval is either the third, fourth or fifth parameter and is a uint and is optional.
            if (values.Length > i && uint.TryParse(values[i].TrimEnd(charsToTrim), out Interval)) { i++; }

            //  Try to find the deltaEpsilon
            //  DeltaEpsilon is always the fourth, fifth or sixth parameter and is a double and is optional.
            if (values.Length > i && double.TryParse(values[i].TrimEnd(charsToTrim), out DeltaEpsilon)) { i++; }

            if (values.Length > i) throw new SimCom_Exception($"Too many variable parts. Max 7", HR.FAIL);
        }
        public void SetInitialised()
        {
            _initialised = true;
        }

        public void DoOnChanged()
        {
            OnChanged?.Invoke(this._simCom, this);
        }

        public event SimComDataEventHandler OnChanged;
    }
}

