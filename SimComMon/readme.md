## SimComMon
SimComMon is a command line demo app to showcase the SimCom library.
Pass all the variables you want to monitor as command line arguments.

```SimComMon.exe "A:NAV OBS:1,degrees,25,1" "HEADING INDICATOR,degrees, 25, 0.1" "TITLE,string"```

Variable name strings consist of 6 parts: Type, Name, Index, Units, Interval, deltaEpsilon

- Type            Optional. Defines the variable type.
- Name            Required. Name of the Variable or Full Reverse Polish Notation calculations.
- Index           Optional. Some variables use an additional index. For example: `A:NAV OBS:1` (Nav radio 1)
- Units           Optional. Units of the variable. For example: degrees, feet, knots, etc. Default type is "NUMBER"
- Interval        Optional. Interval in milliseconds to monitor the variable. Default is 0 (The variable is read only once)
- deltaEpsilon    Optional. The minimum change in value to trigger a notification. Default is 0 (Any change in value triggers a notification)

All types of variables can be monitored. such as A: variables, K: variables, L: variables, etc.

![image](https://github.com/dinther/SimCom/assets/1192916/f2e4c98b-3921-48c3-92cd-f31405b60f75)

