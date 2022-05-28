#pragma this doesnt look like c# anymore...
#define DEBUG

using System;
using c = System.Console;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;
using Iot.Device.Adc;
using System.Device.Spi;
using Iot.Device.DCMotor;
using System.Device.Pwm;

c.WriteLine("Subsystem is now running. Press ctrl+c to exit.");

int powerButtonPin = 3, fanPin = 4;
bool pressedState = false, fanOn = false;
const float fanOnTemp = 65.0f;

using var controller = new GpioController();
controller.OpenPin(powerButtonPin, PinMode.Input);
controller.OpenPin(fanPin, PinMode.Output);

// controller for spi and spi setup
var hardSpiSet = new SpiConnectionSettings(0, 0);
using SpiDevice spi = SpiDevice.Create(hardSpiSet);
using var adc = new Mcp3008(spi);

Func<double, double> NormalizeADCReading = (double reading) => Math.Abs(reading / 10.24); // todo: find constant to map values from x->y to 0->100

// ok i tried like classes, structs, anonymous types, and records but none of them worked
// so heres a massive tuple instead
var GetCellVoltages = () =>
{
    (double Cell1, double Cell2, double Cell3, double Cell4) readings = new();
    readings.Cell1 = NormalizeADCReading(adc.Read(0));
    readings.Cell2 = NormalizeADCReading(adc.Read(1));
    readings.Cell3 = NormalizeADCReading(adc.Read(2));
    readings.Cell4 = NormalizeADCReading(adc.Read(3));
    return readings;
};

var TestADC = () => NormalizeADCReading(adc.Read(0));

// get cpu temp
ProcessStartInfo readtempPsi = new() { FileName = "vcgencmd", Arguments = "measure_temp", RedirectStandardOutput = true };
Func<float> GetCpuTemp = () =>
{
    Process tProcess = new() { StartInfo = readtempPsi };
    tProcess.Start();
    return float.Parse(tProcess.StandardOutput.ReadToEnd().Split('=')[1].Split('\'')[0]);
};

//pin update callbacks
controller.RegisterCallbackForPinValueChangedEvent(powerButtonPin, PinEventTypes.Falling, (pin, value) => { pressedState = false; });

controller.RegisterCallbackForPinValueChangedEvent(powerButtonPin, PinEventTypes.Rising, (pin, value) => { pressedState = true; });

using DCMotor motor = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 50));

Action FanOn = () => { controller.Write(fanPin, PinValue.High); fanOn = true; }; // npn transistor
Action FanOff = () => { controller.Write(fanPin, PinValue.Low); fanOn = false;};
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

for (;;)
{
    // get cpu temp
    if (loopCounter % 30 == 0)
    {
        //temp = GetCpuTemp();
        c.WriteLine($"CPU temp: {temp}'C");
        //if (temp > fanOnTemp && !fanOn) // turn fan on if its too hot
        //    FanOn();
        //else if (temp <= fanOnTemp && fanOn)
        //    FanOff();
        ToggleFan();
    }

    if (loopCounter % 3 == 0)
    {
        // todo once the mcp3008 arrives: sample battery voltage
        //var cellVoltages = GetCellVoltages();
        //var value = TestADC();
        //c.WriteLine($"{value}%");
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
    {
        hold = true; 
        Shutdown(); 
        hold = false; 
        c.Write("Held! "); 
    }
    else if (tap && gestLoopCounter > 1 && doubleTapOk)
    {
        doubleTapOk = false;
        ToggleFan(); 
        c.Write("Double tapped! "); 
    }
    
    if (tap && !pressedState)
        doubleTapOk = true;

    //c.WriteLine(pressedState + " " + gestLoopCounter);
    loopCounter++;
    loopCounter %= 10000;
    Thread.Sleep(100);
}