using SimComLib;

// See https://aka.ms/new-console-template for more information
SimCom sc = new SimCom(1964);  // 1964 is my birthyear :-) Use any number as an identifier for WASimCommander
sc.OnDataChanged += SimCom_OnDataChanged;
sc.Connect();
sc.GetVariable("HEADING INDICATOR:degrees", 25, 0.1);
while (true)
{

}


void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
{
    Console.WriteLine($"{simVal.FullName}={simVal.Value}");
}
