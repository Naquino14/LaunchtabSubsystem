#define DEBUG

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
        try { Process.Start(Environment.ProcessPath!); }
        catch (Exception) { c.WriteLine("Error restarting subsystem."); }
    c.Write('\r');
};

Action Shutdown = () =>
{
    c.WriteLine("\rShutdown request recieved.");
    new Process(){ StartInfo = new("sudo", "shutdown -h now") }.Start();
};

int gestLoopCounter = 0;
bool tap = false, doubleTapped = false, held = false, recording = false;
for (;;)
{
    if (pressedState)
    {
        tap = !tap;
        if (gestLoopCounter > 6) // press and hold aprox 1.25 sec
            held = true;
        else if (gestLoopCounter > 2 && !tap) // double tap fired
            doubleTapped = true;
        gestLoopCounter++;
    }
    else if (!tap)
        gestLoopCounter = 0;

    if (held)
    #if DEBUG
        c.WriteLine("Held.");
    #else
        ShutDown();
    #endif
    if (doubleTapped)
    #if DEBUG
        c.WriteLine("Double tapped.");
    #else
        ;
    #endif
    if (doubleTapped && held)
    #if DEBUG
        c.WriteLine("Double tapped and held.");
    #else
        ;
    #endif

    Thread.Sleep(200);
}