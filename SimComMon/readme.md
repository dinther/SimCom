# SimComMon
SimComMon is a command line demo app to showcase the SimCom library.
Many types of variables can be monitored. such as A: variables, K: variables, L: variables, etc.

The program can set and/or monitor multiple parameters in microsoft flight simulator 2020 and reports to STD OUT.

Pass one or more variable definitions on the command line each definition preceded with a double dash `--`

```SimComMon.exe --A:HEADING INDICATOR,degrees --AUTOPILOT HEADING LOCK DIR,Degrees,INT32,100,1 as APHDG```

This will cause the program to connect to MSFS and return the value of the heading indicator in degrees after which it terminates.

```
HEADING INDICATOR=34.90064381147878
APHDG=126
```

For long names with spaces you might was to pass an alias name like this:

```SimComMon.exe --A:HEADING INDICATOR,degrees as HDG```

This results in

```HDG=35.87491068468636```

after which the program terminates.

## Get Variables

### Variable names
SimCom does quite some magic with variable names and tries to make things as easy as possible.
This is thanks to the way variable names are passed.

The variableName string consist of 7 parts: VarType:Name:Index,Units,ValueType,Interval,deltaEpsilon
For example: `A:NAV OBS:1,degrees,FLOAT,1000,0.1`

#### VarType
VarType is optional if you just use the simconnect variables as published on https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm and https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Event_IDs.htm

SimCom considers variables and events the same thanks to the power of the WASimCommander library. Where possible SimCom will assign the correct VarType if you don't provide one. You can find a description of the various var types here https://docs.flightsimulator.com/html/Additional_Information/Reverse_Polish_Notation.htm

#### Name
This is required. This can be a simconnect variable name or a simconnect event name but also custom variables all thanks the power of the WASimCommander library.

#### Index
Some variables also require an index value. For examle `NAV OBS:1` The index follows after the variable name separated by a colon as shown.

#### Units
Units are there to make life easier. They are an optional in SimCom. Variables can be expressed in a wide range of units as described here https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variable_Units.htm

By default SimCom will use Number as units and thus leaves it up to MSFS what units are returned.

#### ValueType
This is optional and used when dealing with numbers only. By default a 8 byte double floating point variable is assumed unless the Units is "STRING" in which case the default ValueType is assumed to be a string.

Possible value types are "INT8", "INT16", "INT32", "INT64", "FLOAT", "DOUBLE"

#### Interval
This is optional. When no interval is given SimComMon will read the variable once only. Interval duration is given in milliseconds. Therefore a value of 1000 causes the variable to be reported on once every second. However, this will only repeat if the value actually changed. The minimum possible interval is 25 milli seconds.

#### DeltaEpsilon
This is optional. DeltaEpsilon is only relevant when interval is set. DeltaEpsilon defines how much the variable must have changed since the last report before it is reported again. this prevents the output to be swamped with tiny irrelevant changes. for example. `--HEADING INDICATOR,500`
will cause SimComMon to report the tiniest changes of the aircraft heading but only once every 500 milli seconds.
`--HEADING INDICATOR,500,1` will cause SimComMon to report the aircraft heading change only when it changed more than when it reported last and only if at least 500 milliseconds have passed.

## Set Variables
SimComMon can also set variables in MSFS. The syntax for the variable names is identical to reading variables but in addition the variable definition also has an equals sign and a value. For example:
```--AUTOPILOT HEADING LOCK DIR,Degrees=123```
This causes the Autopilot heading bug to be set to 123 degrees after which the program terminates.
Pass an interval if you also wish to monitor the variable.

```--AUTOPILOT HEADING LOCK DIR,Degrees,INT32,100,1 as APHDG=123```

This causes the AUTOPILOT HEADING LOCK DIR variable to be set to 123 degrees once and reported on 'APHDG=123' and after that it will be reported on again under the name `APHDG` if it changes by at least 1 degrees and it has been at least 100 milli seconds since the last report

### Command line examples





![image](https://github.com/dinther/SimCom/assets/1192916/f2e4c98b-3921-48c3-92cd-f31405b60f75)

