using System;
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
            switch (FlightSimulatorInstal.installModule("wasimcommander-module"))
            {
                case ModuleInstallResult.CommunityFolderNotFound: TextBox1.Text = "Microsoft Flight Simulator Community folder not found."; break;
                case ModuleInstallResult.FlightSimulatorNotFound: TextBox1.Text = "Microsoft Flight Simulator not found."; break;
                case ModuleInstallResult.RestartRequired: TextBox1.Text = "WASimCommander Module installed. Restart Flight Simulator to activate."; break;
                case ModuleInstallResult.Installed: TextBox1.Text = "WASimCommander Module installed."; break;
                //case ModuleInstallResult.Failed: TextBox1.Text = "WASimCommander Module installation failed."; break;
            }

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SimCom simCom = new SimCom(1990);  // Use any number as an identifier for WASimCommander
            simCom.OnDataChanged += SimCom_OnDataChanged;
            //  Wait untill connected
            while (!simCom.Connect()) { };

            SimVal simVal = simCom.GetVariable("(A:AUTOPILOT ALTITUDE LOCK VAR, feet) 1000 + (>K:AP_ALT_VAR_SET_ENGLISH) (>H:AP_KNOB_Up)");
            //simCom.GetVariable("Title,string, 2000, 0.0");
            //simCom.GetVariable("A:AUTOPILOT HEADING LOCK DIR:degrees, 50, 0.01", "APHDG");
            //simCom.GetVariable("HEADING INDICATOR:degrees, 50, 0.01");
            //simCom.GetVariable("NAV OBS:1:degrees, 50, 0.01");
            //simCom.GetVariable("GEAR_TOGGLE");
            //simCom.GetVariable("(A:GEAR LEFT POSITION,number) (A:GEAR RIGHT POSITION,number) + (A:GEAR CENTER POSITION,number) +, 25, 0.05", "GEARPOS");

        }
    }
}
