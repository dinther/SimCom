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

        private VariableRequest _variableRequest;
        public SimVal(SimCom simCom, string variableName, uint valIndex)
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
                    OldValue = new string("");
                    Value = new string("");
                }
                else
                {
                    double val =0;
                    OldValue = val;
                    Value = val;
                }
                if (_units == "") _units = "number";
                _nameIndex = (_index > 0 && _index < 255) ? _name + ':' + _index.ToString() : _name;
                _variableRequest = new VariableRequest(_type, _nameIndex, _units);
                _fullName = $"{_type}:{_name}";
                if (_index > 0 && _index < 255) _fullName += ":" + _index.ToString();  //  _indexes are never 0 (I think)
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
        public bool Initialised { get { return _initialised; } }

        public void Set()
        {
            _simCom.setVariable(this, Value);
            //_initialised = true;
        }

        public void Set(dynamic Value)
        {
            _simCom.setVariable(this, Value);
            OldValue = Value;
            this.Value = Value;
            //_initialised = true;
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
    }
}

