using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SimComLib;

namespace SimCom_HSI_Demo
{
    public partial class MainWindow : Window
    {
        public SimCom simCom;
        private SimVal Heading;
        private SimVal Radial;
        private SimVal HeadingBug;
        private SimVal CDI;
        private SimVal GS;
        private SimVal GearDown;
        private SimVal GearUp;
        private SimVal GearSet;
        private SimVal GearToggle;
        private SimVal GearPos;
        private SimVal AircraftName;
        private SimVal HeadingBugSet;
        private SimVal Vor1Set;
        private SimVal APMaster;
        private SimVal APMaster_CMD;
        private SimVal APHDG;
        private SimVal APHDG_CMD;
        private SimVal APROLL_CMD;
        private SimVal APALT;
        private SimVal APALT_CMD;
        private SimVal APALT_VAL;
        private SimVal APVS;
        private SimVal APVS_CMD;
        private SimVal APVS_VAL;
        private SimVal APNAV;
        private SimVal APNAV_CMD;
        private SimVal APAPR;
        private SimVal APFLC;
        private SimVal APFLC_CMD;
        private SimVal APPitch_CMD;

        private DispatcherTimer reconnectTimer;
        private long lastScroll;
        private long lastConnectAttempt;
        public MainWindow()
        {
            InitializeComponent();

            simCom = new SimCom(1964);
            simCom.OnDataChanged += SimCom_OnDataChanged;
            simCom.OnConnection += SimCom_OnConnection;
            reconnectTimer = new DispatcherTimer();
            reconnectTimer.Interval = new TimeSpan(0, 0, 0, 1);
            reconnectTimer.Tick += delegate (object? obj, EventArgs evt)
            {
                simCom.Connect();
            };
            initVariables();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            simCom.Connect();
        }

        private void SimCom_OnConnection(SimCom simCom, SimCom_Connection_Status connection_Status)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (connection_Status == SimCom_Connection_Status.CONNECTED)
                {
                    reconnectTimer.Stop();
                    statusText.Text = "Connected";
                    
                }
                else
                {
                    statusText.Text = "Connecting ...";
                    reconnectTimer.Start();
                }
            }));
        }

        private void initVariables()
        {
            Radial = simCom.GetVariable("A:NAV OBS:1,degrees,25,0.01");
            AircraftName = simCom.GetVariable("Title,string,2000, 0.0");
            GearPos = simCom.GetVariable("(A:GEAR LEFT POSITION,number) (A:GEAR RIGHT POSITION,number) + (A:GEAR CENTER POSITION,number) +,25, 0.1");
            HeadingBug = simCom.GetVariable("A:AUTOPILOT HEADING LOCK DIR,degrees,25, 0.01");
            Heading = simCom.GetVariable("HEADING INDICATOR,degrees, 25, 0.1");

            CDI = simCom.GetVariable("HSI CDI NEEDLE, 50, 0.1");
            GS = simCom.GetVariable("HSI GSI NEEDLE, 50, 0.1");
            GearDown = simCom.GetVariable("GEAR_DOWN");
            GearUp = simCom.GetVariable("GEAR_UP");
            GearSet = simCom.GetVariable("GEAR_SET");
            GearToggle = simCom.GetVariable("GEAR_TOGGLE");
            HeadingBugSet = simCom.GetVariable("HEADING_BUG_SET, 25, 0.01");
            Vor1Set = simCom.GetVariable("VOR1_SET");
            //Vor1Set.Value = Radial.Value;  //  initialise to radial as VOR1_SET is really an event
            APMaster = simCom.GetVariable("AUTOPILOT MASTER, 100");
            APMaster_CMD = simCom.GetVariable("AP_MASTER");
            APHDG = simCom.GetVariable("AUTOPILOT HEADING LOCK, 100");
            APHDG_CMD = simCom.GetVariable("AP_HDG_HOLD_ON");
            APROLL_CMD = simCom.GetVariable("AP_BANK_HOLD");
            APALT = simCom.GetVariable("AUTOPILOT ALTITUDE LOCK, 100");
            APALT_CMD = simCom.GetVariable("AP_ALT_HOLD");
            APALT_VAL = simCom.GetVariable("AUTOPILOT ALTITUDE LOCK VAR,feet, 100, 100");
            APFLC = simCom.GetVariable("AUTOPILOT FLIGHT LEVEL CHANGE, 100");
            APFLC_CMD = simCom.GetVariable("FLIGHT_LEVEL_CHANGE");
            APVS = simCom.GetVariable("AUTOPILOT VERTICAL HOLD, 100");
            APVS_CMD = simCom.GetVariable("AP_VS_ON");
            APVS_VAL = simCom.GetVariable("AUTOPILOT VERTICAL HOLD VAR,feet/minute, 100, 100");
            APALT_VAL = simCom.GetVariable("AUTOPILOT ALTITUDE LOCK VAR,feet, 100, 100");
            APNAV = simCom.GetVariable("AUTOPILOT NAV1 LOCK, 100");
            APNAV_CMD = simCom.GetVariable("AP_NAV1_HOLD");
            APPitch_CMD = simCom.GetVariable("AP_PITCH_LEVELER_ON");
        }

        private void SimCom_OnDataChanged(SimCom simCom, SimVal simVal)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (simVal == AircraftName)
                {
                    if (AircraftName.Value != "") Title = AircraftName.Value;
                    else Title = "SimCom HSI Demo";
                }
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
                if (simVal == CDI)
                {
                    RadialNeedle.X = CDI.Value * 0.5;
                }
                if (simVal == GS)
                {
                    GSNeedle.Y = GS.Value * 0.5;
                }
                if (simVal == APALT_VAL)
                {
                    AltKnob.Angle = -APALT_VAL.Value * 0.1;
                }
                if (simVal == APVS_VAL)
                {
                    VSKnob.Angle = -APVS_VAL.Value * 2;
                }
                if (simVal == APMaster) renderButton(AP_Btn, APMaster.Value == 1);
                if (simVal == APHDG) renderButton(HDG_Btn, APHDG.Value == 1);
                if (simVal == APALT) renderButton(ALT_Btn, APALT.Value == 1);
                if (simVal == APVS) renderButton(VS_Btn, APVS.Value == 1);
                if (simVal == APFLC) renderButton(FLC_Btn, APFLC.Value == 1);
                if (simVal == APNAV) renderButton(NAV_Btn, APNAV.Value == 1);
            }));
        }

        private void renderGear()
        {
            UpOff.Visibility = GearPos.Value < 0.2 ? Visibility.Visible : Visibility.Hidden;
            DownTrans.Visibility = GearPos.Value != 3 && GearPos.Value - GearPos.OldValue > 0 ? Visibility.Visible : Visibility.Hidden;
            UpTrans.Visibility = GearPos.Value > 0.2 && GearPos.Value - GearPos.OldValue < 0 ? Visibility.Visible : Visibility.Hidden;
            DownLocked.Visibility = GearPos.Value > 2.8 ? Visibility.Visible : Visibility.Hidden;
        }

        private void renderInstrument()
        {
            HeadingBugIndicator.Angle = HeadingBug.Value;
            RadialIndicator.Angle = -Heading.Value + Radial.Value;
            HeadingIndicator.Angle = -Heading.Value;
            HeadingBugKnob.Angle = HeadingBug.Value * 5;
            RadialKnob.Angle = Radial.Value * 5;
        }

        private void renderButton(Button button, bool state)
        {
            if (state)
            {
                button.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 0));
                button.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 0));
            }
            else
            {
                button.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 160, 160, 160));
                button.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 160, 160, 160));
            }
        }
           
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            simCom.disconnect();
        }

        private void RadialKnobGrid_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            long newTime = Stopwatch.GetTimestamp();
            long deltaTime = newTime - lastScroll;
            int step = e.Delta / 120;
            if (deltaTime < 220000)
            {
                step *= 5;
            }
            float newVal = Vor1Set.Value + step;
            while (newVal < 0) newVal += 360;
            newVal %= 360;
            Vor1Set.Set(newVal);
            lastScroll = newTime;
        }

        private void HeadingBugKnobGrid_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            long newTime = Stopwatch.GetTimestamp();
            long deltaTime = newTime - lastScroll;
            int step = e.Delta / 120;
            if (deltaTime < 220000)
            {
                step *= 5;
            }
            float newVal = HeadingBug.Value + step;
            while (newVal < 0) newVal += 360;
            newVal %= 360;
            HeadingBug.Set(newVal);
            lastScroll = newTime;
        }

        private void AltKnobGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            long newTime = Stopwatch.GetTimestamp();
            long deltaTime = newTime - lastScroll;
            int step = e.Delta / 120;
            if (deltaTime < 220000)
            {
                step *= 10;
            }
            double val = APALT_VAL.Value + (step * 100);
            double val1 = Math.Round(val / 100) * 100;
            APALT_VAL.Set(Math.Max(0, val1));
            lastScroll = newTime;
        }

        private void VRKnobGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            long newTime = Stopwatch.GetTimestamp();
            long deltaTime = newTime - lastScroll;
            int step = e.Delta / 120;
            if (deltaTime < 220000)
            {
                step *= 5;
            }
            double val = APVS_VAL.Value + (step * 50);
            double val1 = Math.Round(val / 50) * 50;
            APVS_VAL.Set(val1);
            lastScroll = newTime;
        }

        private void UpOff_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GearToggle.Set();
        }

        private void GetButton_Click(object sender, RoutedEventArgs e)
        {
            SimVal simVal = simCom.GetVariable(codeText.Text);
            resultText.Text = $"{simVal.Value}";
        }

        private void SetButton_Click(object sender, RoutedEventArgs e)
        {
            SimVal simVal = simCom.GetVariable(codeText.Text);
            if (simVal.Units == "STRING")
            {
                simVal.Set(resultText.Text);
            }
            else
            {
                double val = 0;
                try
                {
                     val = Convert.ToDouble(resultText.Text);  
                }
                catch
                {
                }
                simVal.Set(val);
            }

            //resultText.Text = $"{simVal.Value}";
        }

        private void AP_Btn_Click(object sender, RoutedEventArgs e)
        {
            APMaster_CMD.Set(1);
        }

        private void HDG_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (APHDG.Value == 0) APHDG_CMD.Set(1);
            else APROLL_CMD.Set(1);
        }

        private void ALT_Btn_Click(object sender, RoutedEventArgs e)
        {
            APALT_CMD.Set(1);
        }

        private void FLC_Btn_Click(object sender, RoutedEventArgs e)
        {
            APFLC_CMD.Set(1);
        }

        private void VS_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (APVS.Value == 0) APVS_CMD.Set(1);
            else APPitch_CMD.Set(1);
        }

        private void NAV_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (APNAV.Value == 0)
            {
                APNAV_CMD.Set(1);
            } else {
                APNAV_CMD.Set(0);
            }
        }
    }
}
