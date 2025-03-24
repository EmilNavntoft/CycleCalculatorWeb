using CycleCalculator.CycleModel.Model.Interfaces;
using CycleCalculator.CycleModel.Model.IO;
using EngineeringUnits;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CycleCalculator.CycleModel.Exceptions;
using Microsoft.JSInterop;

namespace CycleCalculator.CycleModel.Model
{
	public class HeatFlowBoundary : CycleComponent, IHeatFlowExchanger
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

		public HeatFlowBoundary(string name, IJSInProcessObjectReference coolprop) : base(name, coolprop)
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

			TransferState();

			downstreamPort.Connection.Component.CalculateMassBalanceEquation(downstreamPort.Connection);
		}

		public override void CalculateHeatBalanceEquation(Port _)
		{
			Port upstreamPort = GetUpstreamPort();
			Port downstreamPort = GetDownstreamPort();
			downstreamPort.Enthalpy = upstreamPort.Enthalpy + (HeatFlow / upstreamPort.MassFlow);

			Fluid.UpdatePH(upstreamPort.Pressure, downstreamPort.Enthalpy);
			downstreamPort.Temperature = Fluid.Temperature;
			HeatFlowExchanged = upstreamPort.MassFlow * (downstreamPort.Enthalpy - upstreamPort.Enthalpy);

			TransferState();
			downstreamPort.Connection.Component.CalculateHeatBalanceEquation(downstreamPort.Connection);
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
		}
	}
}
