using SimComLib;


SimCom sc = new SimCom(1964);
sc.OnConnection += Sc_OnConnection;
sc.OnDataChanged += SimCom_OnDataChanged;
sc.Connect();


while (true)
{

}


void Sc_OnConnection(SimCom simCom, SimCom_Connection_Status Connection_Status)
{
    switch (Connection_Status)
    {
        case SimCom_Connection_Status.CONNECTED:
            {
                SimVal simVal = sc.GetVariable(args[0]);
                if (simVal.Units == "STRING") sc.SetVariable(simVal, args[1]);
                else sc.SetVariable(simVal, System.Convert.ToDouble(args[1]));
                break;
            }
    };
}

void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
{
    Console.WriteLine($"{simVal.FullName}={simVal.Value}");
}
