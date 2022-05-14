using System;
using c = System.Console;
using System.Device.Gpio;
using System.Threading;

c.WriteLine("Subsustem is now running. Press any key to exit.");
int pin = 18;
using var controller = new GpioController();
controller.OpenPin(pin, PinMode.Output);
