using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Blinky
{ 
    public class Program
    {
        private static readonly OutputPort OnboardLed = new OutputPort(Pins.ONBOARD_LED, false);
        private static readonly OutputPort BreadboardLed = new OutputPort(Pins.GPIO_PIN_D0, false);

        public static void Main()
        {
            // blinky, blinky
            for (int i = 0; i < 10; i++)
            {
                OnboardLed.Write(true);
                Thread.Sleep(100);
                OnboardLed.Write(false);
                Thread.Sleep(100);
            }

            // onboard button pushed turns onboard led on/off
            InterruptPort buttonOnboard = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);
            buttonOnboard.OnInterrupt += buttonOnboard_OnInterrupt;

            // breadboard button pushed turns breadboard led on/off
            InterruptPort buttonBreadboard = new InterruptPort(Pins.GPIO_PIN_D1, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            buttonBreadboard.OnInterrupt += buttonBreadboard_OnInterrupt;

            Thread.Sleep(Timeout.Infinite);
        }

        private static bool _onboardLedState;
        private static bool _breadboardLedState;

        static void buttonOnboard_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            _onboardLedState = !_onboardLedState;
            OnboardLed.Write(_onboardLedState);
        }

        static void buttonBreadboard_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            _breadboardLedState = !_breadboardLedState;
            BreadboardLed.Write(_breadboardLedState);
        }
    }
}
