using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Blinky
{ 
    public class Program
    {
        private static readonly OutputPort OnboardLed = new OutputPort(Pins.ONBOARD_LED, false);

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

            // button on/off on push
            InterruptPort button = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);
            button.OnInterrupt += button_OnInterrupt;

            Thread.Sleep(Timeout.Infinite);
        }

        private static bool _onboardLedState;

        static void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            _onboardLedState = !_onboardLedState;
            OnboardLed.Write(_onboardLedState);
        }
    }
}
