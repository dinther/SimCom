# SimCom
C# Wrapper for the excellent library [WASIMCommander](https://github.com/mpaperno/WASimCommander) by Max Paperno.
SimCom allows c# programmers to access the many variables and events in Microsoft flight Simulator 2020. These include all the SimConnect [Variables](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm) and [Events](https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Event_IDs.htm) but also all the Local variables. More details on the [WASimConnect github](https://github.com/mpaperno/WASimCommander)
This wrapper is under development and is nowhere near ready for use. but you can play with it, critique it and make suggestions which are very welcome.

![image](https://github.com/dinther/SimCom/assets/1192916/b341e068-8c65-449b-ad50-7337c61087e3)

I want the API as simple as possible. Since events and variables are almost the same thing I have attempted to make the use of WASimCommander easier with this wrapper by treating everything as a sim value and leave SimCom to deal with the differences. 
It should not matter if you are dealing with a Simconnect Variable or Event. Drill down into Local variables, create your own Events or entire RPN's. Everything is a SimVal. The power really comes from WASimCommander. All I do here is to make it more accessible for programmers who want quick results and a small learning curve.

Here is a basic example how SimCom is initialised.

``` C#
public MainWindow()
{
    InitializeComponent();

    SimCom simCom = new SimCom(1964);
    simCom.OnDataChanged += SimCom_OnDataChanged;
    simCom.connect();

    simCom.GetVariable("Title,string", 2000, 0.0);
    simCom.GetVariable("A:AUTOPILOT HEADING LOCK DIR:degrees", 25, 0.01);
    simCom.GetVariable("HEADING INDICATOR:degrees", 25, 0.001);
    simCom.GetVariable("NAV OBS:1:degrees", 25, 0.01);
    simCom.GetVariable("A:GROUND ALTITUDE,meters");
    simCom.GetVariable("(A:GEAR LEFT POSITION,number) (A:GEAR RIGHT POSITION,number) + (A:GEAR CENTER POSITION,number) +",25, 0.2);
}

private void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
{
    //  You must use Dispatcher.BeginInvoke to jump back to your UI thread.
    Dispatcher.BeginInvoke(new Action(() =>
    {
        Console.WriteLine($"{simVal.FullName}: {simVal.Value}\n" + data.Text);
    }));
}
```

![image](https://github.com/dinther/SimCom/assets/1192916/2efff5ee-0504-415a-8d94-e412e3e19cf9)

Since we have read only variables in Simconnect and variables that can notify when changed and events that can be send and received with data, I decided to consider all events and variables equal. I call them all Simulator Values (SimVal).
Note how some calls to `client.getVariable` have the optional interval parameters and some don't. In each case it returns a SimVal object but only SimVal's that were given a refresh interval will notify your app when their value changes beyond the given treshold.
Repeated calls of `client.getVariable` returns the same SimVal object if the full name is the same. This is all managed by the SimCOM class.
This same SimVal object can also be used to set a value. for example `HeadingBug.set(HeadingBug.Value + 10);`

You don't have to worry about what type they are, (A: B: K: ect.) this can just be part of the name or you can rely on their simconnect defaults of A: or K: . Variable name can have up to 4 parts separated by a colons or a comma 

`Type:Name:Index,Units`

| Part | Description|
| ------------- | ------------- |
|Type|(Optional, A or K by default depends on the spaces or underscores in the name)|
|Name|(Required)|
|Index|(Optional, 0 by default)|
|Units|(Optional, empty string by default)|

examples:

```
"HEADING INDICATOR"                  -> A:HEADING INDICATOR
"AUTOPILOT HEADING LOCK DIR,degrees" -> A:AUTOPILOT HEADING LOCK DIR,DEGREES
"COM1_VOLUME_SET"                    -> K:COM1_VOLUME_SET
"L:MY FANCY DOODAD:1"                -> L:MY FANCY DOODAD:1
```

`SimVal.FullName` shows the full translated name.

WASimCommander uses different methods and class constructors depending on the type of variable but I want SimCom to handles all that logic.
Contrary to the Simconnect API, the WASM module provides synchronous access to values which makes initialization so much easier. You just call:

``` C#
SimVal HDGBug = client.getVariable("A:AUTOPILOT HEADING LOCK DIR,degrees");
Console.WriteLine(HDGBug.Value);
```

You can also pass in the optional interval and deltaEpsilon which still returns the value synchronously but also saves a Data Request in WASimCommander which causes the OnDataChanged event to fire every time that value changes more than deltaEpsilon.

With Winforms and WPF forms you would normally have a threading problem because WASimCommander is running in a different thread than the WPF application. This can be solved in two ways.

1. Run a `DispatcherTimer` on the MainWindow and let it update UI related content. This makes it possible to run WASimCommander at a set speed doing nothing more than reading and storing values while users application renders the UI at it's own pace.
2. Run all your UI code like this

```
        private void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
        {
            Debug.WriteLine($"{simVal.Name}: {simVal.Value} : {simVal.OldValue}");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (simVal == AircraftName) Title = AircraftName.Value;
                if (simVal == GearPos) renderGear();
                if (simVal == HeadingBug)
                {
                    HeadingBugIndicator.Angle = HeadingBug.Value;
                    HeadingBugKnob.Angle = HeadingBug.Value * 5;
                }
                if (simVal == Heading)
                {
                    HeadingIndicator.Angle = -Heading.Value;
                    RadialIndicator.Angle = -Heading.Value + Radial.Value;
                }
                if (simVal == Radial)
                {
                    RadialKnob.Angle = Radial.Value * 5;
                    RadialIndicator.Angle = -Heading.Value + Radial.Value;
                }
            }));
        }
```

