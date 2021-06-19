﻿using System;
using System.Collections.Generic;
using System.Threading;
using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Weather;
using Meadow.Units;
using SU = Meadow.Units.Speed.UnitType;

namespace Meadow.Foundation.Sensors.Weather
{
    // TODO: there still seems to be some issue with initial reading
    // every once in a while, i'll get 0.0kmh, then i'll give it a spin and
    // it'll give me something insane like:
    // new speed: 65.9kmh, old: 0.0kmh
    // new speed(from observer) : 65.9kmh, old: 0.0kmh
    // then the next reading will be 3kmh.

    /// <summary>
    /// Driver for a "switching" anememoter (wind speed gauge) that has an
    /// internal switch that is triggered during every revolution.
    /// </summary>
    public partial class SwitchingAnemometer :
        ObservableBase<Speed>, IAnemometer
    {
        //==== events
        /// <summary>
        /// Raised when the speed of the wind changes.
        /// </summary>
        public event EventHandler<IChangeResult<Speed>> WindSpeedUpdated = delegate { };

        //==== internals
        IDigitalInputPort inputPort;
        bool running = false;
        int standbyDuration;
        int overSampleCount;
        System.Timers.Timer? noWindTimer;
        List<DigitalPortResult>? samples;

        // Turn on for debug output
        bool debug = false;

        //==== properties
        /// <summary>
        /// The last recored wind speed.
        /// </summary>
        public Speed? WindSpeed { get; protected set; }

        /// <summary>
        /// Calibration for how fast the wind speed is when the switch is hit
        /// once per second. Used to calculate the wind speed based on the time
        /// duration between switch events. Default is `2.4kmh`.
        /// </summary>
        public float KmhPerSwitchPerSecond { get; set; } = 2.4f;

        /// <summary>
        /// Creates a new `SwitchingAnemometer` using the specific digital input
        /// on the device.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="digitalInputPin"></param>
        public SwitchingAnemometer(IDigitalInputController device, IPin digitalInputPin)
            : this(device.CreateDigitalInputPort(
                digitalInputPin, InterruptMode.EdgeFalling,
                ResistorMode.InternalPullUp, 20, 20))
        {
        }

        /// <summary>
        /// Creates a new switching anemometer using the specific `IDigitalInputPort`.
        /// </summary>
        /// <param name="inputPort"></param>
        public SwitchingAnemometer(IDigitalInputPort inputPort)
        {
            this.inputPort = inputPort;
            this.Init();
        }

        protected void Init()
        {
        }

        protected void SubscribeToinputPortEvents()
        {
            inputPort.Changed += HandleInputPortChange;
        }

        protected void UnsubscribeToInputPortEvents()
        {
            inputPort.Changed -= HandleInputPortChange;
        }

        protected void HandleInputPortChange(object sender, DigitalPortResult result)
        {
            if (!running) { return; }

            if(samples == null) { samples = new List<DigitalPortResult>(); }

            // reset our nowind timer, since a sample has come in. note that the API is awkward
            noWindTimer?.Stop();
            noWindTimer?.Start();

            // we need at least two readings to get a valid speed, since the
            // speed is based on duration between clicks, we need at least two
            // clicks to measure duration.
            samples.Add(result);
            if (debug) { Console.WriteLine($"result #[{samples.Count}] new: [{result.New}], old: [{result.Old}]"); }

            // if we don't have two samples, move on
            if (samples.Count < 1) { return; }

            // if we've reached our sample count
            if (samples.Count >= overSampleCount) {
                float speedSum = 0f;
                float oversampledSpeed = 0f;

                // sum up the speeds
                foreach (var sample in samples) {
                    // skip the first (old will be null)
                    if (sample.Old is { } old) {
                        speedSum += SwitchIntervalToKmh(sample.New.Time - old.Time);
                    }
                }
                // average the speeds
                oversampledSpeed = speedSum / samples.Count - 1;

                // clear our samples
                this.samples.Clear();

                // capture history
                Speed? oldSpeed = WindSpeed;
                // save state
                Speed newSpeed = new Speed(oversampledSpeed, SU.KilometersPerHour);
                WindSpeed = newSpeed;
                RaiseUpdated(new ChangeResult<Speed>(newSpeed, oldSpeed));

                // if we need to wait before taking another sample set, 
                if (this.standbyDuration > 0) {
                    this.UnsubscribeToInputPortEvents();
                    Thread.Sleep(standbyDuration);
                    this.SubscribeToinputPortEvents();
                }
            }
        }

        protected void RaiseUpdated(IChangeResult<Speed> changeResult)
        {
            WindSpeedUpdated?.Invoke(this, changeResult);
            base.NotifyObservers(changeResult);
        }

        /// <summary>
        /// A wind speed of 2.4km/h causes the switch to close once per second.
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        protected float SwitchIntervalToKmh(TimeSpan interval)
        {
            // A wind speed of 2.4km/h causes the switch to close once per second.
            return this.KmhPerSwitchPerSecond / ((float)interval.TotalMilliseconds / 1000f);
        }

        /// <summary>
        /// Starts continuously sampling the sensor.
        ///
        /// This method also starts raising `Updated` events and IObservable
        /// subscribers getting notified. Use the `standbyDuration` parameter
        /// to specify how often events and notifications are raised/sent.
        /// </summary>
        /// <param name="sampleCount">How many samples to take during a given
        /// reading. These are automatically averaged to reduce noise.</param>
        /// <param name="standbyDuration">The time, in milliseconds, to wait
        /// between listening for events. This value determines how often
        /// `Changed` events are raised and `IObservable` consumers are notified.</param>
        /// <param name="noWindTimeout">The time, in milliseconds in which to
        /// wait for events from the anemometer. If no events come in by the time
        /// this elapses, then an event of `0` wind will be raised.</param>
        public void StartUpdating(
            int sampleCount = 5,
            int standbyDuration = 500,
            int noWindTimeout = 4000)
        {
            if(standbyDuration < 0) { throw new ArgumentException("`StandbyDuration` must be greater than or equal to `0`."); }
            if(sampleCount < 2) { sampleCount = 2; }
            if(running) { return; }

            this.overSampleCount = sampleCount;
            this.standbyDuration = standbyDuration;

            running = true;

            // start a timer that we can use to raise a zero wind event in the case
            // that we're not getting input events (because there is no wind)
            noWindTimer = new System.Timers.Timer(noWindTimeout);
            noWindTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => {
                if (debug) { Console.WriteLine("No wind timer elapsed."); }
                // if not running, clear the timer and bail out.
                if (!running) {
                    noWindTimer.Stop();
                    return;
                }
                // if there aren't enough samples to make a reading
                if (samples == null || samples.Count <= overSampleCount ) {
                    // capture the old value
                    Speed? oldSpeed = WindSpeed;
                    // save state
                    Speed newSpeed = new Speed(0, SU.KilometersPerHour);
                    WindSpeed = newSpeed;

                    // raise the wind updated event with `0` wind speed
                    RaiseUpdated(new ChangeResult<Speed>(newSpeed, oldSpeed));
                    // sleep for the standby duration
                    if (standbyDuration > 0) {
                        if (debug) { Console.WriteLine("Sleeping for a bit."); }
                        Thread.Sleep(standbyDuration);
                        if (debug) { Console.WriteLine("Woke up."); }
                    }

                    if (debug) { Console.WriteLine($"timer enabled? {noWindTimer.Enabled}"); }

                    // if still running, start the timer again
                    if (running) {
                        if (debug) { Console.WriteLine("restarting timer."); }
                        noWindTimer.Start();
                    }
                }
            };
            noWindTimer.Start();

            SubscribeToinputPortEvents();
        }

        /// <summary>
        /// Stops the driver from raising wind speed events.
        /// </summary>
        public void StopUpdating()
        {
            if(running) {
                UnsubscribeToInputPortEvents();
            }
            if (noWindTimer != null && noWindTimer.Enabled) {
                noWindTimer.Stop();
            }

            // state machine
            running = false;
        }

    }
}
