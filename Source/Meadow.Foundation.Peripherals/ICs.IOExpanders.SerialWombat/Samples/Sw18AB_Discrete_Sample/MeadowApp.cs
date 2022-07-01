﻿using Meadow;
using Meadow.Devices;
using Meadow.Foundation.ICs.IOExpanders;
using Meadow.Hardware;
using System;
using System.Threading.Tasks;

namespace ICs.IOExpanders.Sw18AB_Samples
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        private Sw18AB _wombat;
        private IDigitalOutputPort _output;

        public MeadowApp()
        {
        }

        public override Task Initialize()
        {
            Console.WriteLine("Initialize...");

            try
            {
                _wombat = new Sw18AB(Device.CreateI2cBus());
                _output = _wombat.CreateDigitalOutputPort(_wombat.Pins.WP0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public override async Task Run()
        {
            Resolver.Log.Info("Running...");

            bool state = false;

            while (true)
            {
                Console.WriteLine($"SW0 = {(state ? "high" : "low")}");
                _output.State = state;
                state = !state;

                await Task.Delay(1000);
            }
        }

        //<!=SNOP=>
    }
}