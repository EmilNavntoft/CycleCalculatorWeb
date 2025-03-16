using EngineeringUnits;
using Microsoft.JSInterop;
using System.Diagnostics;

namespace CycleCalculatorWeb.CoolpropJsInterop
{
    public class CoolpropJsFluid
    {
		IJSInProcessObjectReference _CoolpropJS { get; set; }
		private Density _Density = Density.Zero;
		public Density Density { get => _Density; set => _Density = value; }
		private Enthalpy enthalpy = Enthalpy.Zero;
		public Enthalpy Enthalpy { get => enthalpy; set => enthalpy = value; }
		private Temperature _Temperature = Temperature.Zero;
		public Temperature Temperature { get => _Temperature; set => _Temperature = value; }
		private Pressure _Pressure = Pressure.Zero;
		public Pressure Pressure { get => _Pressure; set => _Pressure = value; }
		private SpecificEntropy _Entropy = SpecificEntropy.Zero;
		public SpecificEntropy Entropy { get => _Entropy; set => _Entropy = value; }

		private FluidNames _FluidName = FluidNames.Ammonia;
		public FluidNames FluidName { get => _FluidName; set => _FluidName = value; }

        public CoolpropJsFluid(IJSInProcessObjectReference coolprop)
        {
            _CoolpropJS = coolprop;
        }

        public void UpdatePH(Pressure P, Enthalpy H)
        {
			double T = _CoolpropJS.Invoke<double>("PropsSI", 'T', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString());
			double D = _CoolpropJS.Invoke<double>("PropsSI", 'D', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString());
			double S = _CoolpropJS.Invoke<double>("PropsSI", 'S', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString());
			Temperature = Temperature.FromKelvin(T);
			Density = Density.FromKilogramPerCubicMeter(D);
			Entropy = SpecificEntropy.FromJoulePerKilogramKelvin(S);
			Pressure = P;
			Enthalpy = H;
		}

		public void UpdatePS(Pressure P, SpecificEntropy S)
		{
			double T = _CoolpropJS.Invoke<double>("PropsSI", 'T', 'P', P.Pascal, 'S', S.JoulePerKilogramKelvin, FluidName.ToString());
			double D = _CoolpropJS.Invoke<double>("PropsSI", 'D', 'P', P.Pascal, 'S', S.JoulePerKilogramKelvin, FluidName.ToString());
			double H = _CoolpropJS.Invoke<double>("PropsSI", 'H', 'P', P.Pascal, 'S', S.JoulePerKilogramKelvin, FluidName.ToString());
			Temperature = Temperature.FromKelvin(T);
			Density = Density.FromKilogramPerCubicMeter(D);
			Enthalpy = Enthalpy.FromJoulePerKilogram(H);
			Pressure = P;
			Entropy = S;
		}

		public void UpdatePT(Pressure P, Temperature T)
		{
			double S = _CoolpropJS.Invoke<double>("PropsSI", 'S', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString());
			double D = _CoolpropJS.Invoke<double>("PropsSI", 'D', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString());
			double H = _CoolpropJS.Invoke<double>("PropsSI", 'H', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString());
			Entropy = SpecificEntropy.FromJoulePerKilogramKelvin(S);
			Density = Density.FromKilogramPerCubicMeter(D);
			Enthalpy = Enthalpy.FromJoulePerKilogram(H);
			Pressure = P;
			Temperature = T;
		}

		public Temperature GetSatTemperature(Pressure P)
		{
			double T = _CoolpropJS.Invoke<double>("PropsSI", 'T', 'P', P.Pascal, 'X', 0, FluidName.ToString());
			return Temperature.FromKelvins(T);
		}
	}

}
