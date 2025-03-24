using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using CycleCalculator.CycleModel.Model.IO;
using CycleCalculator.CycleModel.Model.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using CycleCalculator.CycleModel.Exceptions;
using Microsoft.JSInterop;
namespace CycleCalculator.CycleModel.Model
{
    class SimpleCompressor : CycleComponent, IPressureSetter, IMassFlowSetter, IPowerConsumer
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

		public SimpleCompressor(string name, IJSInProcessObjectReference coolprop) : base(name, coolprop)
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
            
            TransferState();
        }

        public override void CalculatePressureDrop(Port _)
        {
            return;
        }

        public override void CalculateHeatBalanceEquation(Port _)
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();
            Fluid.UpdatePH(upstreamPort.Pressure, upstreamPort.Enthalpy);
            SpecificEntropy inletEntropy = Fluid.Entropy;

            Fluid.UpdatePS(DischargePressure, inletEntropy);
            Enthalpy isentropicOutletEnthalpy = Fluid.Enthalpy;
            Enthalpy specificIsentropicWork = isentropicOutletEnthalpy - upstreamPort.Enthalpy;
            Enthalpy actualOutletEnthalpy = upstreamPort.Enthalpy + (specificIsentropicWork / Efficiency);

			Fluid.UpdatePH(DischargePressure, actualOutletEnthalpy);
            downstreamPort.Temperature = Fluid.Temperature;
            downstreamPort.Pressure = DischargePressure;
            downstreamPort.Enthalpy = actualOutletEnthalpy;

            PowerConsumption = upstreamPort.MassFlow * (downstreamPort.Enthalpy - upstreamPort.Enthalpy);

			TransferState();
            GetDownstreamPort().Connection.Component.CalculateHeatBalanceEquation(GetDownstreamPort().Connection);
        }

        public override void ReceiveAndCascadeTemperatureAndEnthalpy(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            bool portIsDownstreamPort = GetDownstreamPort() == port;
            if (portIsDownstreamPort)
            {
                throw new SolverException($"Downstream port of compressor {Name} is connected to the downstream port of compressor {port.Connection.Component.Name}");
            }

            port.Temperature = port.Connection.Temperature;
            port.Enthalpy = port.Connection.Enthalpy;
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
