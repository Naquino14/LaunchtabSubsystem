#define DEBUG

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
Action ToggleFan = () => 
{
    fanOn = !fanOn;
    if (fanOn)
        FanOn();
    else
        FanOff();
};

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

int loopCounter = 0, gestLoopCounter = 0, prevCounter = 0;
bool tap = false, hold = false, doubleTapOk = false;
float temp = 0.0f;

ProcessStartInfo readtempPsi = new () { FileName = "vcgencmd", Arguments = "measure_temp", RedirectStandardOutput = true };
for (;;)
{
    // get cpu temp
    if (loopCounter % 30 == 0)
    {
        Process tProcess = new() { StartInfo = readtempPsi };
        tProcess.Start();
        temp = float.Parse(tProcess.StandardOutput.ReadToEnd().Split('=')[1].Split('\'')[0]);
        //c.WriteLine($"CPU temp: {temp}'C");
        if (temp > fanOnTemp && !fanOn) // turn fan on if its too hot
            FanOn();
        else if (temp <= fanOnTemp && fanOn)
            FanOff();

        // todo once the mcp3008 arrives: sample battery voltage
    }

    if (pressedState)
    { gestLoopCounter++; prevCounter = loopCounter; tap = true;}
    else
        gestLoopCounter = 0;
    if (gestLoopCounter > 4)
        tap = false;
    
    if (loopCounter - prevCounter > 10)
    {
        hold = false;
        tap = false;
        doubleTapOk = false;
    }

    if (gestLoopCounter > 10 && !hold)
        {hold = true; Shutdown(); hold = false; }
    else if (tap && gestLoopCounter > 1 && doubleTapOk)
        {doubleTapOk = false; ToggleFan();}
    
    if (tap && !pressedState)
        doubleTapOk = true;

    //c.WriteLine(pressedState + " " + gestLoopCounter);
    loopCounter++;
    loopCounter %= 10000;
    Thread.Sleep(100);
}