using CycleCalculatorWeb.CycleModel.Model.IO;
using static CycleCalculatorWeb.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using SharpFluids;
using System.ComponentModel.DataAnnotations;
using CycleCalculatorWeb.CycleModel.Exceptions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CycleCalculatorWeb.CycleModel.Model
{
    public abstract class CycleComponent
    {
		public string Name { get; set; }
		[Editable(false)]
		public Port PortA { get; set; }
		[Editable(false)]
		public Port PortB { get; set; }
		[Editable(false)]
		public Fluid Fluid { get; private set; } = new Fluid(FluidList.Ammonia);

        private FluidList _FluidType = FluidList.Ammonia;
		public FluidList FluidType
        {
            get
            {
                return _FluidType;
            }
            set
            {
                _FluidType = value;
				Fluid.SetNewMedia(value);
			}
        }

		[Editable(false)]
		public Dictionary<PortIdentifier, Port> Ports { get; private set; } = new Dictionary<PortIdentifier, Port>();

		public CycleComponent(string name)
        {
            Name = name;
        }

        public abstract void CalculateMassBalanceEquation();

        public abstract void CalculateHeatBalanceEquation();

        public virtual void CalculatePressureDrop()
        {
            var downstreamPort = GetDownstreamPort();
            downstreamPort.Pressure = GetUpstreamPort().Pressure;

            TransferState();
            downstreamPort.Connection.Component.CalculatePressureDrop();
        }

        public abstract double[] GetMassBalanceEquations(double[] x);

        public virtual bool IsMassBalanceEquationIndeterminate()
        {
            return Ports.Values.All(port => port.MassFlow == MassFlow.NaN);
        }

        public virtual void TransferState()
        {
            foreach (Port port in Ports.Values)
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
        }

        public virtual void ReceiveAndCascadePressure(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            port.Pressure = port.Connection.Pressure;

            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            otherPort.Pressure = port.Connection.Pressure;

            otherPort.Connection.Component.ReceiveAndCascadePressure(otherPort.Connection);
        }

        public virtual void ReceiveAndCascadeMassFlow(Port port, bool setAsFixedMassFlow)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }
            if (port.MassFlow == MassFlow.Zero - port.Connection.MassFlow) return;

            var negatedMassFlow = MassFlow.Zero - port.Connection.MassFlow; //Negate incoming massFlow in order to match reference frame of this component
            port.MassFlow = negatedMassFlow;

            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            otherPort.MassFlow = port.Connection.MassFlow;

            otherPort.Connection.Component.ReceiveAndCascadeMassFlow(otherPort.Connection, setAsFixedMassFlow);
        }

        public virtual void ReceiveAndCascadeTemperatureAndEnthalpy(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            var negatedMassFlow = MassFlow.Zero - port.Connection.MassFlow; //Negate incoming massFlow in order to match reference frame of this component
            port.MassFlow = negatedMassFlow;
            port.Temperature = port.Connection.Temperature;
            port.Enthalpy = port.Connection.Enthalpy;

            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            otherPort.MassFlow = port.Connection.MassFlow;
            otherPort.Temperature = port.Connection.Temperature;
            otherPort.Enthalpy = port.Connection.Enthalpy;

            otherPort.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(otherPort.Connection);
        }

        public virtual void CascadeMassBalanceCalculation()
        {
            CalculateMassBalanceEquation();
            GetDownstreamPort().Connection.Component.CascadeMassBalanceCalculation();
        }

        public virtual Port GetUpstreamPort()
        {
            Port upstreamPort = Ports.Values.First(port => port.MassFlow > MassFlow.Zero) ?? throw new SolverException($"Upstream ports for {Name} are undetermined");

            return upstreamPort;
        }

        public virtual Port GetDownstreamPort()
        {
            Port downstreamPort = Ports.Values.First(port => port.MassFlow == MassFlow.NaN || port.MassFlow < MassFlow.Zero)
                ?? throw new SolverException($"Upstream ports for {Name} are undetermined");

            return downstreamPort;
        }
    }
}
