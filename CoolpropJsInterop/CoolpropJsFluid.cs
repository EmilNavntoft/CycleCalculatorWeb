using EngineeringUnits;
using Microsoft.JSInterop;
using System.Diagnostics;

namespace CycleCalculatorWeb.CoolpropJsInterop
{
    public class CoolpropJsFluid
    {
		IJSInProcessObjectReference CoolpropJs { get; set; }
		public Density Density { get; set; } = Density.Zero;
		public Enthalpy Enthalpy { get; set; } = Enthalpy.Zero;
		public Temperature Temperature { get; set; } = Temperature.Zero;
		public Pressure Pressure { get; set; } = Pressure.Zero;
		public SpecificEntropy Entropy { get; set; } = SpecificEntropy.Zero;

		public FluidNames FluidName { get; set; } = FluidNames.Ammonia;

		public CoolpropJsFluid(IJSInProcessObjectReference coolprop)
        {
            CoolpropJs = coolprop;
        }

        public void UpdatePH(Pressure P, Enthalpy H)
        {
			double T = CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString());
			double D = CoolpropJs.Invoke<double>("PropsSI", 'D', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString());
			double S = CoolpropJs.Invoke<double>("PropsSI", 'S', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString());
			Temperature = Temperature.FromKelvin(T);
			Density = Density.FromKilogramPerCubicMeter(D);
			Entropy = SpecificEntropy.FromJoulePerKilogramKelvin(S);
			Pressure = P;
			Enthalpy = H;
		}

		public void UpdatePS(Pressure P, SpecificEntropy S)
		{
			double T = CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', P.Pascal, 'S', S.JoulePerKilogramKelvin, FluidName.ToString());
			double D = CoolpropJs.Invoke<double>("PropsSI", 'D', 'P', P.Pascal, 'S', S.JoulePerKilogramKelvin, FluidName.ToString());
			double H = CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', P.Pascal, 'S', S.JoulePerKilogramKelvin, FluidName.ToString());
			Temperature = Temperature.FromKelvin(T);
			Density = Density.FromKilogramPerCubicMeter(D);
			Enthalpy = Enthalpy.FromJoulePerKilogram(H);
			Pressure = P;
			Entropy = S;
		}

		public void UpdatePT(Pressure P, Temperature T)
		{
			double S = CoolpropJs.Invoke<double>("PropsSI", 'S', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString());
			double D = CoolpropJs.Invoke<double>("PropsSI", 'D', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString());
			double H = CoolpropJs.Invoke<double>("PropsSI", 'H', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString());
			Entropy = SpecificEntropy.FromJoulePerKilogramKelvin(S);
			Density = Density.FromKilogramPerCubicMeter(D);
			Enthalpy = Enthalpy.FromJoulePerKilogram(H);
			Pressure = P;
			Temperature = T;
		}

		public Temperature GetSatTemperature(Pressure P)
		{
			double x = 0;
			double T = CoolpropJs.Invoke<double>("PropsSI", 'T', 'P', P.Pascal, 'Q', x, FluidName.ToString());
			return Temperature.FromKelvins(T);
		}
	}

}
