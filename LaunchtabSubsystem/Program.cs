﻿#define DEBUG

using System;
using c = System.Console;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

c.WriteLine("Subsystem is now running. Press ctrl+c to exit.");

int powerButtonPin = 3, fanPin = 4;
bool pressedState = false, fanOn = false;
const float fanOnTemp = 65.0f;

using var controller = new GpioController();
controller.OpenPin(powerButtonPin, PinMode.Input);
controller.OpenPin(fanPin, PinMode.Output);

//pin update callbacks
controller.RegisterCallbackForPinValueChangedEvent(powerButtonPin, PinEventTypes.Falling, (pin, value) => { pressedState = false; });

controller.RegisterCallbackForPinValueChangedEvent(powerButtonPin, PinEventTypes.Rising, (pin, value) => { pressedState = true; });

Action FanOn = () => { controller.Write(fanPin, PinValue.Low); fanOn = true; }; // pnp transistor
Action FanOff = () => { controller.Write(fanPin, PinValue.High); fanOn = false;};

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
    // get cpu temp
    Process tProcess = new() { StartInfo = new() { FileName = "vcgencmd", Arguments = "measure_temp", RedirectStandardOutput = true } };
    tProcess.Start();
    float temp = float.Parse(tProcess.StandardOutput.ReadToEnd().Split('=')[1].Split('\'')[0]);
    //c.WriteLine($"CPU temp: {temp}'C");
    if (temp > fanOnTemp && !fanOn) // turn fan on if its too hot
        FanOn();
    else if (temp <= fanOnTemp && fanOn)
        FanOff();

    if (pressedState) // todo: fix logic
    {
        tap = !tap;
        if (gestLoopCounter > 6) // press and hold aprox 1.25 sec
            { held = true; gestLoopCounter = 0;}
        else if (gestLoopCounter > 2 && !tap) // double tap fired
            { doubleTapped = true; gestLoopCounter = 0; }
        gestLoopCounter++;
    }
    else if (!tap)
        gestLoopCounter = 0;

    if (held)
        c.WriteLine("Held.");
    else if (doubleTapped)
        c.WriteLine("Double tapped.");
    else if (doubleTapped && held)
        c.WriteLine("Double tapped and held.");

    Thread.Sleep(200);
}