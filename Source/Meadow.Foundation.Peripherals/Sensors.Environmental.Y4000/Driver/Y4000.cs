﻿using Meadow.Hardware;
using Meadow.Modbus;
using Meadow.Units;
using System.Threading.Tasks;
using System;

namespace Meadow.Foundation.Sensors.Environmental
{
    /// <summary>
    /// Represents a Yosemitech Y4000 Multiparameter Sonde water quality sensor 
    /// for dissolved oxygen, conductivity, turbidity, pH, chlorophyll, 
    /// blue green algae, oil-in-water and temperature
    /// </summary>
    public partial class Y4000 : PollingSensorBase<(ConcentrationInWater? DisolvedOxygen,
                                                    ConcentrationInWater? Chlorophyl, 
                                                    ConcentrationInWater? BlueGreenAlgae,
                                                    Conductivity? ElectricalConductivity,
                                                    PotentialHydrogen? PH,
                                                    Turbidity? Turbidity,
                                                    Units.Temperature? Temperature,
                                                    Voltage? OxidationReductionPotential)>
    {
        /// <summary>
        /// Raised when the DisolvedOxygen value changes
        /// </summary>
        public event EventHandler<IChangeResult<ConcentrationInWater>> DisolvedOxygenUpdated = delegate { };

        /// <summary>
        /// Raised when the Chlorophyl value changes
        /// </summary>
        public event EventHandler<IChangeResult<ConcentrationInWater>> ChlorophylUpdated = delegate { };

        /// <summary>
        /// Raised when the BlueGreenAlgae value changes
        /// </summary>
        public event EventHandler<IChangeResult<ConcentrationInWater>> BlueGreenAlgaeUpdated = delegate { };

        /// <summary>
        /// Raised when the ElectricalConductivity value changes
        /// </summary>
        public event EventHandler<IChangeResult<Conductivity>> ElectricalConductivityUpdated = delegate { };

        /// <summary>
        /// Raised when the PotentialHydrogen (pH) value changes
        /// </summary>
        public event EventHandler<IChangeResult<PotentialHydrogen>> PHUpdated = delegate { };

        /// <summary>
        /// Raised when the Turbidity value changes
        /// </summary>
        public event EventHandler<IChangeResult<Turbidity>> TurbidityUpdated = delegate { };

        /// <summary>
        /// Raised when the Temperature value changes
        /// </summary>
        public event EventHandler<IChangeResult<Units.Temperature>> TemperatureUpdated = delegate { };

        /// <summary>
        /// Raised when the OxidationReductionPotential (redux) value changes
        /// </summary>
        public event EventHandler<IChangeResult<Voltage>> OxidationReductionPotentialUpdated = delegate { };

        /// <summary>
        /// The current Disolved Oxygen concentration
        /// </summary>
        public ConcentrationInWater? DisolvedOxygen => Conditions.DisolvedOxygen;

        /// <summary>
        /// The current Chlorophyl concentration
        /// </summary>
        public ConcentrationInWater? Chlorophyl => Conditions.Chlorophyl;

        /// <summary>
        /// The current Blue Green Algae concentration
        /// </summary>
        public ConcentrationInWater? BlueGreenAlgae => Conditions.BlueGreenAlgae;

        /// <summary>
        /// The current Electrical Conductivity
        /// </summary>
        public Conductivity? ElectricalConductivity => Conditions.ElectricalConductivity;

        /// <summary>
        /// The current Potential Hydrogen (pH)
        /// </summary>
        public PotentialHydrogen? PH => Conditions.PH;

        /// <summary>
        /// The current Turbidity
        /// </summary>
        public Turbidity? Turbidity => Conditions.Turbidity;

        /// <summary>
        /// The current Oxidation Reduction Potential (redux)
        /// </summary>
        public Voltage? OxidationReductionPotential => Conditions.OxidationReductionPotential;



        readonly IModbusBusClient modbusClient;

        /// <summary>
        /// 9600 baud 8-N-1
        /// </summary>
        readonly ISerialPort serialPort;

        readonly byte ModbusAddress = 0x01;

        /// <summary>
        /// Creates a new Y4000 object
        /// </summary>
        public Y4000(IMeadowDevice device, SerialPortName serialPortName, IPin? enablePin = null)
        {   // 9600 baud 8-N-1
            serialPort = device.CreateSerialPort(serialPortName, 9600, 8, Parity.None, StopBits.One);
            serialPort.WriteTimeout = serialPort.ReadTimeout = TimeSpan.FromSeconds(5);

            if (enablePin != null)
            {
                var enablePort = device.CreateDigitalOutputPort(enablePin, false);
                modbusClient = new ModbusRtuClient(serialPort, enablePort);
            }
            else
            {
                modbusClient = new ModbusRtuClient(serialPort);
            }
        }

        /// <summary>
        /// Initialize sensor
        /// </summary>
        /// <returns></returns>
        public Task Initialize()
        {
            Console.WriteLine("Initialize");
            return modbusClient.Connect();
        }

        public async Task<ushort[]> GetISDN()
        {
            Console.WriteLine("GetISDN");

            var data = await modbusClient.ReadHoldingRegisters(0xFF, Registers.ISDN.Offset, Registers.ISDN.Length);

            return data;
        }

        public async Task<ushort[]> GetSerialNumber()
        {
            Console.WriteLine("GetSerialNumber");

            var data = await modbusClient.ReadHoldingRegisters(ModbusAddress, Registers.SerialNumber.Offset, Registers.SerialNumber.Length);

            return data;
        }

        public async Task<ushort[]> GetVersion()
        {
            Console.WriteLine("GetVersion");

            var data = await modbusClient.ReadHoldingRegisters(ModbusAddress, Registers.Version.Offset, Registers.Version.Length);
            return data;
        }

        public async Task<TimeSpan> GetBrushInterval()
        {
            var value = await modbusClient.ReadHoldingRegisters(ModbusAddress, Registers.BrushInterval.Offset, Registers.BrushInterval.Length);
            return TimeSpan.FromMinutes(value[0]);
        }

        /// <summary>
        /// Reads data from the sensor
        /// </summary>
        /// <returns>The latest sensor reading</returns>
        protected override async Task<(ConcentrationInWater? DisolvedOxygen, 
            ConcentrationInWater? Chlorophyl, 
            ConcentrationInWater? BlueGreenAlgae, 
            Conductivity? ElectricalConductivity, 
            PotentialHydrogen? PH, 
            Turbidity? Turbidity, 
            Units.Temperature? Temperature, 
            Voltage? OxidationReductionPotential)> 
            ReadSensor()
        {
            (ConcentrationInWater? DisolvedOxygen,
            ConcentrationInWater? Chlorophyl,
            ConcentrationInWater? BlueGreenAlgae,
            Conductivity? ElectricalConductivity,
            PotentialHydrogen? PH,
            Turbidity? Turbidity,
            Units.Temperature? Temperature,
            Voltage? OxidationReductionPotential) conditions;

            var values = await modbusClient.ReadHoldingRegistersFloat(ModbusAddress, Registers.Data.Offset, Registers.Data.Length / 2);
            var measurements = new Measurements(values);

            conditions.BlueGreenAlgae = measurements.BlueGreenAlgae;
            conditions.Chlorophyl = measurements.Chlorophyl;
            conditions.DisolvedOxygen = measurements.DissolvedOxygen;
            conditions.ElectricalConductivity = measurements.ElectricalConductivity;
            conditions.OxidationReductionPotential = measurements.OxidationReductionPotential;
            conditions.PH = measurements.PH;
            conditions.Temperature = measurements.Temperature;
            conditions.Turbidity = measurements.Turbidity;

            return conditions;
        }

        /// <summary>
        /// Raise events for subcribers and notify of value changes
        /// </summary>
        /// <param name="changeResult">The updated sensor data</param>
        protected override void RaiseEventsAndNotify(
             IChangeResult<
             (ConcentrationInWater? DisolvedOxygen,
             ConcentrationInWater? Chlorophyl,
             ConcentrationInWater? BlueGreenAlgae,
             Conductivity? ElectricalConductivity,
             PotentialHydrogen? PH,
             Turbidity? Turbidity,
             Units.Temperature? Temperature,
             Voltage? OxidationReductionPotential)
             > changeResult)
         {

                base.RaiseEventsAndNotify(changeResult);
         }
    }
}