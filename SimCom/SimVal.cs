using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;

namespace SimComCon
{
    public class SimVal
    {
        private SimCom _simCom = null;
        private uint _valIndex;
        private char _type;
        private string _name;
        private byte _index;
        private string _units;
        private string _nameIndex;
        private string _fullName;
        private bool _isRPN;

        private VariableRequest _variableRequest;
        public SimVal(SimCom simCom, string variableName, uint valIndex)
        {
            _simCom = simCom;
            _valIndex = valIndex;
            _isRPN = isRPN(variableName);
            if (_isRPN)
            {
                _name = variableName;
                _nameIndex = variableName;
                _fullName = variableName;
            }
            else
            {
                splitVariableName(variableName, out _type, out _name, out _index, out _units);
                if (_units == "") _units = "number";
                _nameIndex = _index == 0 ? _name : _name + ':' + _index.ToString();
                _variableRequest = new VariableRequest(_type, _nameIndex, _units);
                _fullName = $"{_type}:{_name}";
                if (_index > 0) _fullName += ":" + _index.ToString();
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
        public bool IsRPN { get { return _isRPN; } }
        public dynamic Value = 0;
        public dynamic OldValue = 0;

        public void Set()
        {
            _simCom.setVariable(this, Value);
        }

        public void Set(dynamic Value)
        {
            _simCom.setVariable(this, Value);
            OldValue = Value;
            this.Value = Value;
        }

        private static bool isRPN(string variableName)
        {
            return variableName.Contains('(') && variableName.Contains(')');
        }

        private void splitVariableName(string variableName, out char Type, out string Name, out byte Index, out string Units)
        {
            Char[] delimiters = { ':', ',' };
            string[] values = variableName.ToUpper().Split(delimiters);
            Type = '\0';
            Name = "";
            Index = 0;
            Units = "";

            if (values.Length == 4)
            {
                Type = values[0][0];
                Name = values[1];
                if (!byte.TryParse(values[2], out Index))
                {
                    throw new SimCom_Exception($"Incorrect value for Index {values[2]}", HR.FAIL);
                }
                Units = values[3];
            }
            else if (values.Length == 3)
            {
                if (values[0].Length == 1) // Type - Name - Index OR Units
                {
                    Type = values[0][0];
                    Name = values[1];
                    if (!byte.TryParse(values[2], out Index))
                    {
                        Units = values[2];
                    }
                }
                else // Name: Index: Units
                {
                    Name = values[0];
                    if (!byte.TryParse(values[1], out Index))
                    {
                        throw new SimCom_Exception($"Incorrect value for Index {values[1]}", HR.FAIL);
                    }
                    Units = values[2];
                }
            }
            else if (values.Length == 2) //  Type OR Name, Name OR Index OR Units
            {
                if (values[0].Length == 1) // Type - Name - Index OR Units
                {
                    Type = values[0][0];
                    Name = values[1];
                }
                else
                {
                    Name = values[0];
                    if (!byte.TryParse(values[1], out Index))
                    {
                        Units = values[1];
                    }
                }
            }
            else if (values.Length == 1)
            {
                Name = values[0];
            }
            else
            {
                throw new SimCom_Exception($"A count of {values.Length} is too many variable parts. Max 3", HR.FAIL);
            }
            if (Type == '\0') // Set the type to default A or K depending on the presence of Underscores or spaces.
            {
                Type = (Name.Contains(' ') || Name == "TITLE") ? 'A' : 'K';
            }
        }
    }
}

