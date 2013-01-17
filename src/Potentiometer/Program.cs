using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace Potentiometer
{
    public class Program
    {
        public static void Main()
        {
            AnalogInput pot = new AnalogInput(Cpu.AnalogChannel.ANALOG_0);

            while (true)
            {
                Debug.Print(pot.Read().ToString());
                Thread.Sleep(200);
            }
        }
    }
}
