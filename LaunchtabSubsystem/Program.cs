using System;
using c = System.Console;
using System.Device.Gpio;
using System.Threading;

c.WriteLine("Blinking led!");
int pin = 18;
using var controller = new GpioController();
controller.OpenPin(pin, PinMode.Output);
bool state = false;
for (;;)
{
    controller.Write(pin, state);
    state = !state;
    Thread.Sleep(500);
}