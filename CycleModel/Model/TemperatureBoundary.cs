using CycleCalculatorWeb.CycleModel.Model.IO;
using static CycleCalculatorWeb.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using CycleCalculatorWeb.CycleModel.Model.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using CycleCalculatorWeb.CycleModel.Exceptions;

namespace CycleCalculatorWeb.CycleModel.Model
{
    public class TemperatureBoundary : CycleComponent, ITemperatureOrEnthalpySetter
    {
        [Editable(false)]
        public Temperature OutletTemperature { get; set; } = Temperature.FromDegreeCelsius(10);

		[DisplayName("Outlet temperature [°C]")]
		public double OutletTemperatureDouble
		{
			get
			{
				return OutletTemperature.DegreeCelsius;
			}
			set
			{
				OutletTemperature = Temperature.FromDegreeCelsius(value);
			}
		}
		public TemperatureBoundary(string name) : base(name)
        {
            PortA = new Port(A, this);
            PortB = new Port(B, this);
            Ports.Add(A, PortA);
            Ports.Add(B, PortB);
        }

        public override void CalculateMassBalanceEquation()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            downstreamPort.MassFlow = MassFlow.Zero - upstreamPort.MassFlow;

            TransferState();
        }

        public override double[] GetMassBalanceEquations(double[] x)
        {
            double[] equations =
            [
                x[PortA.MdotIdent] + x[PortB.MdotIdent],
                x[PortA.MdotIdent] + x[PortA.Connection.MdotIdent],
                x[PortA.PIdent] - x[PortA.Connection.PIdent],
                x[PortB.PIdent] - x[PortA.PIdent]
            ];
            return equations;
        }


        public override void CalculateHeatBalanceEquation()
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();
            Fluid.UpdatePT(upstreamPort.Pressure, OutletTemperature);
            downstreamPort.Enthalpy = Fluid.Enthalpy;
            downstreamPort.Temperature = Fluid.Temperature;
        }

        public override bool IsMassBalanceEquationIndeterminate()
        {
            return false;
        }

        public override void ReceiveAndCascadeTemperatureAndEnthalpy(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }
            port.Temperature = port.Connection.Temperature;
            port.Pressure = port.Connection.Pressure;
            port.Enthalpy = port.Connection.Enthalpy;

            var otherIdentifier = port.Identifier == A ? B : A;
            var otherPort = Ports[otherIdentifier];
            otherPort.Pressure = port.Connection.Pressure;
            otherPort.Temperature = OutletTemperature;
            if (port.Connection.Pressure != Pressure.NaN)
            {
                Fluid.UpdatePT(port.Connection.Pressure, OutletTemperature);
                otherPort.Enthalpy = Fluid.Enthalpy;
            }
        }

        public void CascadeTemperatureAndEnthalpyDownstream()
        {
            CalculateHeatBalanceEquation();
            Port downstreamPort = GetDownstreamPort();
            downstreamPort.Connection.Component.ReceiveAndCascadeTemperatureAndEnthalpy(downstreamPort.Connection);
        }

        public void StartHeatBalanceCalculation()
        {
            CalculateHeatBalanceEquation();
            TransferState();
            GetDownstreamPort().Connection.Component.CalculateHeatBalanceEquation();
        }
    }
}
