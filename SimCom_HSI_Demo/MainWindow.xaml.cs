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
        private SimVal GearDown;
        private SimVal GearUp;
        private SimVal GearSet;
        private SimVal GearToggle;
        private SimVal GearPos;
        private SimVal AircraftName;
        private SimVal HeadingBugSet;
        private SimVal Vor1Set;
        private SimVal APMaster;
        private SimVal APHDG;
        private SimVal APHDG_CMD;
        private SimVal APROLL_CMD;
        private SimVal APALT;
        private SimVal APALT_CMD;
        private SimVal APVS;
        private SimVal APVS_CMD;
        private SimVal APFLC;
        private SimVal APFLC_CMD;
        private SimVal APPitch_CMD;
        private DispatcherTimer renderTimer;
        private bool _needRender = true;
        private long lastScroll;
        public MainWindow()
        {
            InitializeComponent();

            simCom = new SimCom(1964);
            simCom.OnDataChanged += SimCom_OnDataChanged;

            try
            {
                simCom.Connect();
            }
            catch (SimCom_Exception e)
            {
                statusText.Text = e.Message;
            }
            if (!simCom.Connected) { throw new Exception($"Failed to connect with simulator"); }
            AircraftName = simCom.GetVariable("Title,string", 2000, 0.0);
            GearPos = simCom.GetVariable("(A:GEAR LEFT POSITION,number) (A:GEAR RIGHT POSITION,number) + (A:GEAR CENTER POSITION,number) +", 25, 0.1);
            HeadingBug = simCom.GetVariable("A:AUTOPILOT HEADING LOCK DIR,degrees", 25, 0.01);
            Heading = simCom.GetVariable("HEADING INDICATOR,degrees", 25, 0.1);
            Radial = simCom.GetVariable("NAV OBS:1,degrees", 25, 0.01);
            GearDown = simCom.GetVariable("GEAR_DOWN");
            GearUp = simCom.GetVariable("GEAR_UP");
            GearSet = simCom.GetVariable("GEAR_SET");
            GearToggle = simCom.GetVariable("GEAR_TOGGLE");
            HeadingBugSet = simCom.GetVariable("HEADING_BUG_SET", 25, 0.01);
            Vor1Set = simCom.GetVariable("VOR1_SET");
            Vor1Set.Value = Radial.Value;  //  initialise to radial as VOR1_SET is really an event
            APMaster = simCom.GetVariable("AUTOPILOT MASTER", 100);
            APHDG = simCom.GetVariable("AUTOPILOT HEADING LOCK", 100);
            APHDG_CMD = simCom.GetVariable("AP_HDG_HOLD_ON");
            APROLL_CMD = simCom.GetVariable("AP_BANK_HOLD");
            APALT = simCom.GetVariable("AUTOPILOT ALTITUDE LOCK", 100);
            APALT_CMD = simCom.GetVariable("AP_ALT_HOLD");
            APFLC = simCom.GetVariable("AUTOPILOT FLIGHT LEVEL CHANGE", 100);
            APFLC_CMD = simCom.GetVariable("FLIGHT_LEVEL_CHANGE");
            APVS = simCom.GetVariable("AUTOPILOT VERTICAL HOLD", 100);
            APVS_CMD = simCom.GetVariable("AP_VS_ON");
            APPitch_CMD = simCom.GetVariable("AP_PITCH_LEVELER_ON");
            initUI();
        }

        private void initUI()
        {
            Title = (string)AircraftName.Value;
            renderGear();
            renderInstrument();
        }

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
                if (simVal == APMaster) renderButton(AP_Btn, APMaster.Value == 1);
                if (simVal == APHDG) renderButton(HDG_Btn, APHDG.Value == 1);
                if (simVal == APALT) renderButton(ALT_Btn, APALT.Value == 1);
                if (simVal == APVS) renderButton(VS_Btn, APVS.Value == 1);
                if (simVal == APFLC) renderButton(FLC_Btn, APFLC.Value == 1);
            }));
        }

        private void renderGear()
        {
            UpOff.Visibility = GearPos.Value == 0 ? Visibility.Visible : Visibility.Hidden;
            DownTrans.Visibility = GearPos.Value != 3 && GearPos.Value - GearPos.OldValue > 0 ? Visibility.Visible : Visibility.Hidden;
            UpTrans.Visibility = GearPos.Value != 0 && GearPos.Value - GearPos.OldValue < 0 ? Visibility.Visible : Visibility.Hidden;
            DownLocked.Visibility = GearPos.Value == 3.0 ? Visibility.Visible : Visibility.Hidden;
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
            Vor1Set.Value += step;
            while (Vor1Set.Value < 0) Vor1Set.Value += 360;
            Vor1Set.Value %= 360;
            Vor1Set.Set(Vor1Set.Value);
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
            HeadingBug.Value += step;
            while (HeadingBug.Value < 0) HeadingBug.Value += 360;
            HeadingBug.Value %= 360;
            HeadingBug.Set(HeadingBug.Value);
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
                simVal.Set(Convert.ToDouble(resultText.Text));
            }

            //resultText.Text = $"{simVal.Value}";
        }

        private void HDG_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (APHDG.Value == 0) APHDG_CMD.Set(1);
            else APROLL_CMD.Set(1);
        }

        private void VS_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (APVS.Value == 0) APVS_CMD.Set(1);
            else APPitch_CMD.Set(1);
        }

        private void FLC_Btn_Click(object sender, RoutedEventArgs e)
        {
            APFLC_CMD.Set(1);
        }

        private void ALT_Btn_Click(object sender, RoutedEventArgs e)
        {
            APALT_CMD.Set(1);
        }
    }
}
