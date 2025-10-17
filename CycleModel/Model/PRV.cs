using CycleCalculator.CycleModel.Model.IO;
using static CycleCalculator.CycleModel.Model.IO.PortIdentifier;
using EngineeringUnits;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CycleCalculator.CycleModel.Exceptions;
using CycleCalculatorWeb.CoolpropJsInterop;
using Microsoft.JSInterop;

namespace CycleCalculator.CycleModel.Model
{
    public class Prv : CycleComponent
    {
        [Editable(false)]
        public Pressure OutletPressure { get; set; } = Pressure.FromBar(1);

		[DisplayName("Outlet pressure [bar]")]
		public double OutletPressureDouble
		{
			get
			{
				return OutletPressure.Bar;
			}
			set
			{
				OutletPressure = Pressure.FromBar(value);
			}
		}

		public Prv(string name, IJSInProcessObjectReference coolProp) : base(name, coolProp)
        {
            PortA = new Port(A, this);
            PortB = new Port(B, this);
            Ports.Add(A, PortA);
            Ports.Add(B, PortB);
        }

        public override void CalculateMassBalanceEquation(Port _)
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();

            downstreamPort.MassFlow = MassFlow.Zero - upstreamPort.MassFlow;

            if (downstreamPort.Pressure != Pressure.NaN && downstreamPort.Pressure != OutletPressure) 
            {
                throw new SolverException($"Outlet pressure of {Name} set by downstream component to a value that does not match OutletPressure. " +
                    $"Can also be a sign of reverse flow through the PRV.");
            }
            
            downstreamPort.Pressure = OutletPressure;

            TransferThermalState();

			downstreamPort.Connection.Component.CalculateMassBalanceEquation(downstreamPort.Connection);
		}

        public override void CalculateHeatBalanceEquation(Port _)
        {
            Port upstreamPort = GetUpstreamPort();
            Port downstreamPort = GetDownstreamPort();
            double t = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', downstreamPort.Pressure.Pascal, 'H', upstreamPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);
            double x = Fluid1.CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', downstreamPort.Pressure.Pascal, 'H', upstreamPort.Enthalpy.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[Fluid1.FluidName]);

            downstreamPort.Temperature = Temperature.FromKelvin(t);
            downstreamPort.Enthalpy = upstreamPort.Enthalpy;
            downstreamPort.Pressure = OutletPressure;
            downstreamPort.Quality = x;
            

            TransferThermalState();
            downstreamPort.Connection.Component.CalculateHeatBalanceEquation(downstreamPort.Connection);
        }

        public override void ReceiveAndCascadePressure(Port port)
        {
            if (!Ports.ContainsValue(port))
            {
                throw new SolverException($"Cascaded port does not belong to {Name}");
            }

            port.Pressure = port.Connection.Pressure;
        }
        public void CascadePressureDownstream()
        {
            Port downstreamPort = GetDownstreamPort();
            downstreamPort.Pressure = OutletPressure;
            
            downstreamPort.Connection.Component.ReceiveAndCascadePressure(downstreamPort.Connection);
        }
    }
}
