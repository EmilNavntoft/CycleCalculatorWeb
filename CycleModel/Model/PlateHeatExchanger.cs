using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using CycleCalculator.CycleModel.Model;
using CycleCalculator.CycleModel.Model.IO;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using Microsoft.JSInterop;
using CycleCalculator.CycleModel.Exceptions;
using CycleCalculator.CycleModel.Model.Interfaces;
using CycleCalculatorWeb.CoolpropJsInterop;
using EngineeringUnits;

namespace CycleCalculatorWeb.CycleModel.Model
{
	public class PlateHeatExchanger : CycleComponent, IHeatExchanger
	{
		private Port PortC { get; set; }
		private Port PortD { get; set; }
		
		[Editable(false)]
		public CoolpropJsFluid Fluid2 { get; private set; }
		
		private FluidName _fluidType2 = FluidName.Ammonia;
		public FluidName FluidType2
		{
			get
			{
				return _fluidType2;
			}
			set
			{
				_fluidType2 = value;
				Fluid2.FluidName = _fluidType2;

				StartCascadeFluidTypeChange();
			}
		}
		
		public double Efficiency { get; set; } = 0.6;
		
		// Dictionary describing how the 4 ports are connected internally
		private readonly Dictionary<PortIdentifier, PortIdentifier> _internalConnections = new() {
			{A, B },
			{B, A },
			{C, D },
			{D, C }
		};
		
		private readonly Dictionary<PortIdentifier, PortIdentifier> _internalDiagonallyOppositePorts = new() {
			{A, D },
			{B, C },
			{C, B },
			{D, A }
		};

		public readonly Dictionary<PortIdentifier, CoolpropJsFluid> FluidPortConnections = new();
		
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
			Fluid2 = new CoolpropJsFluid(coolProp);
			FluidPortConnections = new()
			{
				{ A, Fluid1 },
				{ B, Fluid1 },
				{ C, Fluid2 },
				{ D, Fluid2 }
			};
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

		public override void CalculateMassBalanceEquation(Port port)
		{
			PortIdentifier otherIdentifier = _internalConnections[port.Identifier];
			var otherPort = Ports[otherIdentifier];

			otherPort.MassFlow = MassFlow.Zero - port.MassFlow;
			otherPort.Pressure = port.Pressure;

			TransferThermalState(otherPort);

			otherPort.Connection.Component.CalculateMassBalanceEquation(otherPort.Connection);
		}

		public void CalculateHeatExchanger()
		{
			Stopwatch sw = Stopwatch.StartNew();
			
			var portsList = Ports.Values;
			List<Port> inletPorts = new List<Port>(2);
			List<Port> outletPorts = new List<Port>(2);
    
			foreach (var port in portsList)
			{
				if (port.MassFlow > MassFlow.Zero)
					inletPorts.Add(port);
				else if (port.MassFlow < MassFlow.Zero)
					outletPorts.Add(port);
			}


			var hotInletPort = inletPorts.MaxBy(x => x.Temperature.DegreeCelsius);
			var hotOutletPort =  Ports[_internalConnections[hotInletPort.Identifier]];
			hotInletPort.CopyThermalStateTo(hotOutletPort);
			var coldInletPort = inletPorts.MinBy(x => x.Temperature.DegreeCelsius);
			var coldOutletPart = Ports[_internalConnections[coldInletPort.Identifier]];
			coldInletPort.CopyThermalStateTo(coldOutletPart);
			
			CoolpropJsFluid hotFluid;
			CoolpropJsFluid coldFluid;
			if (hotInletPort.Identifier == A || hotInletPort.Identifier == B)
			{
				hotFluid = Fluid1;
				coldFluid = Fluid2;
			}
			else
			{
				hotFluid = Fluid2;
				coldFluid = Fluid1;
			}
			
			var oppositeHotInletPort = Ports[_internalDiagonallyOppositePorts[hotInletPort.Identifier]];
			double hHotoutOutMinDdouble = hotFluid.CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', hotInletPort.Pressure.Pascal, 'T', oppositeHotInletPort.Temperature.Kelvin, FluidNameStrings.FluidNameToStringDict[hotFluid.FluidName]);
			var hHotOutMin = Enthalpy.FromJoulePerKilogram(hHotoutOutMinDdouble);
			var qMaxHot = (hotInletPort.Enthalpy - hHotOutMin) * hotInletPort.MassFlow;
			
			var oppositeColdInletPort = Ports[_internalDiagonallyOppositePorts[coldInletPort.Identifier]];
			double hColdOutMaxDdouble = coldFluid.CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', coldInletPort.Pressure.Pascal, 'T', oppositeColdInletPort.Temperature.Kelvin, FluidNameStrings.FluidNameToStringDict[coldFluid.FluidName]);
			var hColdOutMax = Enthalpy.FromJoulePerKilogram(hColdOutMaxDdouble);
			var qMaxCold = (hColdOutMax - coldInletPort.Enthalpy) * coldInletPort.MassFlow;

			var smallestQ = qMaxHot < qMaxCold ? qMaxHot : qMaxCold;

			Power q = smallestQ * Efficiency;
				
			foreach (var outletPort in outletPorts)
			{
				PortIdentifier internalConnectionIdentifier = _internalConnections[outletPort.Identifier];
				Port internalConnection = Ports[internalConnectionIdentifier];

				if (outletPort == hotOutletPort)
				{
					outletPort.Enthalpy = internalConnection.Enthalpy - q / internalConnection.MassFlow.Abs();
					double t = hotFluid.CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', outletPort.Pressure.Pascal, 'H', outletPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[hotFluid.FluidName]);
					double x = hotFluid.CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', outletPort.Pressure.Pascal, 'H', outletPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[hotFluid.FluidName]);
					outletPort.Temperature = Temperature.FromKelvin(t);
					outletPort.Quality = x;
				}
				else
				{
					outletPort.Enthalpy = internalConnection.Enthalpy + q / internalConnection.MassFlow.Abs();
					double t = coldFluid.CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', outletPort.Pressure.Pascal, 'H', outletPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[coldFluid.FluidName]);
					double x = coldFluid.CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', outletPort.Pressure.Pascal, 'H', outletPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[coldFluid.FluidName]);
					outletPort.Temperature = Temperature.FromKelvin(t);
					outletPort.Quality = x;
				}	
				TransferThermalState(outletPort);
			}

			sw.Stop();
			Debug.Print($"PHE calc time: {sw.ElapsedMilliseconds} ms");
		}

		public override void CalculateHeatBalanceEquation(Port port)
		{
			if (!Ports.ContainsValue(port))
			{
				throw new SolverException($"Cascaded port does not belong to {Name}");
			}

			PortIdentifier otherIdentifier = _internalConnections[port.Identifier];

			var otherPort = Ports[otherIdentifier];
			if (otherPort.Temperature.IsNaN() || otherPort.Enthalpy.IsNaN())
			{
				// Set temperature and enthalpy in the other port in the first iteration where values are NaN
				otherPort.Temperature = port.Connection.Temperature;
				otherPort.Enthalpy = port.Connection.Enthalpy;
				otherPort.Quality = port.Quality;
			}
			otherPort.Pressure = port.Pressure;

			TransferThermalState(otherPort);
			otherPort.Connection.Component.CalculateHeatBalanceEquation(otherPort.Connection);
		}

		public override void StartCascadeFluidTypeChange()
		{
			if (PortA.Connection != null)
			{
				PortA.Connection.Component.CascadeFluidTypeChange(PortA.Connection, FluidType1);
			}
			if (PortB.Connection != null)
			{
				PortB.Connection.Component.CascadeFluidTypeChange(PortB.Connection, FluidType1);
			}
			if (PortC.Connection != null)
			{
				PortC.Connection.Component.CascadeFluidTypeChange(PortC.Connection, FluidType2);
			}
			if (PortD.Connection != null)
			{
				PortD.Connection.Component.CascadeFluidTypeChange(PortD.Connection, FluidType2);
			}
		}
		
		public override void CascadeFluidTypeChange(Port port, FluidName fluidName)
		{
			if (port.Identifier == A && FluidType1 == fluidName || port.Identifier == B && FluidType1 == fluidName)
			{
				return;
			}
			if (port.Identifier == C && FluidType2 == fluidName || port.Identifier == D && FluidType2 == fluidName)
			{
				return;
			}

			if (port.Identifier == A || port.Identifier == B)
			{
				FluidType1 = fluidName;
				var otherIdentifier = _internalConnections[port.Identifier];
				var otherPort = Ports[otherIdentifier];
				if (otherPort.Connection != null)
				{
					otherPort.Connection.Component.CascadeFluidTypeChange(otherPort.Connection, fluidName);
				}
			}
			else if (port.Identifier == C || port.Identifier == D)
			{
				FluidType2 = fluidName;
				var otherIdentifier = _internalConnections[port.Identifier];
				var otherPort = Ports[otherIdentifier];
				if (otherPort.Connection != null)
				{
					otherPort.Connection.Component.CascadeFluidTypeChange(otherPort.Connection, fluidName);
				}
			}
		}

		public void TransferThermalState(Port port)
		{
			if (port.MassFlow is null)
			{
				throw new SolverException($"Port {port.Identifier} of {port.Component} has null massflow");
			}

			if (port.Connection is null)
			{
				throw new SolverException($"Port {port.Identifier} of {port.Component} has null connection");
			}

			port.Connection.ReceiveThermalStateFromConnection();
		}

		public override void TransferThermalState()
		{
			throw new NotImplementedException();
		}
	}
}
