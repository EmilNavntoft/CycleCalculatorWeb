using CycleCalculator.CycleModel.Model.Interfaces;
using CycleCalculator.CycleModel.Model.IO;
using EngineeringUnits;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CycleCalculator.CycleModel.Exceptions;
using CycleCalculatorWeb.CoolpropJsInterop;
using Microsoft.JSInterop;

namespace CycleCalculator.CycleModel.Model
{
	public class HeatFlowBoundary : CycleComponent, IBoundary
	{
		[Editable(false)]
		public Power HeatFlowExchanged { get; set; } = Power.NaN;

		[Editable(false)]
		public Power HeatFlow { get; set; } = Power.FromKilowatt(0);

		[DisplayName("Heat Flow Rate [kW]")]
		public double HeatFlowDouble
		{
			get
			{
				return HeatFlow.Kilowatt;
			}
			set
			{
				HeatFlow = Power.FromKilowatt(value);
				HeatFlowExchanged = HeatFlow;
			}
		}

		public HeatFlowBoundary(string name, IJSInProcessObjectReference coolProp) : base(name, coolProp)
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

			TransferThermalState();

			downstreamPort.Connection.Component.CalculateMassBalanceEquation(downstreamPort.Connection);
		}

		public override void CalculateHeatBalanceEquation(Port _)
		{
			Port upstreamPort = GetUpstreamPort();
			Port downstreamPort = GetDownstreamPort();
			downstreamPort.Enthalpy = upstreamPort.Enthalpy + (HeatFlow / upstreamPort.MassFlow);

			double t = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', upstreamPort.Pressure.Pascal, 'H', downstreamPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);
			double x = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', upstreamPort.Pressure.Pascal, 'H', downstreamPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);
			downstreamPort.Temperature = Temperature.FromKelvins(t);
			downstreamPort.Quality = x;
			downstreamPort.Pressure = downstreamPort.Pressure;
			
			HeatFlowExchanged = upstreamPort.MassFlow * (downstreamPort.Enthalpy - upstreamPort.Enthalpy);

			TransferThermalState();
			downstreamPort.Connection.Component.CalculateHeatBalanceEquation(downstreamPort.Connection);
			GetDownstreamPort().Connection.Component.CalculateHeatBalanceEquation(GetDownstreamPort().Connection);
		}
	}
}
