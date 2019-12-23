﻿using Meadow;
using System.Threading;

namespace Sensors.Temperature.AnalogTemperature_Sample
{
    class Program
    {
        static IApp app;

        public static void Main(string[] args)
        {
            app = new MeadowApp();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
