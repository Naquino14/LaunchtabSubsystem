using System;
using c = System.Console;
using System.Device.Gpio;
using System.Diagnostics;

c.WriteLine("Subsustem is now running. Press any key to exit.");
int pin = 3;
using var controller = new GpioController();
controller.OpenPin(pin, PinMode.Input);

//pin update callbacks
controller.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Falling, (pin, value) =>
{
    c.WriteLine($"Falling! Pin {pin} changed to {value}");
});

controller.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Rising, (pin, value) =>
{
    c.WriteLine($"Rising! Pin {pin} changed to {value}");
});

// exit warning
AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
{
    // focus to console
    FocusHandler.Focus(FocusHandler.ShowWindowEnum.Show);
    c.WriteLine("Warning: Subsystem is shutting down. Press any key to continue, or 'r' to restart.");
    var key = c.ReadKey();
    if (key.KeyChar == 'r') // restart subsystem
        try { Process.Start(Process.GetCurrentProcess().MainModule!.FileName!); }
        catch (Exception) { c.WriteLine("Error restarting subsystem."); }
    //controller.ClosePin(pin);
};

c.ReadKey();
