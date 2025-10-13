using Accord.Math;
using CycleCalculator.CycleModel.Exceptions;
using CycleCalculator.CycleModel.Model.IO;
using EngineeringUnits;
using Microsoft.JSInterop;
using SharpFluids;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;

namespace CycleCalculator.CycleModel.Model
{
    public class TeeSection : CycleComponent
    {
		[Editable(false)]
		public Port PortC { get; set; }
        public TeeSection(string name, IJSInProcessObjectReference coolProp) : base(name, coolProp)
        {
            PortA = new Port(A, this);
            PortB = new Port(B, this);
            PortC = new Port(C, this);
            Ports.Add(A, PortA);
            Ports.Add(B, PortB);
            Ports.Add(C, PortC);
        }

        public override void ReceiveAndCascadePressure(Port cascadedPort)
        {
            if (!Ports.ContainsValue(cascadedPort))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            foreach (Port port in Ports.Values)
            {
                port.Pressure = cascadedPort.Connection.Pressure;
                if (port != cascadedPort && port.Pressure != port.Connection.Pressure)
                {
                    port.Connection.Component.ReceiveAndCascadePressure(port.Connection);
                }
            }
        }

        public override void ReceiveAndCascadeMassFlow(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            var negatedMassFlow = MassFlow.Zero - port.Connection.MassFlow; //Negate incoming massFlow in order to match reference frame of this component
            port.MassFlow = negatedMassFlow;

            var unknownPorts = GetUnknownPorts();
            var upstreamPorts = GetUpstreamPorts();
            var downstreamPorts = GetDownstreamPorts();
            if (unknownPorts.Count == 1)
            {
                CalculateMassBalanceEquation(unknownPorts[0].Connection);
            }
        }

        public override void ReceiveAndCascadeTemperatureAndEnthalpy(Port cascadedPort)
        {
            if (!Ports.ContainsValue(cascadedPort))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            cascadedPort.Temperature = cascadedPort.Connection.Temperature;
            cascadedPort.Enthalpy = cascadedPort.Connection.Enthalpy;

            List<Port> upstreamPorts = GetUpstreamPorts();
            List<Port> downstreamPorts = GetDownstreamPorts();

            if (downstreamPorts.Count == 1 && upstreamPorts.Count == 2)
            {
                if (upstreamPorts.Any(port => port.Enthalpy == Enthalpy.NaN)) return;

                var totalDownstreamMassFlow = upstreamPorts[0].MassFlow + upstreamPorts[1].MassFlow;
                downstreamPorts[0].Enthalpy = (upstreamPorts[0].Enthalpy * upstreamPorts[0].MassFlow + upstreamPorts[1].Enthalpy * upstreamPorts[1].MassFlow) / totalDownstreamMassFlow;
                downstreamPorts[0].Temperature = (upstreamPorts[0].Temperature * upstreamPorts[0].MassFlow + upstreamPorts[1].Temperature * upstreamPorts[1].MassFlow) / totalDownstreamMassFlow;

                foreach (Port port in downstreamPorts)
                {
                    port.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(port.Connection);
                }
            }
            else
            {
                foreach (Port port in downstreamPorts)
                {
                    port.Temperature = cascadedPort.Connection.Temperature;
                    port.Enthalpy = cascadedPort.Connection.Enthalpy;
                    port.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(port.Connection);
                }
            }
        }

        public override void CalculateMassBalanceEquation(Port _)
        {
            var upstreamPorts = GetUpstreamPorts();
            var downstreamPorts = GetDownstreamPorts();
            var unknownPorts = GetUnknownPorts();

            if (unknownPorts.Count == 0)
            {
                return;
            }

            unknownPorts = GetUnknownPorts();
            if (unknownPorts.Count == 1)
            {
                MassFlow incomingFlow = MassFlow.Zero;
                MassFlow outgoingFlow = MassFlow.Zero;

                upstreamPorts.ForEach(port => incomingFlow += port.MassFlow);
                downstreamPorts.ForEach(port => outgoingFlow += port.MassFlow);

                unknownPorts[0].MassFlow = outgoingFlow.Abs() - incomingFlow;
            }

            downstreamPorts = GetDownstreamPorts(); //Recheck for downstream ports

            foreach (var port in Ports.Values)
            {
                port.Connection.Component.ReceiveAndCascadeMassFlow(port.Connection);
            }
        }

        public override void CalculatePressureDrop(Port port)
        {
            var upstreamPorts = GetUpstreamPorts();

            var downstreamPorts = GetDownstreamPorts();

            if (upstreamPorts.Any(port => port.Pressure == Pressure.NaN)) return;

            foreach (var downstreamPort in downstreamPorts)
            {
                downstreamPort.Pressure = upstreamPorts[0].Pressure;
            }

            TransferState(); 

            foreach (var downstreamPort in downstreamPorts)
            {
                downstreamPort.Connection.Component.CalculatePressureDrop(downstreamPort.Connection);
            }
        }

        public override void CalculateHeatBalanceEquation(Port incomingPort)
        {
            var upstreamPorts = GetUpstreamPorts();
            if (upstreamPorts.Count == 0)
            {
                throw new SolverException($"Tee section {Name} has not received a downstream massflow");
            }

            var downstreamPorts = GetDownstreamPorts();

            if (downstreamPorts.Count == 1 && upstreamPorts.Count == 2)
            {
                if (upstreamPorts.Any(port => port.Enthalpy == Enthalpy.NaN)) return;

                var totalDownstreamMassFlow = upstreamPorts[0].MassFlow + upstreamPorts[1].MassFlow;
                downstreamPorts[0].Enthalpy = (upstreamPorts[0].Enthalpy * upstreamPorts[0].MassFlow + upstreamPorts[1].Enthalpy * upstreamPorts[1].MassFlow) / totalDownstreamMassFlow;
                downstreamPorts[0].Temperature = (upstreamPorts[0].Temperature * upstreamPorts[0].MassFlow + upstreamPorts[1].Temperature * upstreamPorts[1].MassFlow) / totalDownstreamMassFlow;
            }
            else if (downstreamPorts.Count == 1 && upstreamPorts.Count == 1)
            {
                downstreamPorts[0].Enthalpy = upstreamPorts[0].Enthalpy;
                downstreamPorts[0].Temperature = upstreamPorts[0].Temperature;
            }
            else if (downstreamPorts.Count == 2)
            {
                foreach (Port port in downstreamPorts)
                {
                    port.Enthalpy = upstreamPorts[0].Enthalpy;
                    port.Temperature = upstreamPorts[0].Temperature;
                }
            }
            else
            {
                //Not possible, throw exception anyway just to be sure
                throw new SolverException($"More than two downstream ports in {Name}");
            }

            TransferState();

            foreach (var port in downstreamPorts)
            {
                port.Connection.Component.CalculateHeatBalanceEquation(port.Connection);
            }
        }

        public override bool IsMassBalanceEquationIndeterminate()
        {
            var unknownPorts = GetUnknownPorts();
            var upstreamPorts = GetUpstreamPorts();
            return unknownPorts.Count == 2 && upstreamPorts.Count == 1;
        }

        public List<Port> GetDownstreamPorts()
        {
            List<Port> downstreamPorts = new List<Port>();
            foreach (var port in Ports.Values)
            {
                if (port.MassFlow < MassFlow.Zero)
                {
                    downstreamPorts.Add(port);
                }
            }

            return downstreamPorts;
        }

        public List<Port> GetUpstreamPorts()
        {
            List<Port> upstreamPorts = new List<Port>();
            foreach (var port in Ports.Values)
            {
                if (port.MassFlow > MassFlow.Zero)
                {
                    upstreamPorts.Add(port);
                }
            }

            return upstreamPorts;
        }

        public override Port GetUpstreamPort()
        {
            throw new NotImplementedException();
        }

        public override Port GetDownstreamPort()
        {
            throw new NotImplementedException();
        }

        public List<Port> GetUnknownPorts()
        {
            return Ports.Values.ToList().FindAll(port => port.MassFlow == MassFlow.NaN);
        }

        public void StartMassBalanceCalculation()
        {
            CalculateMassBalanceEquation(null);
            var downstreamPorts = GetDownstreamPorts();
            foreach (var port in downstreamPorts)
            {
                port.Connection.Component.CalculateMassBalanceEquation(port.Connection);
            }
        }
    }
}
