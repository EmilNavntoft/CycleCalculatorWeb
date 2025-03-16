using CycleCalculator.CycleModel.Model.IO;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CycleCalculator.CycleModel.Exceptions;
using Microsoft.JSInterop;

namespace CycleCalculator.CycleModel.Model
{
    public class PRV : CycleComponent
    {
        [Editable(false)]
        public Pressure OutletPressure { get; set; } = Pressure.FromBar(1);

		[DisplayName("Outlet pressure [bar]")]
		public double OutletPressureDouble
		{
			get
			{
				return OutletPressure.Bar;
			}
			set
			{
				OutletPressure = Pressure.FromBar(value);
			}
		}

		public PRV(string name, IJSInProcessObjectReference coolprop) : base(name, coolprop)
        {
            PortA = new Port(A, this);
            PortB = new Port(B, this);
            Ports.Add(A, PortA);
            Ports.Add(B, PortB);
        }

        public override void CalculateMassBalanceEquation()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            downstreamPort.MassFlow = MassFlow.Zero - upstreamPort.MassFlow;

            if (downstreamPort.Pressure != Pressure.NaN && downstreamPort.Pressure != OutletPressure) 
            {
                throw new SolverException($"Outlet pressure of {Name} set by downstream component to a value that does not match OutletPressure. " +
                    $"Can also be a sign of reverse flow through the PRV.");
            }
            
            downstreamPort.Pressure = OutletPressure;

            TransferState();
        }

        public override void CalculateHeatBalanceEquation()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();
            Fluid.UpdatePH(downstreamPort.Pressure, upstreamPort.Enthalpy);
            downstreamPort.Temperature = Fluid.Temperature;
            downstreamPort.Enthalpy = upstreamPort.Enthalpy;

            TransferState();
            downstreamPort.Connection.Component.CalculateHeatBalanceEquation();
        }

        public override void CalculatePressureDrop()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            if (upstreamPort.Pressure < OutletPressure)
            {
                throw new SolverException($"Upstream pressure of PRV {Name} is lower than the specified outlet pressure");
            }

            downstreamPort.Pressure = OutletPressure;

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
            otherPort.Enthalpy = port.Connection.Enthalpy;
            if (port.Connection.Enthalpy != Enthalpy.NaN)
            {
                Fluid.UpdatePH(otherPort.Pressure, otherPort.Enthalpy);
                otherPort.Temperature = Fluid.Temperature;
            }

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
