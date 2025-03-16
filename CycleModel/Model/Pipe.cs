using CycleCalculator.CycleModel.Exceptions;
using CycleCalculator.CycleModel.Model.IO;
using EngineeringUnits;
using Microsoft.JSInterop;
using SharpFluids;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;

namespace CycleCalculator.CycleModel.Model
{
    public class Pipe : CycleComponent
    {
        /// <summary>
        /// Unit: [bar/(m^3/s)^2]
        /// </summary>
        public double PressureDropCoefficient { get; set; }

        private Density _density = Density.FromKilogramPerCubicMeter(10);
        public Pipe(string name, double pressureDropCoefficient, IJSInProcessObjectReference coolprop) : base(name, coolprop)
        {
            PortA = new Port(A, this);
            PortB = new Port(B, this);
            Ports.Add(A, PortA);
            Ports.Add(B, PortB);
            PressureDropCoefficient = pressureDropCoefficient;
        }

        public override void CalculateMassBalanceEquation()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            downstreamPort.MassFlow = MassFlow.Zero - upstreamPort.MassFlow;

            TransferState();
        }

        public override void CalculateHeatBalanceEquation()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();
            Fluid.UpdatePH(downstreamPort.Pressure, upstreamPort.Enthalpy);
            downstreamPort.Temperature = Fluid.Temperature;
            downstreamPort.Enthalpy = Fluid.Enthalpy;
            _density = Fluid.Density;

            TransferState();
            downstreamPort.Connection.Component.CalculateHeatBalanceEquation();
        }

        public override void CalculatePressureDrop()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            VolumeFlow volumeFlow = VolumeFlow.Zero;
            if (upstreamPort.Enthalpy != Enthalpy.NaN)
            {
                Fluid.UpdatePH(upstreamPort.Pressure, upstreamPort.Enthalpy);
                volumeFlow = upstreamPort.MassFlow / Fluid.Density;
            } 
            else if (upstreamPort.Temperature != Temperature.NaN)
            {
                Fluid.UpdatePT(upstreamPort.Pressure, upstreamPort.Temperature);
                volumeFlow = upstreamPort.MassFlow / Fluid.Density;
            } 
            else
            {
                return;
                //volumeFlow = upstreamPort.MassFlow / Density.FromKilogramPerCubicMeter(10);
            }


            downstreamPort.Pressure = upstreamPort.Pressure - Pressure.FromBar(PressureDropCoefficient * Math.Pow(volumeFlow.CubicMeterPerSecond, 2));

            TransferState();
            downstreamPort.Connection.Component.CalculatePressureDrop();
        }

        public override void ReceiveAndCascadePressure(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            port.Pressure = port.Connection.Pressure;
        }

        public override void ReceiveAndCascadeTemperatureAndEnthalpy(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            port.Temperature = port.Connection.Temperature;
            port.Enthalpy = port.Connection.Enthalpy;

            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            otherPort.Temperature = port.Connection.Temperature;
            otherPort.Enthalpy = port.Connection.Enthalpy;

            otherPort.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(otherPort.Connection);
        }

        public override bool IsMassBalanceEquationIndeterminate()
        {
            return Ports.Values.All(port => port.MassFlow == MassFlow.NaN);
        }

        public void CascadePressureDownstream()
        {
            Port downstreamPort = GetDownstreamPort();
            downstreamPort.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(downstreamPort.Connection);
        }
    }
}
