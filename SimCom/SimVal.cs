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
        private SimCom? _simCom = null;
        private uint _valIndex;
        private char _type;
        private string _name;
        private byte _index;
        private string _units;
        private uint _interval;
        private double _deltaEpsilon;
        private string _nameIndex;
        private string _fullName;
        private bool _isRPN;
        private bool _initialised = false;
        private float _lastHighSpeedAdjustValue = 0;
        private float _lastHighSpeedTime;
        private DateTime dStartTime = DateTime.Now;
        private VariableRequest _variableRequest;
        public float GetTimestampInSeconds()
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSpan = now - dStartTime;
            return (float)timeSpan.TotalSeconds;
        }
        public SimVal(SimCom simCom, string variableName, uint valIndex, dynamic Default)
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
                splitVariableName(variableName, out _type, out _name, out _index, out _units, out _interval, out _deltaEpsilon);
                if (_units == "STRING")
                {
                    OldValue = new string(Default != null? Default : "");
                    Value = new string(Default != null ? Default : "");
                }
                else
                {
                    double val = Default != null ? Default : 0;
                    OldValue = val;
                    Value = val;
                }
                if (_units == "") _units = "number";
                _nameIndex = (_index > 0 && _index < 255) ? _name + ':' + _index.ToString() : _name;
                _variableRequest = new VariableRequest(_type, _nameIndex, _units);
                _fullName = $"{_type}:{_name}";
                if (_index > 0 && _index < 255) _fullName += ":" + _index.ToString();  //  _indexes are never 255 (I think)
                if (_units.Length > 0) _fullName += "," + _units;
            }
        }
        public string FullName { get { return _fullName; } }
        public VariableRequest VariableRequest { get { return _variableRequest; } }
        public uint ValIndex { get { return _valIndex; } }
        public char Type { get { return _type; } }
        public string Name { get { return _name; } }
        public string NameIndex { get { return _nameIndex; } }
        public byte Index { get { return _index; } }
        public string Units { get { return _units; } }
        public uint Interval { get { return _interval; } }
        public double DeltaEpsilon { get { return _deltaEpsilon; } }
        public bool IsRPN { get { return _isRPN; } }
        public dynamic Value;
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
            _simCom.setVariable(this, Value);
            this.Value = Value;
            OldValue = Value;
            return Value;
        }

        public dynamic Adj(dynamic Value)
        {
            return Set(this.Value +=Value);
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

        public dynamic Adj(float adjustValue, float slowFastTreshold, float slowMultiplier, float fastMultiplier, float slowNearest, float fastNearest, float min, float max, bool loopRange = false, bool absoluteMode=false, float fastFallBackTime = 0.6f)
        {
            float step;
            float multiplier;
            float nearest;
            if (Math.Abs(adjustValue) > slowFastTreshold)
            {
                _lastHighSpeedAdjustValue = Math.Abs(adjustValue);
                _lastHighSpeedTime = GetTimestampInSeconds();
                multiplier = fastMultiplier;
                nearest = fastNearest;
            }
            else
            {
                float nowTime = GetTimestampInSeconds();
                float timeSinceLastHighSpeedMode = nowTime - _lastHighSpeedTime;
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
            Value += adjustValue * multiplier;
            if (min != 0 || max != 0)
            {
                {
                    if (Value < min)
                    {
                        if (loopRange) Value = max + (Value - min);
                        else Value = min;
                    }
                    else if (Value > max)
                    {
                        if (loopRange) Value = min + (Value - max);
                        else Value = max;
                    }
                }
            }
            if (nearest != 0)
            {
                Value = (float)Math.Round(Value / nearest, MidpointRounding.AwayFromZero) * nearest;
            }
            return Set(Value);
        }


        private static bool isRPN(string variableName)
        {
            return variableName.Contains('(') && variableName.Contains(')');
        }

        private void splitVariableName(string variableName, out char Type, out string Name, out byte Index, out string Units, out uint Interval, out double DeltaEpsilon)
        {

            //string userString = "A:NAV OBS:1,degrees,25,0.01";
            string pattern = "(?<=[:,])";
            //string[] sentencesList = Regex.Split(userString, pattern);
            char[] charsToTrim = { ':', ','};

            //Char[] delimiters = { ':', ',' };
            string[] values = Regex.Split(variableName.ToUpper() , pattern);
            Type = '\0';
            Name = "";
            Index = 255;
            Units = "";
            Interval = 0;
            DeltaEpsilon = 0;

            int i = 0;

            //  Try to find the type
            //  Type is always the first parameter and is a single character and not a byte. This is optional.
            if (values.Length > i && values[i].Length == 2 && values[i].EndsWith(':') && !byte.TryParse(values[i][0].ToString(), out byte TestInt))
            {
                Type = values[i][0];
                i++;
            }   else
            {
                Type = (values.Length > i && (values[i].Contains(' ') || values[i].TrimEnd(charsToTrim) == "TITLE")) ? 'A' : 'K';
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
            //  Units is always the second, third or fourth parameter and is not a number and is optional.
            decimal numberTest = 0;
            if (values.Length > i && !decimal.TryParse(values[i].TrimEnd(charsToTrim), out numberTest))
            {
                Units = values[i].TrimEnd(charsToTrim);
                i++;
            }

            //  Try to find the interval
            //  Interval is always the third, fourth or fifth parameter and is a uint and is optional.
            if (values.Length > i && uint.TryParse(values[i].TrimEnd(charsToTrim), out Interval)) { i++; }

            //  Try to find the deltaEpsilon
            //  DeltaEpsilon is always the fourth, fifth or sixth parameter and is a double and is optional.
            if (values.Length > i && double.TryParse(values[i].TrimEnd(charsToTrim), out DeltaEpsilon)) { i++; }

            if (values.Length > i) throw new SimCom_Exception($"Too many variable parts. Max 6", HR.FAIL);
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

