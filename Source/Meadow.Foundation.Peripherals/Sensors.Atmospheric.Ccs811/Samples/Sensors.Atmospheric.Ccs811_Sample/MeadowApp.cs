﻿using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Atmospheric;
using Meadow.Units;

namespace Sensors.AirQuality.Ccs811_Sample
{
    public class MeadowApp
        : App<F7Micro, MeadowApp>
    {
        Ccs811 sensor;

        public MeadowApp()
        {
            Console.WriteLine("Initializing...");

            // configure our sensor on the Bus
            var i2c = Device.CreateI2cBus(Meadow.Hardware.I2cBusSpeed.Fast);
            sensor = new Ccs811(i2c);

            //==== IObservable 
            // Example that uses an IObersvable subscription to only be notified
            // when the temperature changes by at least a degree, and humidty by 5%.
            // (blowing hot breath on the sensor should trigger)
            var consumer = Ccs811.CreateObserver(
                handler: result => {
                    Console.WriteLine($"Observer triggered:");
                    Console.WriteLine($"   new CO2: {result.New.Co2?.PartsPerMillion:N1}ppm, old: {result.Old?.Co2?.PartsPerMillion:N1}ppm.");
                    Console.WriteLine($"   new VOC: {result.New.Voc?.PartsPerBillion:N1}ppb, old: {result.Old?.Voc?.PartsPerBillion:N1}ppb.");
                },
                // only notify if the change is greater than 1000ppm CO2 and 100ppb VOC.
                // breathing on the sensor should trigger
                filter: result => {
                    if (result.Old is { } old) { //c# 8 pattern match syntax. checks for !null and assigns var.
                        return (
                        (result.New.Co2.Value - old.Co2.Value).Abs().PartsPerMillion > 1000 // 1000ppm
                          &&
                        (result.New.Voc.Value - old.Voc.Value).Abs().PartsPerBillion > 100 // 100ppb
                        );
                    }
                    return false;
                }
                // if you want to always get notified, pass null for the filter:
                //filter: null
                );
            sensor.Subscribe(consumer);

            //==== Events
            // classical .NET events can also be used:
            sensor.Updated += (object sender, IChangeResult<(Concentration? Co2, Concentration? Voc)> e) => {
                Console.WriteLine($"CO2: {e.New.Co2.Value.PartsPerMillion:n1}ppm, VOC: {e.New.Voc.Value.PartsPerBillion:n1}ppb");
            };

            //==== one-off read
            ReadConditions().Wait();

            // start updating continuously
            sensor.StartUpdating();
        }

        protected async Task ReadConditions()
        {
            var result = await sensor.Read();
            Console.WriteLine("Initial Readings:");
            Console.WriteLine($"  CO2: {result.Co2.Value.PartsPerMillion:n1}ppm");
            Console.WriteLine($"  VOC: {result.Voc.Value.PartsPerBillion:n1}ppb");
        }
    }
}