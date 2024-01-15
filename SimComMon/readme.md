# SimComMon
SimComMon is a command line demo app to showcase the SimCom library. Undfer the hood SimCom uses both SimConnect for the events and WASimCommander for the rest. Thanks to [WASimCommander](https://github.com/mpaperno/WASimCommander) many types of variables can be monitored. such as A: variables, K: variables, L: variables, etc.

The program can get, set and/or monitor most variables in microsoft flight simulator 2020 via the command line. It reports to STD OUT.

SimCom can handle
- [SimConnect variables](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm)
- [SimConnect events](https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Event_IDs.htm)
- [Calculated variables](https://docs.flightsimulator.com/html/Additional_Information/Reverse_Polish_Notation.htm)
- Custom variables and whatever else WASimcommander can do.

Pass one or more variable definitions on the command line each definition preceded with a double dash `--`

```SimComMon.exe --A:HEADING INDICATOR,degrees --AUTOPILOT HEADING LOCK DIR,Degrees,INT32,100,1 as APHDG```

This will cause the program to connect to MSFS and return the value of the heading indicator in degrees after which it terminates.

```
HEADING INDICATOR=34.90064381147878
AUTOPILOT HEADING LOCK DIR=126
```

For long names with spaces you might was to pass an alias name like this:

```
SimComMon.exe --A:HEADING INDICATOR,degrees as HDG
```
This causes SimComMon to report that variable by its alias name.

```
HDG=35.87491068468636
```
after which the program terminates.

## Get Variables

### Variable names
SimCom does quite some magic with variable names and tries to make things as easy as possible.
This is thanks to the way variable names are passed.

The variableName string consist of 7 parts: VarType:Name:Index,Units,ValueType,Interval,deltaEpsilon
For example:

`A:NAV OBS:1,degrees,FLOAT,1000,0.1`

#### VarType
VarType is optional if you just use the simconnect variables as published on [SimConnect variables](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm) and [SimConnect events](https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Event_IDs.htm)

SimCom considers variables and events the same thanks to the power of the WASimCommander library. Where possible SimCom will assign the correct VarType if you don't provide one. You can find a description of the various var types here https://docs.flightsimulator.com/html/Additional_Information/Reverse_Polish_Notation.htm

#### Name
This is required. This can be a simconnect variable name or a simconnect event name but also custom variables and even calculated vsariables all thanks the power of the WASimCommander library.
The name part can be as simple as `TITLE` or a represent a calculation more about calculations later.

#### Index
Some variables also require an index value. For examle `NAV OBS:1` The index follows after the variable name separated by a colon as shown.

#### Units
Units are there to make life easier. They are an optional in SimCom. Variables can be expressed in a wide range of units as described here https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variable_Units.htm

By default SimCom will use Number as units and thus leaves it up to MSFS what units are returned.

#### ValueType
This is optional and used when dealing with numbers only. By default a 8 byte double floating point variable is assumed unless the Units is "STRING" in which case the default ValueType is assumed to be a string.

Possible value types are "INT8", "INT16", "INT32", "INT64", "FLOAT", "DOUBLE"
INT causes the number to be reported without fractions. The number following INT defines the resolution of the Integer in bits.
INT8 = 0 - 255
INT16 = 0 - 65025
INT32 = 0 - 4228250625
INT64 = 0 - 17878103347812890625 (Just in case you wanted to know)

FLOAT is a 4 byte value representing a floating point value. It's fine in most cases but with modern 64 bit computers the performance difference is minimal. 

DOUBLE is a 8 byte value representing a floating point value with much higher precision.
This is the default ValueType if none is provided. 

It can be useful if you want a variable to be reported in whole numbers without the decimal. For example
```
SimComMon.exe --HEADING INDICATOR,degrees,INT32,500
```

produces
```
HEADING INDICATOR=34
```

You could use an INT16 here as well because the values for heading in degrees ranges from 0 to 359. However INT8 does not have enough resolution as it casn only hold values from 0 to 255.

#### Interval
This is optional. When no interval is given SimComMon will read the variable once only. Interval duration is given in milliseconds. Therefore a value of 1000 causes the variable to be reported on once every second. However, this will only repeat if the value actually changed. The minimum possible interval is 25 milli seconds.

#### DeltaEpsilon
This is optional. DeltaEpsilon is only relevant when interval is set. DeltaEpsilon defines how much the variable must have changed since the last report before it is reported again. this prevents the output to be swamped with tiny irrelevant changes. for example.
```
SimComMon.exe --HEADING INDICATOR,500
```
will cause SimComMon to report the tiniest changes of the aircraft heading but only once every 500 milli seconds.
```
SimComMon.exe --HEADING INDICATOR,500,1
```
will cause SimComMon to report the aircraft heading change only when it changed more than when it reported last and only if at least 500 milliseconds have passed.

### Calculated values

Often it will be useful to have basic calculations done on variables MSFS before itis handed over. For example, you want to know if all three wheels of the landing gear are fully down and locked. For this you could use three separate variables and do the calculation your self like. Each wheel position is reported between 0 (UP) and 1(DOWN). Add all three together and gear is down and locked when the value is three.
```
SimComMon.exe --A:GEAR LEFT POSITION,number,50,0.2 --A:GEAR RIGHT POSITION,number,50,0.2 --A:GEAR CENTER POSITION,number,50,0.2
```
This results in a jumble of numbers.
```
GEAR LEFT POSITION=0
GEAR RIGHT POSITION=0
GEAR CENTER POSITION=0
GEAR LEFT POSITION=0.21944746538065374
GEAR RIGHT POSITION=0.21944746538065374
GEAR CENTER POSITION=0.21944746538065374
GEAR LEFT POSITION=0.41945029818452895
GEAR RIGHT POSITION=0.41945029818452895
GEAR CENTER POSITION=0.41945029818452895
GEAR LEFT POSITION=0.6194617638830096
GEAR RIGHT POSITION=0.6194617638830096
GEAR CENTER POSITION=0.6194617638830096
GEAR LEFT POSITION=0.8194797981996089
GEAR RIGHT POSITION=0.8194797981996089
GEAR CENTER POSITION=0.8194797981996089
```

Or you can make use of the [Reverse Polish Notation (RPN) build into MSFS](https://docs.flightsimulator.com/html/Additional_Information/Reverse_Polish_Notation.htm) syntax to make calculations.

```
SimComMon.exe --(A:GEAR LEFT POSITION,number) (A:GEAR RIGHT POSITION,number) + (A:GEAR CENTER POSITION,number) +,50, 0.1 as GEARPOS
```

This produces a much easier to consume output
```
GEARPOS=0
GEARPOS=0.2250001011416316
GEARPOS=0.42500940011814237
GEARPOS=0.6250109011307359
GEARPOS=0.8250270020216703
GEARPOS=1.0916999001055956
GEARPOS=1.2917016972787678
GEARPOS=1.4917150000110269
GEARPOS=1.6917260996997356
GEARPOS=1.958385399542749
GEARPOS=2.15840370208025
GEARPOS=2.358406703453511
GEARPOS=2.5584116033278406
GEARPOS=2.8251011995598674
```

## Set Variables
SimComMon can also set variables in MSFS. The syntax for the variable names is identical to reading variables but in addition the variable definition also has an equals sign and a value. For example:
```
SimComMon.exe --AUTOPILOT HEADING LOCK DIR,Degrees=123
```
This causes the Autopilot heading bug to be set to 123 degrees after which the program terminates.
Pass an interval if you also wish to monitor the variable.

```
SimComMon.exe --AUTOPILOT HEADING LOCK DIR,Degrees,INT32,100,1 as APHDG=123
```

This causes the AUTOPILOT HEADING LOCK DIR variable to be set to 123 degrees once and reported on 'APHDG=123' and after that it will be reported on again under the name `APHDG` if it changes by at least 1 degrees and it has been at least 100 milli seconds since the last report.
