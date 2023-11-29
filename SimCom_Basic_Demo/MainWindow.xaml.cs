using System;
using System.Windows;
using SimComCon;

namespace SimCom_Basic_Demo
{
    public partial class MainWindow : Window
    {
        SimCom sc;
        SimVal AircraftName;
        SimVal HeadingBug;
        SimVal Heading;
        SimVal Radial;
        SimVal GearToggle;
        SimVal GearPos;
        public MainWindow()
        {
            InitializeComponent();
            sc = new SimCom(1964);  // 1964 is my birthyear :-) Use any number as an identifier for WASimCommander
            sc.OnDataChanged += SimCom_OnDataChanged;
            sc.Connect();

            AircraftName = sc.GetVariable("Title,string", 2000, 0.0);
            HeadingBug = sc.GetVariable("A:AUTOPILOT HEADING LOCK DIR:degrees", 25, 0.01);
            Heading = sc.GetVariable("HEADING INDICATOR:degrees", 25, 0.001);
            Radial = sc.GetVariable("NAV OBS:1:degrees", 25, 0.01);
            GearToggle = sc.GetVariable("GEAR_TOGGLE");
            GearPos = sc.GetVariable("(A:GEAR LEFT POSITION,number) (A:GEAR RIGHT POSITION,number) + (A:GEAR CENTER POSITION,number) +", 25, 0.5);
        }

        private void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
        {
            Dispatcher.BeginInvoke(new Action(() => //  You must use Dispatcher.BeginInvoke to jump back to your UI thread.
            {
                data.Text = $"{simVal.FullName}: {simVal.Value}\n" + data.Text;
            }));
        }
    }
}
