﻿using System;
using System.Windows;
using SimComLib;

namespace SimCom_Basic_Demo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //  Ensure WASimcommander module is installed in the community folder
            if (FlightSimulatorInstal.installModule("wasimcommander-module") == ModuleInstallResult.RestartRequired)
            {
                TextBox1.Text = "WASimCommander Module installed. Restart Flight Simulator to activate.\n" + TextBox1.Text;
                return;
            }
            SimCom sc = new SimCom(1964);  // 1964 is my birthyear :-) Use any number as an identifier for WASimCommander
            sc.OnDataChanged += SimCom_OnDataChanged;
            sc.Connect();
            sc.GetVariable("Title,string, 2000, 0.0");
            sc.GetVariable("A:AUTOPILOT HEADING LOCK DIR:degrees, 25, 0.01", "APHDG");
            sc.GetVariable("HEADING INDICATOR:degrees, 25, 0.001");
            sc.GetVariable("NAV OBS:1:degrees, 25, 0.01");
            sc.GetVariable("GEAR_TOGGLE");
            sc.GetVariable("(A:GEAR LEFT POSITION,number) (A:GEAR RIGHT POSITION,number) + (A:GEAR CENTER POSITION,number) +, 25, 0.05", "GEARPOS");
        }

        private void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
        {
            Dispatcher.BeginInvoke(new Action(() => //  You must use Dispatcher.BeginInvoke to jump back to your UI thread.
            {
                string valName = simVal.Alias == "" ? simVal.FullName : simVal.Alias;
                TextBox1.Text = $"{valName}: {simVal.Value}\n" + TextBox1.Text;
            }));
            Console.WriteLine();
        }
    }
}
