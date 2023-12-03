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
            SimCom sc = new SimCom(1964);  // 1964 is my birthyear :-) Use any number as an identifier for WASimCommander
            sc.OnDataChanged += SimCom_OnDataChanged;
            sc.Connect();
            sc.GetVariable("Title,string", 2000, 0.0);
            sc.GetVariable("A:AUTOPILOT HEADING LOCK DIR:degrees", 25, 0.01);
            sc.GetVariable("HEADING INDICATOR:degrees", 25, 0.001);
            sc.GetVariable("NAV OBS:1:degrees", 25, 0.01);
            sc.GetVariable("GEAR_TOGGLE");
            sc.GetVariable("(A:GEAR LEFT POSITION,number) (A:GEAR RIGHT POSITION,number) + (A:GEAR CENTER POSITION,number) +", 25, 0.5);
        }

        private void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
        {
            Dispatcher.BeginInvoke(new Action(() => //  You must use Dispatcher.BeginInvoke to jump back to your UI thread.
            {
                TextBox1.Text = $"{simVal.FullName}: {simVal.Value}\n" + TextBox1.Text;
            }));
            Console.WriteLine();
        }
    }
}
