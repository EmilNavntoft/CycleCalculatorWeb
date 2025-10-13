using System.ComponentModel.DataAnnotations;
using CycleCalculator.CycleModel.Model;
using CycleCalculator.CycleModel.Model.IO;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using Microsoft.JSInterop;
using CycleCalculator.CycleModel.Exceptions;
using CycleCalculator.CycleModel.Model.Interfaces;
using EngineeringUnits;

namespace CycleCalculatorWeb.CycleModel.Model
{
	public class PlateHeatExchanger : CycleComponent, IHeatExchanger
	{
		private Port PortC { get; set; }
		private Port PortD { get; set; }
		[Editable(false)]
		public Area Area { get; set; } = Area.FromSquareMeter(1);
		public HeatTransferCoefficient HeatTransferCoefficientSideAb { get; set; } = HeatTransferCoefficient.FromWattPerSquareMeterKelvin(50);
		[Editable(false)]
		public HeatTransferCoefficient HeatTransferCoefficientSideCd { get; set; } = HeatTransferCoefficient.FromWattPerSquareMeterKelvin(50);
		[Editable(false)]
		public ThermalConductivity PlateThermalConductivity { get; set; } = ThermalConductivity.FromWattPerMeterKelvin(15);
		[Editable(false)]
		public Length PlateThickness { get; set; } = Length.FromMillimeter(1);

		private double Efficiency { get; set; } = 0.6;
		
		// Dictionary describing how the 4 ports are connected internally
		private readonly Dictionary<PortIdentifier, PortIdentifier> _internalConnections = new() {
			{A, B },
			{B, A },
			{C, D },
			{D, C }
		};
		private bool _isStable = false;
		public PlateHeatExchanger(string name, IJSInProcessObjectReference coolProp) : base(name, coolProp)
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

			PortIdentifier otherIdentifier = _internalConnections[port.Identifier];
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

			PortIdentifier otherIdentifier = _internalConnections[port.Identifier];

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

			PortIdentifier otherIdentifier = _internalConnections[port.Identifier];

			var otherPort = Ports[otherIdentifier];
			if (otherPort.Temperature.IsNaN())
			{
				// Set temperature and enthalpy in the other port in the first iteration where values are NaN
				otherPort.Temperature = port.Connection.Temperature;
				otherPort.Enthalpy = port.Connection.Enthalpy;
			}

			otherPort.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(otherPort.Connection);
		}

		public override void CalculateMassBalanceEquation(Port port)
		{
			PortIdentifier otherIdentifier = _internalConnections[port.Identifier];
			var otherPort = Ports[otherIdentifier];

			otherPort.MassFlow = MassFlow.Zero - port.MassFlow;
			otherPort.Pressure = port.Pressure;

			TransferState(otherPort);

			otherPort.Connection.Component.CalculateMassBalanceEquation(otherPort.Connection);
		}

		public void CalculateHeatExchanger()
		{
			double dT1 = Math.Abs(PortA.Temperature.Kelvin - PortC.Temperature.Kelvin);
			double dT2 = Math.Abs(PortB.Temperature.Kelvin - PortD.Temperature.Kelvin);
			
			List<Port> inletPorts = Ports.Values.ToList().FindAll(port => port.MassFlow > MassFlow.Zero);
			List<Port> outletPorts = Ports.Values.ToList().FindAll(port => port.MassFlow < MassFlow.Zero);
			
			var hotInletPort = inletPorts.OrderBy(port => port.Temperature).Last();
			var hotOutletPort =  Ports[_internalConnections[hotInletPort.Identifier]];
			var coldInletPort = inletPorts.OrderBy(port => port.Temperature).First();
			var coldOutletPart = Ports[_internalConnections[coldInletPort.Identifier]];
			
			Fluid.UpdatePT(hotInletPort.Pressure, coldInletPort.Temperature);
			var hHotOutMin = Fluid.Enthalpy;
			var qMaxHot = (hotInletPort.Enthalpy - hHotOutMin) * hotInletPort.MassFlow;
			
			Fluid.UpdatePT(coldInletPort.Pressure, hotInletPort.Temperature);
			var hColdOutMax = Fluid.Enthalpy;
			var qMaxCold = (hColdOutMax - coldInletPort.Enthalpy) * coldInletPort.MassFlow;

			var smallestQ = new[] { qMaxCold, qMaxCold }.Min();

			Power q = smallestQ * Efficiency;
				
			foreach (var outletPort in outletPorts)
			{
				PortIdentifier internalConnectionIdentifier = _internalConnections[outletPort.Identifier];
				Port internalConnection = Ports[internalConnectionIdentifier];

				if (outletPort == hotOutletPort)
				{
					outletPort.Enthalpy = internalConnection.Enthalpy - q / internalConnection.MassFlow;
				}
				else
				{
					outletPort.Enthalpy = internalConnection.Enthalpy + q / internalConnection.MassFlow;
				}		
				Fluid.UpdatePH(outletPort.Pressure, outletPort.Enthalpy);
				outletPort.Temperature = Fluid.Temperature;
			}
		}

		public override void CalculateHeatBalanceEquation(Port port)
		{
			PortIdentifier otherIdentifier = _internalConnections[port.Identifier];
			var otherPort = Ports[otherIdentifier];
			
			if (port.Enthalpy == otherPort.Enthalpy)
			{
				otherPort.Enthalpy = Ports.Values.ToList().First(x => x.Enthalpy != port.Enthalpy).Enthalpy * 0.99;
				var otherTemperature = otherPort.Temperature = Ports.Values.ToList().First(x => x.Enthalpy != port.Enthalpy).Temperature;
				if (otherTemperature > otherPort.Temperature)
				{
					otherPort.Temperature = otherTemperature - Temperature.FromKelvin(0.1);

				}
				else
				{
					otherPort.Temperature = otherTemperature + Temperature.FromKelvin(0.1);
				}
				
				Fluid.UpdatePT(port.Pressure, otherPort.Temperature);
				otherPort.Enthalpy = Fluid.Enthalpy;
				
				TransferState(otherPort);
				otherPort.Connection.Component.CalculateHeatBalanceEquation(otherPort.Connection);
			}
			else
			{
				// Simply transfer state without change
				TransferState(otherPort);
				otherPort.Connection.Component.CalculateHeatBalanceEquation(otherPort.Connection);
			}
		}

		public override void CalculatePressureDrop(Port port)
		{
			PortIdentifier otherIdentifier = _internalConnections[port.Identifier];
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
