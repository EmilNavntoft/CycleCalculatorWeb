using Accord.Math;
using CycleCalculatorWeb.CycleModel.Exceptions;
using CycleCalculatorWeb.CycleModel.Model.IO;
using EngineeringUnits;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static CycleCalculatorWeb.CycleModel.Model.IO.PortIdentifier;

namespace CycleCalculatorWeb.CycleModel.Model
{
    public class TeeSection : CycleComponent
    {
        [Editable(false)]
        double relativeDifferenceLimit = 0.00001;
		[Editable(false)]
		public Port PortC { get; set; }
        public TeeSection(string name) : base(name)
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
                if (port != cascadedPort)
                {
                    port.Connection.Component.ReceiveAndCascadePressure(port.Connection);
                }
            }
        }

        public override void ReceiveAndCascadeMassFlow(Port port, bool setAsFixedMassFlow)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            var negatedMassFlow = MassFlow.Zero - port.Connection.MassFlow; //Negate incoming massFlow in order to match reference frame of this component
            port.MassFlow = negatedMassFlow;

            if (setAsFixedMassFlow)
            {
                port.IsFixedMassFlow = true;
            }

            var unknownPorts = GetUnknownPorts();
            var upstreamPorts = GetUpstreamPorts();
            var downstreamPorts = GetDownstreamPorts();
            if (unknownPorts.Count == 1)
            {
                CalculateMassBalanceEquation();
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

        public override void CalculateMassBalanceEquation()
        {
            var upstreamPorts = GetUpstreamPorts();
            var downstreamPorts = GetDownstreamPorts();
            var unknownPorts = GetUnknownPorts();

            if (unknownPorts.Count == 0)
            {
                return;
            }

            if (IsMassBalanceEquationIndeterminate())
            {
                //Guess a split for now, for cases where only the downstream port is known
                SplitIndeterminatePortsEqually();
            }

            //There can only be one unknown port at this point, since indeterminate state has already been checked
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
                port.Connection.Component.ReceiveAndCascadeMassFlow(port.Connection, false);
            }
        }

        public override double[] GetMassBalanceEquations(double[] x)
        {
            double[] equations =
            [
                x[PortA.MdotIdent] + x[PortB.MdotIdent] + x[PortC.MdotIdent],
                x[PortA.PIdent] - x[PortB.PIdent],
                x[PortA.PIdent] - x[PortC.PIdent]
            ];
            
            if (PortA.Connection.Identifier == B)
            {
                double[] portAConnectionEquations = [
                    x[PortA.MdotIdent] + x[PortA.Connection.MdotIdent],
                    x[PortA.PIdent] - x[PortA.Connection.PIdent]
                    ];
                equations = equations.Concat(portAConnectionEquations).ToArray();
            }
            if (PortB.Connection.Identifier == B)
            {
                double[] portBConnectionEquations = [
                    x[PortB.MdotIdent] + x[PortB.Connection.MdotIdent],
                    x[PortB.PIdent] - x[PortB.Connection.PIdent]
                    ];
                equations = equations.Concat(portBConnectionEquations).ToArray();
            }
            if (PortC.Connection.Identifier == B)
            {
                double[] portCConnectionEquations = [
                    x[PortC.MdotIdent] + x[PortC.Connection.MdotIdent],
                    x[PortC.PIdent] - x[PortC.Connection.PIdent]
                    ];
                equations = equations.Concat(portCConnectionEquations).ToArray();
            }
            return equations;
        }

        public override void CalculatePressureDrop()
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
                downstreamPort.Connection.Component.CalculatePressureDrop();
            }
        }

        public override void CalculateHeatBalanceEquation()
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
                port.Connection.Component.CalculateHeatBalanceEquation();
            }
        }

        public bool HasMismatchedUpstreamPressures()
        {
            var upstreamPorts = Ports.Values.Where(port => port.MassFlow >= MassFlow.Zero).ToList();
            if (upstreamPorts.Count < 2) return false; //There must be two upstream ports for them to be able to be mismatched
            if (upstreamPorts.Any(port => port.IsFixedMassFlow)) return false; //If any of these ports are fixed, there is no room to adjust flows.

            
            double relativeDifference = (upstreamPorts[0].Pressure - upstreamPorts[1].Pressure).Abs() / upstreamPorts[0].Pressure;
            bool hasMismatchedLocalPorts = relativeDifference > relativeDifferenceLimit;

            bool hasMismatchedConnectedPorts = upstreamPorts.Any(port => 
            (port.Connection.Pressure - port.Pressure).Abs() / port.Connection.Pressure > relativeDifferenceLimit);

            return hasMismatchedLocalPorts || hasMismatchedConnectedPorts;
        }

        public void AdjustUpstreamFlowsToMismatchedPressures(Dictionary<PortIdentifier, MassFlow> upstreamPortMassFlowPairs)
        {
            int knownUpstreamPorts = GetUpstreamPorts().Count;
            if (knownUpstreamPorts != 0)
            {
                //If one of the upstream ports have already been set, there is no room to adjust upstream flows
                if (knownUpstreamPorts == 1 && GetDownstreamPorts().Count == 1)
                {
                    CalculateMassBalanceEquation();
                }
                return;
            }

            foreach (var pair in upstreamPortMassFlowPairs)
            {
                if (Ports[pair.Key].MassFlow == MassFlow.NaN)
                {
                    Ports[pair.Key].MassFlow = pair.Value;
                }
            }

            var upstreamPorts = GetUpstreamPorts();
            if (upstreamPorts.Count < 2) return;
            if (upstreamPorts.Any(port => port.IsFixedMassFlow)) return;

            var downstreamPorts = GetDownstreamPorts();

            MassFlow totalUpstreamFlow = MassFlow.Zero;
            upstreamPorts.ForEach(port => totalUpstreamFlow += port.MassFlow);

            var unknownPorts = GetUnknownPorts();
            if (downstreamPorts.Count == 1)
            {
                //Account for change in downstream flow due to adjustment of downstream tees.
                MassFlow totalActualUpstreamFlow = MassFlow.Zero - downstreamPorts[0].MassFlow;
                double previousToActualFlowRatio = totalActualUpstreamFlow.KilogramPerSecond / totalUpstreamFlow.KilogramPerSecond;
                upstreamPorts.ForEach(port => port.MassFlow *= previousToActualFlowRatio);
            } 
            else if (unknownPorts.Count == 1)
            {
                //This unknown port must be the downstream port
                unknownPorts[0].MassFlow = MassFlow.Zero - totalUpstreamFlow;
            }

            upstreamPorts.ForEach(port => port.Pressure = port.Connection.Pressure); //Retrieve connected port's pressure, as it might mismatch.

            double relativeDifference = (upstreamPorts[0].Pressure.Bar - upstreamPorts[1].Pressure.Bar)/ Math.Max(upstreamPorts[0].Pressure.Bar, upstreamPorts[1].Pressure.Bar);
            
            if (Math.Abs(relativeDifference) < relativeDifferenceLimit) return;


            double change = 0.05;
            double limit = totalUpstreamFlow.KilogramPerSecond / 4;
            if (relativeDifference > 0)
            {
                change = totalUpstreamFlow.KilogramPerSecond * relativeDifference;
                change = Math.Min(change, limit);
            }
            else
            {
                change = totalUpstreamFlow.KilogramPerSecond * relativeDifference;
                change = Math.Max(change, -limit);
            }

            upstreamPorts[0].MassFlow += MassFlow.FromKilogramPerSecond(change);
            upstreamPorts[1].MassFlow -= MassFlow.FromKilogramPerSecond(change);

            var knownPorts = Ports.Values.ToList().FindAll(port => port.MassFlow != MassFlow.NaN);
            foreach (var port in upstreamPorts)
            {
                port.Connection.Component.ReceiveAndCascadeMassFlow(port.Connection, false);
            }
            return;
        }

        public double GetRelativePressureDifference()
        {
            var upstreamPorts = GetUpstreamPorts();
            if (upstreamPorts.Count == 2)
            {
                return (upstreamPorts[0].Pressure.Bar - upstreamPorts[1].Pressure.Bar) / Math.Max(upstreamPorts[0].Pressure.Bar, upstreamPorts[1].Pressure.Bar);
            } else
            {
                return 0;
            }
        }

        public void CascadeKnownMassFlows()
        {
            if (GetUnknownPorts().Count != 0) return;

            foreach (Port port in Ports.Values)
            {
                port.Connection.Component.ReceiveAndCascadeMassFlow(port.Connection, false);
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

        public List<Port> GetFixedPorts()
        {
            return Ports.Values.ToList().FindAll(port => port.IsFixedMassFlow);
        }

        public void StartMassBalanceCalculation()
        {
            CalculateMassBalanceEquation();
            var downstreamPorts = GetDownstreamPorts();
            foreach (var port in downstreamPorts)
            {
                port.Connection.Component.CascadeMassBalanceCalculation();
            }
        }

        public override void CascadeMassBalanceCalculation()
        {
            var unknownPorts = GetUnknownPorts();
            if (unknownPorts.Any())
            {
                CalculateMassBalanceEquation();
            }
            var downstreamPorts = GetDownstreamPorts();
            TransferState();
            foreach (var port in downstreamPorts)
            {
                port.Connection.Component.CascadeMassBalanceCalculation();
            }
        }

        private void SplitIndeterminatePortsEqually()
        {
            var unknownDownstreamPorts = Ports.Values.ToList().FindAll(port => port.MassFlow == MassFlow.NaN);
            var upstreamPorts = GetUpstreamPorts();
            if (upstreamPorts.Count == 0) return;
            unknownDownstreamPorts.ForEach(port => port.MassFlow = MassFlow.Zero - upstreamPorts[0].MassFlow / 2);
        }
    }
}
