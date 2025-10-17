using CycleCalculator.CycleModel.Model.IO;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using System.ComponentModel.DataAnnotations;
using CycleCalculator.CycleModel.Exceptions;
using CycleCalculator.CycleModel.Solver;
using Microsoft.JSInterop;
using CycleCalculatorWeb.CoolpropJsInterop;

namespace CycleCalculator.CycleModel.Model
{
    public abstract class CycleComponent
    {
		public string Name { get; set; }
		[Editable(false)]
		public Port PortA { get; set; }
		[Editable(false)]
		public Port PortB { get; set; }
		[Editable(false)]
		public CoolpropJsFluid Fluid1 { get; private set; }

        private FluidName _fluidType1 = FluidName.Ammonia;
		public FluidName FluidType1
        {
            get
            {
                return _fluidType1;
            }
            set
            {
                _fluidType1 = value;
				Fluid1.FluidName = _fluidType1;

				StartCascadeFluidTypeChange();
			}
        }
		[Editable(false)]
		public Dictionary<PortIdentifier, Port> Ports { get; private set; } = new Dictionary<PortIdentifier, Port>();

        [Editable(false)]
        public Dictionary<PortIdentifier, PortState> StoredPortStates { get; private set; } = new Dictionary<PortIdentifier, PortState>();
        
		public CycleComponent(string name, IJSInProcessObjectReference coolProp)
        {
            Name = name;
            Fluid1 = new CoolpropJsFluid(coolProp);
		}

        public abstract void CalculateMassBalanceEquation(Port port);

        public virtual void CalculateHeatBalanceEquation(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            port.CopyThermalStateTo(otherPort);
            
            TransferThermalState();
            otherPort.Connection.Component.CalculateHeatBalanceEquation(otherPort.Connection);
        }

        public virtual void CalculatePressureDrop(Port port)
        {
            var downstreamPort = GetDownstreamPort();
            downstreamPort.Pressure = GetUpstreamPort().Pressure;

            TransferThermalState();
            downstreamPort.Connection.Component.CalculatePressureDrop(downstreamPort.Connection);
        }

        public virtual void TransferThermalState()
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

                port.Connection.ReceiveThermalStateFromConnection();
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

        public virtual void ReceiveAndCascadeMassFlow(Port port)
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

            otherPort.Connection.Component.ReceiveAndCascadeMassFlow(otherPort.Connection);
        }

        public virtual void StartCascadeFluidTypeChange()
        {
            if (PortA.Connection != null)
            {
                PortA.Connection.Component.CascadeFluidTypeChange(PortA.Connection, FluidType1);
            }
            if (PortB.Connection != null)
            {
                PortB.Connection.Component.CascadeFluidTypeChange(PortB.Connection, FluidType1);
            }
        }

        public virtual void CascadeFluidTypeChange(Port port, FluidName fluidName)
        {
            if (FluidType1 == fluidName)
            {
                return;
            }
            FluidType1 = fluidName;
            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            if (otherPort.Connection != null)
            {
                otherPort.Connection.Component.CascadeFluidTypeChange(otherPort.Connection, fluidName);
            }
        }


        public virtual Port GetUpstreamPort()
        {
            Port upstreamPort = Ports.Values.First(port => port.MassFlow > MassFlow.Zero) ?? throw new SolverException($"Upstream ports for {Name} are undetermined");

            return upstreamPort;
        }

        public virtual Port GetDownstreamPort()
        {
            Port downstreamPort = Ports.Values.First(port => port.MassFlow == MassFlow.NaN || port.MassFlow < MassFlow.Zero)
                ?? throw new SolverException($"Downstream ports for {Name} are undetermined");

            return downstreamPort;
        }

        public void StorePortStates()
        {
            foreach (var key in Ports.Keys)
            {
                StoredPortStates[key] = new PortState()
                {
                    PressureBars = Ports[key].Pressure.Bar,
                    TemperatureCelsius = Ports[key].Temperature.DegreeCelsius,
                    EnthalpyJoulePerKilogram = Ports[key].Enthalpy.JoulePerKilogram
                };
            }
        }

        public double CalculateResidual()
        {
            double pressureResidual = 0;
            double temperatureResidual = 0;
            double enthalpyResidual = 0;

            foreach (var key in Ports.Keys)
            {
                double pr = Math.Abs((StoredPortStates[key].PressureBars - Ports[key].Pressure.Bar) / StoredPortStates[key].PressureBars);
                double tr = Math.Abs((StoredPortStates[key].TemperatureCelsius - Ports[key].Temperature.DegreeCelsius) / StoredPortStates[key].TemperatureCelsius);
                double hr = Math.Abs((StoredPortStates[key].EnthalpyJoulePerKilogram - Ports[key].Enthalpy.JoulePerKilogram) / StoredPortStates[key].EnthalpyJoulePerKilogram);

                if (pr > pressureResidual)
                {
                    pressureResidual = pr;
                }
                if (tr > temperatureResidual)
                {
                    temperatureResidual = tr;
                }
                if (hr > enthalpyResidual)
                {
                    enthalpyResidual = hr;
                }
            }

            return new[] { pressureResidual, temperatureResidual, enthalpyResidual }.Max();
        }
    }
}
