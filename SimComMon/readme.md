# SimComMon
SimComMon is a command line demo app to showcase the SimCom library.

The program can monitor and set multiple parameters in microsoft flight simulator 2020
Pass one or more variable definitions on the command line each definition preceded with a double dash `--`

```SimComMon.exe --A:HEADING INDICATOR,degrees```

This will cause the program to connect to MSFS and return the value of the heading indicator in degrees after which it terminates.

```HEADING INDICATOR=35.87491068468636```

For long names with spaces you might was to pass an alias name like this:

```SimComMon.exe --A:HEADING INDICATOR,degrees as HDG```

This results in

```HDG=35.87491068468636```

after which the program terminates.

## Reading variables

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

### Command line examples
Using interval and DeltaEpsilon can reduce the data stream to a minimum but relevant output.
Optional. Char    - Defines the variable VarType.
        //  Name            Required. String  - Name of the Variable or Full Reverse Polish Notation calculations.
        //  Index           Optional. Integer - Some variables use an additional index. For example: A:NAV OBS:1 (Nav radio 1)
        //  Units           Optional. String  - Units of the variable. For example: degrees, feet, knots, (See Simconnect SDK). Default units is "NUMBER"
        //  ValueType       Optional. String  - Type of the value returned. INT8, INT16, INT32, INT64, FLOAT, DOUBLE. Default type is DOUBLE
        //  Interval        Optional. Integer - Interval in milliseconds to monitor the variable. Default is 0 (The variable is read once)
        //  deltaEpsilon    Optional. Float   - The minimum change in value to trigger a notification. Default is 0 (Any change in value triggers a notification)


All types of variables can be monitored. such as A: variables, K: variables, L: variables, etc.

![image](https://github.com/dinther/SimCom/assets/1192916/f2e4c98b-3921-48c3-92cd-f31405b60f75)

