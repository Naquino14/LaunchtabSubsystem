using System;
using c = System.Console;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

c.WriteLine("Subsystem is now running. Press ctrl+c to exit.");
int powerButtonPin = 3;
bool pressedState = false;
using var controller = new GpioController();
controller.OpenPin(powerButtonPin, PinMode.Input);

//pin update callbacks
controller.RegisterCallbackForPinValueChangedEvent(powerButtonPin, PinEventTypes.Falling, (pin, value) => { pressedState = false; });

controller.RegisterCallbackForPinValueChangedEvent(powerButtonPin, PinEventTypes.Rising, (pin, value) => { pressedState = true; });

// exit warning
AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
{
    c.WriteLine("\rWarning: Subsystem is shutting down. Press any key to continue, or 'r' to restart.");
    var key = c.ReadKey();
    if (key.KeyChar == 'r') // restart subsystem
        try { Process.Start(Process.GetCurrentProcess().MainModule!.FileName!); }
        catch (Exception) { c.WriteLine("Error restarting subsystem."); }
    c.Write('\r');
};

Action Shutdown = () =>
{
    c.WriteLine("\rShutdown request recieved.");
    new Process(){ StartInfo = new("sudo", "shutdown -h now") }.Start();
};

int gestLoopCounter = 0;
for (;;)
{
    if (pressedState)
    {
        if (gestLoopCounter > 6) // aprox 1.25 sec
            Shutdown();
        gestLoopCounter++;
    }
    else
        gestLoopCounter = 0;

    Thread.Sleep(200);
}