using CycleCalculator.CycleModel.Model.IO;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using CycleCalculator.CycleModel.Model.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using CycleCalculator.CycleModel.Exceptions;
using Microsoft.JSInterop;

namespace CycleCalculator.CycleModel.Model
{
    public class TemperatureBoundary : CycleComponent, ITemperatureOrEnthalpySetter, IHeatFlowExchanger
	{
        public enum TemperatureBoundaryMode
		{
            OutletTemperature,
            Superheating,
            Subcooling
        }

        public TemperatureBoundaryMode Mode { get; set; } = TemperatureBoundaryMode.OutletTemperature;

		[Editable(false)]
		public Power HeatFlowExchanged { get; set; } = Power.NaN;

		[Editable(false)]
        public Temperature Temperature { get; set; } = Temperature.FromDegreeCelsius(10);

		[DisplayName("Temperature [°C]")]
		public double TemperatureDouble
		{
			get
			{
				return Temperature.DegreeCelsius;
			}
			set
			{
				Temperature = Temperature.FromDegreeCelsius(value);
			}
		}
		public TemperatureBoundary(string name, IJSInProcessObjectReference coolprop) : base(name, coolprop)
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

            TransferState();
        }

        public override void CalculateHeatBalanceEquation()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            

			if (Mode == TemperatureBoundaryMode.OutletTemperature)
			{
				downstreamPort.Temperature = Temperature;
			}
			else if (Mode == TemperatureBoundaryMode.Superheating)
			{
				downstreamPort.Temperature = Fluid.GetSatTemperature(upstreamPort.Pressure) + Temperature.FromKelvin(Temperature.DegreeCelsius);
			}
			else if (Mode == TemperatureBoundaryMode.Subcooling)
			{
				downstreamPort.Temperature = Fluid.GetSatTemperature(upstreamPort.Pressure) - Temperature.FromKelvin(Temperature.DegreeCelsius);
			}

			Fluid.UpdatePT(upstreamPort.Pressure, downstreamPort.Temperature);
            downstreamPort.Enthalpy = Fluid.Enthalpy;

            HeatFlowExchanged = upstreamPort.MassFlow * (downstreamPort.Enthalpy - upstreamPort.Enthalpy);
		}

        public override bool IsMassBalanceEquationIndeterminate()
        {
            return false;
        }

        public override void ReceiveAndCascadeTemperatureAndEnthalpy(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }
            port.Temperature = port.Connection.Temperature;
            port.Pressure = port.Connection.Pressure;
            port.Enthalpy = port.Connection.Enthalpy;

            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            otherPort.Pressure = port.Connection.Pressure;
            if (Mode == TemperatureBoundaryMode.OutletTemperature)
            {
				otherPort.Temperature = Temperature;
			} 
            else if (Mode == TemperatureBoundaryMode.Superheating)
            {
                otherPort.Temperature = Fluid.GetSatTemperature(port.Pressure) + Temperature.FromKelvin(Temperature.DegreeCelsius);
			} 
            else if (Mode == TemperatureBoundaryMode.Subcooling)
            {
				otherPort.Temperature = Fluid.GetSatTemperature(port.Pressure) - Temperature.FromKelvin(Temperature.DegreeCelsius);
			}
            

            if (port.Connection.Pressure != Pressure.NaN)
            {
                Fluid.UpdatePT(port.Connection.Pressure, Temperature);
                otherPort.Enthalpy = Fluid.Enthalpy;
            }
        }

        public void CascadeTemperatureAndEnthalpyDownstream()
        {
            CalculateHeatBalanceEquation();
            Port downstreamPort = GetDownstreamPort();
            downstreamPort.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(downstreamPort.Connection);
        }

        public void StartHeatBalanceCalculation()
        {
            CalculateHeatBalanceEquation();
            TransferState();
            GetDownstreamPort().Connection.Component.CalculateHeatBalanceEquation();
        }
    }
}
