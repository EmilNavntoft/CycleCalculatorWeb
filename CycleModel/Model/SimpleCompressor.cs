using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using CycleCalculator.CycleModel.Model.IO;
using CycleCalculator.CycleModel.Model.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Diagnostics;
using CycleCalculator.CycleModel.Exceptions;
using CycleCalculatorWeb.CoolpropJsInterop;
using Microsoft.JSInterop;
namespace CycleCalculator.CycleModel.Model
{
    internal class SimpleCompressor : CycleComponent, IPressureSetter, IMassFlowSetter, IPowerConsumer
    {
		[DisplayName("Isentropic efficiency")]
		public double Efficiency { get; set; } = 0.7;
        [Editable(false)]
		public MassFlow NominalMassFlow { get; set; } = MassFlow.FromKilogramPerSecond(1);
		[Editable(false)]
		public MassFlow NominalMassFlowError { get; private set; } = MassFlow.Zero;
        [Editable(false)]
        public Power PowerConsumption { get; set; } = Power.NaN;
		[Editable(false)]
		public Pressure DischargePressure { get; set; } = Pressure.FromBar(5);
		[DisplayName("Discharge pressure [bar]")]
		public double DischargePressureDouble
		{
			get
			{
				return DischargePressure.Bar;
			}
			set
			{
				DischargePressure = Pressure.FromBar(value);
			}
		}

		[DisplayName("Nominal mass flow [kg/s]")]
		public double NominalMassFlowDouble
		{
			get
			{
				return NominalMassFlow.KilogramPerSecond;
			}
			set
			{
				NominalMassFlow = MassFlow.FromKilogramPerSecond(value);
			}
		}

		public SimpleCompressor(string name, IJSInProcessObjectReference coolProp) : base(name, coolProp)
        {
            PortA = new Port(A, this);
            PortB = new Port(B, this);
            Ports.Add(A, PortA);
            Ports.Add(B, PortB);
            InitializeMassFlow();
        }

        public override void CalculateMassBalanceEquation(Port _)
        {
            PortB.MassFlow = MassFlow.Zero - PortA.MassFlow;
            NominalMassFlowError = PortA.MassFlow - NominalMassFlow;
            if (PortA.MassFlow > NominalMassFlow)
            {
                PortB.MassFlow = MassFlow.Zero - NominalMassFlow;
            }
            
            TransferThermalState();
        }

        public void StartHeatBalanceCalculation()
        {
            CalculateHeatBalanceEquation(null);
            TransferThermalState();
            GetDownstreamPort().Connection.Component.CalculateHeatBalanceEquation(GetDownstreamPort().Connection);
        }

        public override void CalculateHeatBalanceEquation(Port _)
        {
            Stopwatch sw =  new Stopwatch();

            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();
            
            double s = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'S', 'P', upstreamPort.Pressure.Pascal, 'H', upstreamPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);
            SpecificEntropy inletEntropy = SpecificEntropy.FromJoulePerKilogramKelvin(s);

            double h;
            try
            {
                h = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', DischargePressure.Pascal, 'S', inletEntropy.JoulePerKilogramKelvin, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);
            }
            catch
            {
                h = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', DischargePressure.Pascal, "S|twophase", inletEntropy.JoulePerKilogramKelvin, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);
            }

            Enthalpy isentropicOutletEnthalpy = Enthalpy.FromJoulePerKilogram(h);
            Enthalpy specificIsentropicWork = isentropicOutletEnthalpy - upstreamPort.Enthalpy;
            Enthalpy actualOutletEnthalpy = upstreamPort.Enthalpy + (specificIsentropicWork / Efficiency);

            double t = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', DischargePressure.Pascal, 'H', actualOutletEnthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);
            double x = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', DischargePressure.Pascal, 'H', actualOutletEnthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);

            downstreamPort.Temperature = Temperature.FromKelvin(t);
            downstreamPort.Pressure = DischargePressure;
            downstreamPort.Enthalpy = actualOutletEnthalpy;
            downstreamPort.Quality = x;

            PowerConsumption = upstreamPort.MassFlow * (downstreamPort.Enthalpy - upstreamPort.Enthalpy);

			TransferThermalState();
            sw.Stop();
            Debug.Print($"Compressor calc time: {sw.ElapsedMilliseconds} ms");
        }

        public override void ReceiveAndCascadeMassFlow(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }
            if (port.MassFlow == MassFlow.Zero - port.Connection.MassFlow) return;

            if (port.Connection.MassFlow > NominalMassFlow)
            {
                return;
            }

            var negatedMassFlow = MassFlow.Zero - port.Connection.MassFlow; //Negative MassFlow is taken to fit reference of this component

            port.MassFlow = negatedMassFlow;

            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            otherPort.MassFlow = port.Connection.MassFlow;

            otherPort.Connection.Component.ReceiveAndCascadeMassFlow(otherPort.Connection);
        }

        public override void ReceiveAndCascadePressure(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            bool portIsDownstreamPort = GetDownstreamPort() == port;
            if (portIsDownstreamPort)
            {
                if (port.Pressure != port.Connection.Pressure)
                {
                    throw new SolverException($"Compressor {Name} discharge has a different pressure from its connection {port.Connection.Component.Name}");
                }
                return;
            }

            port.Pressure = port.Connection.Pressure;
        }

        public void CascadePressureDownstream()
        {
            if (PortB.Connection.Pressure == PortB.Pressure) return;
            PortB.Connection.Component.ReceiveAndCascadePressure(PortB.Connection);
        }

        public void CascadeMassFlowDownstream()
        {
            PortB.Connection.Component.ReceiveAndCascadeMassFlow(PortB.Connection);
        }
        public void CascadeMassFlowUpstream()
        {
            PortA.Connection.Component.ReceiveAndCascadeMassFlow(PortA.Connection);
        }

        public void StartMassBalanceCalculation()
        {
            CalculateMassBalanceEquation(GetDownstreamPort().Connection);
            GetDownstreamPort().Connection.Component.CalculateMassBalanceEquation(GetDownstreamPort().Connection);
		}

        public void StartPressureDropCalculation()
        {
            GetDownstreamPort().Connection.Component.CalculatePressureDrop(GetDownstreamPort().Connection);
        }

        public override Port GetUpstreamPort()
        {
            return PortA;
        }

        public override Port GetDownstreamPort()
        {
            return PortB;
        }

        public void InitializeMassFlow()
        {
            PortB.MassFlow = MassFlow.Zero - NominalMassFlow;
            PortA.MassFlow = NominalMassFlow;
            PortB.Pressure = DischargePressure;
        }

        public void InitializePressure()
        {
            PortB.Pressure = DischargePressure;
        }
    }
}
