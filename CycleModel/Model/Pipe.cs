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
        public Pipe(string name, double pressureDropCoefficient, IJSInProcessObjectReference coolProp) : base(name, coolProp)
        {
            PortA = new Port(A, this);
            PortB = new Port(B, this);
            Ports.Add(A, PortA);
            Ports.Add(B, PortB);
            PressureDropCoefficient = pressureDropCoefficient;
        }

        public override void CalculateMassBalanceEquation(Port _)
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            downstreamPort.MassFlow = MassFlow.Zero - upstreamPort.MassFlow;

            TransferThermalState();
        }

        public override void CalculateHeatBalanceEquation(Port _)
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();
            Fluid1.UpdatePH(downstreamPort.Pressure, upstreamPort.Enthalpy);
            downstreamPort.Temperature = Fluid1.Temperature;
            downstreamPort.Enthalpy = Fluid1.Enthalpy;
            _density = Fluid1.Density;

            TransferThermalState();
            downstreamPort.Connection.Component.CalculateHeatBalanceEquation(downstreamPort.Connection);
        }

        public override void ReceiveAndCascadePressure(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            port.Pressure = port.Connection.Pressure;
        }

        public void CascadePressureDownstream()
        {
            Port downstreamPort = GetDownstreamPort();
            downstreamPort.Connection.Component.ReceiveAndCascadePressure(downstreamPort.Connection);
        }
    }
}
