using CycleCalculator.CycleModel.Model.IO;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using CycleCalculator.CycleModel.Model.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Diagnostics;
using CycleCalculator.CycleModel.Exceptions;
using CycleCalculatorWeb.CoolpropJsInterop;
using Microsoft.JSInterop;

namespace CycleCalculator.CycleModel.Model
{
    public class TemperatureBoundary : CycleComponent, ITemperatureOrEnthalpySetter, IBoundary
	{
        public enum TemperatureBoundaryMode
		{
            OutletTemperature,
            SuperHeating,
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
		public TemperatureBoundary(string name, IJSInProcessObjectReference coolProp) : base(name, coolProp)
        {
            PortA = new Port(A, this);
            PortB = new Port(B, this);
            Ports.Add(A, PortA);
            Ports.Add(B, PortB);
        }

        public override void CalculateMassBalanceEquation(Port _)
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            downstreamPort.MassFlow = MassFlow.Zero - upstreamPort.MassFlow;
            downstreamPort.Pressure = upstreamPort.Pressure;

			TransferThermalState();

			downstreamPort.Connection.Component.CalculateMassBalanceEquation(downstreamPort.Connection);
		}

        public override void CalculateHeatBalanceEquation(Port _)
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

			if (Mode == TemperatureBoundaryMode.OutletTemperature)
			{
				downstreamPort.Temperature = Temperature;
			}
			else if (Mode == TemperatureBoundaryMode.SuperHeating)
			{
				downstreamPort.Temperature = Fluid1.GetSatTemperature(upstreamPort.Pressure) + Temperature.FromKelvin(Temperature.DegreeCelsius);
			}
			else if (Mode == TemperatureBoundaryMode.Subcooling)
			{
				downstreamPort.Temperature = Fluid1.GetSatTemperature(upstreamPort.Pressure) - Temperature.FromKelvin(Temperature.DegreeCelsius);
			}
			
			double h = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', upstreamPort.Pressure.Pascal, 'T', downstreamPort.Temperature.Kelvin, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);
			double x = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', upstreamPort.Pressure.Pascal, 'T', downstreamPort.Temperature.Kelvin, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);

			downstreamPort.Enthalpy = Enthalpy.FromJoulePerKilogram(h);
            downstreamPort.Quality = x;
            downstreamPort.Pressure = upstreamPort.Pressure;

            TransferThermalState();
            HeatFlowExchanged = upstreamPort.MassFlow * (downstreamPort.Enthalpy - upstreamPort.Enthalpy);
            GetDownstreamPort().Connection.Component.CalculateHeatBalanceEquation(GetDownstreamPort().Connection);
		}

        public void StartHeatBalanceCalculation()
        {
            CalculateHeatBalanceEquation(null);
            TransferThermalState();
            GetDownstreamPort().Connection.Component.CalculateHeatBalanceEquation(GetDownstreamPort().Connection);
        }
    }
}
