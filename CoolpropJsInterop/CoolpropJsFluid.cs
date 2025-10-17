using EngineeringUnits;
using Microsoft.JSInterop;
using System.Diagnostics;

namespace CycleCalculatorWeb.CoolpropJsInterop
{
    public class CoolpropJsFluid
    {
	    public IJSInProcessObjectReference CoolpropJs { get; set; }
		public Density Density { get; set; } = Density.Zero;
		public Enthalpy Enthalpy { get; set; } = Enthalpy.Zero;
		public Temperature Temperature { get; set; } = Temperature.Zero;
		public Pressure Pressure { get; set; } = Pressure.Zero;
		public SpecificEntropy Entropy { get; set; } = SpecificEntropy.Zero;
		public SpecificHeatCapacity SpecificHeatCapacity { get; set; } = SpecificHeatCapacity.Zero;

		public double Quality { get; set; } = 0;

		public FluidName FluidName { get; set; } = FluidName.Ammonia;

		public CoolpropJsFluid(IJSInProcessObjectReference coolprop)
        {
            CoolpropJs = coolprop;
        }

        public void UpdatePH(Pressure p, Enthalpy h)
        {
			double T = CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', p.Pascal, 'H', h.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double d = CoolpropJs.Invoke<double>("PropsSI", 'D', 'P', p.Pascal, 'H', h.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double s = CoolpropJs.Invoke<double>("PropsSI", 'S', 'P', p.Pascal, 'H', h.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double cp = CoolpropJs.Invoke<double>("PropsSI", 'C', 'P', p.Pascal, 'H', h.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double x = CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', p.Pascal, 'H', h.JoulePerKilogram, FluidNameStrings.FluidNameToStringDict[FluidName]);
			Temperature = Temperature.FromKelvin(T);
			Density = Density.FromKilogramPerCubicMeter(d);
			Entropy = SpecificEntropy.FromJoulePerKilogramKelvin(s);
			SpecificHeatCapacity = SpecificHeatCapacity.FromJoulePerKilogramKelvin(cp);
			Pressure = p;
			Enthalpy = h;
			Quality = x;
        }

		public void UpdatePS(Pressure p, SpecificEntropy s)
		{
			double T = CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', p.Pascal, 'S', s.JoulePerKilogramKelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double d = CoolpropJs.Invoke<double>("PropsSI", 'D', 'P', p.Pascal, 'S', s.JoulePerKilogramKelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double h = CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', p.Pascal, 'S', s.JoulePerKilogramKelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double cp = CoolpropJs.Invoke<double>("PropsSI", 'C', 'P', p.Pascal, 'S', s.JoulePerKilogramKelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double x = CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', p.Pascal, 'S', s.JoulePerKilogramKelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			Temperature = Temperature.FromKelvin(T);
			Density = Density.FromKilogramPerCubicMeter(d);
			Enthalpy = Enthalpy.FromJoulePerKilogram(h);
			SpecificHeatCapacity = SpecificHeatCapacity.FromJoulePerKilogramKelvin(cp);
			Pressure = p;
			Entropy = s;
			Quality = x;
		}

		public void UpdatePT(Pressure p, Temperature T)
		{
			double s = CoolpropJs.Invoke<double>("PropsSI", 'S', 'P', p.Pascal, 'T', T.Kelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double d = CoolpropJs.Invoke<double>("PropsSI", 'D', 'P', p.Pascal, 'T', T.Kelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double h = CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', p.Pascal, 'T', T.Kelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double cp = CoolpropJs.Invoke<double>("PropsSI", 'C', 'P', p.Pascal, 'T', T.Kelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			double x = CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', p.Pascal, 'T', T.Kelvin, FluidNameStrings.FluidNameToStringDict[FluidName]);
			Entropy = SpecificEntropy.FromJoulePerKilogramKelvin(s);
			Density = Density.FromKilogramPerCubicMeter(d);
			Enthalpy = Enthalpy.FromJoulePerKilogram(h);
			SpecificHeatCapacity = SpecificHeatCapacity.FromJoulePerKilogramKelvin(cp);
			Pressure = p;
			Temperature = T;
			Quality = x;
		}

		public Temperature GetSatTemperature(Pressure p)
		{
			double x = 0;
			double T = CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', p.Pascal, 'Q', x, FluidNameStrings.FluidNameToStringDict[FluidName]);
			return Temperature.FromKelvins(T);
		}

		public double GetQuality(Pressure p, Enthalpy h)
		{
			double x = CoolpropJs.Invoke<double>("PropsSI", 'Q', 'P', p.Pascal, 'H', h.JoulePerKilogram,
				FluidNameStrings.FluidNameToStringDict[FluidName]);
			return x;
		}
    }

}
