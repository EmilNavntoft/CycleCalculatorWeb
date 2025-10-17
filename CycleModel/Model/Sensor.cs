using CycleCalculator.CycleModel.Model.IO;
using EngineeringUnits;
using Microsoft.JSInterop;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;

namespace CycleCalculator.CycleModel.Model
{
	public class Sensor : CycleComponent
	{
		public Sensor(string name, IJSInProcessObjectReference coolProp) : base(name, coolProp)
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

			upstreamPort.CopyThermalStateTo(downstreamPort);
			
			TransferThermalState();
			downstreamPort.Connection.Component.CalculateHeatBalanceEquation(downstreamPort.Connection);
		}
	}
}
