﻿using Meadow.Hardware;
using System.Linq;

namespace Meadow.Foundation.ICs.IOExpanders
{
    /// <summary>
    /// Represent an Pcx8574 8 bit I2C port expander
    /// </summary>
    public abstract partial class Pcx8574 : Pcx857x
    {
        /// <summary>
        /// Pcx8574 pin definitions
        /// </summary>
        public PinDefinitions Pins { get; }

        /// <summary>
        /// The number of IO pins avaliable on the device
        /// </summary>
        public override int NumberOfPins => 8;

        /// <summary>
        /// Is the pin valid for this device instance
        /// </summary>
        /// <param name="pin">The IPin to validate</param>
        /// <returns>True if pin is valid</returns>
        protected override bool IsValidPin(IPin pin) => Pins.AllPins.Contains(pin);

        /// <summary>
        /// Creates an Pcx8574 object
        /// </summary>
        /// <param name="i2cBus">The I2C bus</param>
        /// <param name="address">The I2C address</param>
        /// <param name="interruptPin">The interrupt pin</param>
        protected Pcx8574(II2cBus i2cBus, byte address, IPin? interruptPin = null) :
            base(i2cBus, address, interruptPin)
        {
            Pins = new PinDefinitions(this)
            {
                Controller = this
            };
        }

        /// <summary>
        /// Creates an Pcx8574 object
        /// </summary>
        /// <param name="i2cBus">The I2C bus</param>
        /// <param name="address">The I2C address</param>
        /// <param name="interruptPort">The interrupt port</param>
        protected Pcx8574(II2cBus i2cBus, byte address, IDigitalInterruptPort? interruptPort = null) :
            base(i2cBus, address, interruptPort)
        {
            Pins = new PinDefinitions(this)
            {
                Controller = this
            };
        }

        /// <summary>
        /// Get pin reference by name
        /// </summary>
        /// <param name="pinName">The pin name as a string</param>
        /// <returns>IPin reference if found</returns>
        public override IPin GetPin(string pinName)
        {
            return Pins.AllPins.FirstOrDefault(p => p.Name == pinName || p.Key.ToString() == p.Name);
        }
    }
}