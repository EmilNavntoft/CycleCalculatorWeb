using CycleCalculator.CycleModel.Model;
using CycleCalculator.CycleModel.Model.IO;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using Microsoft.JSInterop;
using CycleCalculator.CycleModel.Exceptions;
using EngineeringUnits;
using EngineeringUnits.Units;

namespace CycleCalculatorWeb.CycleModel.Model
{
	public class PlateHeatExchanger : CycleComponent
	{
		Port PortC { get; set; }
		Port PortD { get; set; }
		Area Area { get; set; } = Area.FromSquareMeter(1);
		HeatTransferCoefficient HeatTransferCoefficientSideAB { get; set; } = HeatTransferCoefficient.FromWattPerSquareMeterKelvin(500);
		HeatTransferCoefficient HeatTransferCoefficientSideCD { get; set; } = HeatTransferCoefficient.FromWattPerSquareMeterKelvin(500);
		ThermalConductivity PlateThermalConductivity { get; set; } = ThermalConductivity.FromWattPerMeterKelvin(15);
		Length PlateThickness { get; set; } = Length.FromMillimeter(1);
		Dictionary<PortIdentifier, PortIdentifier> _InternalConnections = new() {
			{A, B },
			{B, A },
			{C, D },
			{D, C }
		};
		private bool _IsStable = false;
		public PlateHeatExchanger(string name, IJSInProcessObjectReference coolprop) : base(name, coolprop)
		{
			PortA = new Port(A, this);
			PortB = new Port(B, this);
			PortC = new Port(C, this);
			PortD = new Port(D, this);
			Ports.Add(A, PortA);
			Ports.Add(B, PortB);
			Ports.Add(C, PortC);
			Ports.Add(D, PortD);
		}

		public override void ReceiveAndCascadePressure(Port port)
		{
			if (!Ports.ContainsValue(port))
			{
				throw new SolverException($"Cascaded port does not belong to {Name}");
			}

			port.Pressure = port.Connection.Pressure;

			PortIdentifier otherIdentifier = _InternalConnections[port.Identifier];
			var otherPort = Ports[otherIdentifier];
			otherPort.Pressure = port.Connection.Pressure;

			otherPort.Connection.Component.ReceiveAndCascadePressure(otherPort.Connection);
		}

		public override void ReceiveAndCascadeMassFlow(Port port)
		{
			if (!Ports.ContainsValue(port))
			{
				throw new SolverException($"Cascaded port does not belong to {Name}");
			}
			if (port.MassFlow == MassFlow.Zero - port.Connection.MassFlow) return;

			var negatedMassFlow = MassFlow.Zero - port.Connection.MassFlow; //Negate incoming massFlow in order to match reference frame of this component
			port.MassFlow = negatedMassFlow;

			PortIdentifier otherIdentifier = _InternalConnections[port.Identifier];

			var otherPort = Ports[otherIdentifier];
			otherPort.MassFlow = port.Connection.MassFlow;

			otherPort.Connection.Component.ReceiveAndCascadeMassFlow(otherPort.Connection);
		}

		public override void ReceiveAndCascadeTemperatureAndEnthalpy(Port port)
		{
			if (!Ports.ContainsValue(port))
			{
				throw new SolverException($"Cascaded port does not belong to {Name}");
			}

			port.Temperature = port.Connection.Temperature;
			port.Enthalpy = port.Connection.Enthalpy;

			PortIdentifier otherIdentifier = _InternalConnections[port.Identifier];

			var otherPort = Ports[otherIdentifier];
			otherPort.MassFlow = port.Connection.MassFlow;
			otherPort.Temperature = port.Connection.Temperature;
			otherPort.Enthalpy = port.Connection.Enthalpy;

			otherPort.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(otherPort.Connection);
		}

		public override void CalculateMassBalanceEquation(Port port)
		{
			PortIdentifier otherIdentifier = _InternalConnections[port.Identifier];
			var otherPort = Ports[otherIdentifier];

			otherPort.MassFlow = MassFlow.Zero - port.MassFlow;
			otherPort.Pressure = port.Pressure;

			TransferState(otherPort);

			otherPort.Connection.Component.CalculateMassBalanceEquation(otherPort.Connection);
		}

		public override void CalculateHeatBalanceEquation(Port port)
		{
			double dT1 = Math.Abs(PortA.Temperature.Kelvin - PortC.Temperature.Kelvin);
			double dT2 = Math.Abs(PortB.Temperature.Kelvin - PortD.Temperature.Kelvin);

			Temperature LMTD;
			if (dT1 == dT2)
			{
				LMTD = Temperature.FromKelvin(dT1);
			}
			else if (dT1 == 0 || dT2 == 0) 
			{
				LMTD = Temperature.FromKelvin(0);
			}
			else
			{
				double ln = Math.Log(dT1 / dT2);
				LMTD = Temperature.FromKelvin((dT1 - dT2) / ln);
			}

			HeatTransferCoefficient U = HeatTransferCoefficientSideAB + PlateThermalConductivity / PlateThickness + HeatTransferCoefficientSideCD;

			Power Q = U * Area * LMTD;

			List<Port> hotSidePorts = new List<Port>();
			if (PortA.Temperature > PortC.Temperature)
			{
				hotSidePorts.Add(PortA);
				hotSidePorts.Add(PortB);
			}
            else

            {
				hotSidePorts.Add(PortC);
				hotSidePorts.Add(PortD);
            }

            List<Port> outletPorts = Ports.Values.ToList().FindAll(port => port.MassFlow < MassFlow.Zero);

            foreach (var outletPort in outletPorts)
            {
				PortIdentifier internalConnectionIdentifier = _InternalConnections[outletPort.Identifier];
				Port internalConnection = Ports[internalConnectionIdentifier];

				if (hotSidePorts.Contains(outletPort))
				{
					outletPort.Enthalpy = internalConnection.Enthalpy - Q / internalConnection.MassFlow;
				}
				else
				{
					outletPort.Enthalpy = internalConnection.Enthalpy + Q / internalConnection.MassFlow;
				}
				Fluid.UpdatePH(outletPort.Pressure, outletPort.Enthalpy);
				outletPort.Temperature = Fluid.Temperature;
            }

			PortIdentifier otherIdentifier = _InternalConnections[port.Identifier];
			var otherPort = Ports[otherIdentifier];
			otherPort.Connection.Component.CalculateHeatBalanceEquation(otherPort.Connection);
        }

		public override void CalculatePressureDrop(Port port)
		{
			PortIdentifier otherIdentifier = _InternalConnections[port.Identifier];
			var otherPort = Ports[otherIdentifier];
			otherPort.Pressure = port.Pressure;

			TransferState(otherPort);
			otherPort.Connection.Component.CalculatePressureDrop(otherPort.Connection);
		}

		public void TransferState(Port port)
		{
			if (port.MassFlow is null)
			{
				throw new SolverException($"Port {port.Identifier} of {port.Component} has null massflow");
			}

			if (port.Connection is null)
			{
				throw new SolverException($"Port {port.Identifier} of {port.Component} has null connection");
			}

			port.Connection.ReceiveState();
		}

		public override void TransferState()
		{
			throw new NotImplementedException();
		}
	}
}
