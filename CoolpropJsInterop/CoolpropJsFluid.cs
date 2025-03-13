using EngineeringUnits;
using Microsoft.JSInterop;

namespace CycleCalculatorWeb.CoolpropJsInterop
{
    public class CoolpropJsFluid
    {
		IJSObjectReference _CoolpropJS { get; set; }
		private Density _Density = Density.Zero;
		Density Density { get => _Density; set => _Density = value; }
		private Enthalpy enthalpy = Enthalpy.Zero;
		Enthalpy Enthalpy { get => enthalpy; set => enthalpy = value; }
		private Temperature _Temperature = Temperature.Zero;
		Temperature Temperature { get => _Temperature; set => _Temperature = value; }
		private Pressure _Pressure = Pressure.Zero;
		Pressure Pressure { get => _Pressure; set => _Pressure = value; }
		private Entropy _Entropy = Entropy.Zero;
		Entropy Entropy { get => _Entropy; set => _Entropy = value; }

		private FluidNames _FluidName = FluidNames.Ammonia;


		FluidNames FluidName { get => _FluidName; set => _FluidName = value; }

        public CoolpropJsFluid(IJSObjectReference coolprop)
        {
            _CoolpropJS = coolprop;
        }

        public void UpdatePH(Pressure P, Enthalpy H)
        {
            double T = _CoolpropJS.InvokeAsync<double>("PropsSI", 'T', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString()).Result;
			double D = _CoolpropJS.InvokeAsync<double>("PropsSI", 'D', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString()).Result;
			double S = _CoolpropJS.InvokeAsync<double>("PropsSI", 'S', 'P', P.Pascal, 'H', H.JoulePerKilogram, FluidName.ToString()).Result;
			Temperature = Temperature.FromKelvin(T);
			Density = Density.FromKilogramPerCubicMeter(D);
			Entropy = Entropy.FromJoulePerKelvin(S);
			Pressure = P;
			Enthalpy = H;
		}

		public void UpdatePS(Pressure P, Entropy S)
		{
			double T = _CoolpropJS.InvokeAsync<double>("PropsSI", 'T', 'P', P.Pascal, 'S', S.JoulePerKelvin, FluidName.ToString()).Result;
			double D = _CoolpropJS.InvokeAsync<double>("PropsSI", 'D', 'P', P.Pascal, 'S', S.JoulePerKelvin, FluidName.ToString()).Result;
			double H = _CoolpropJS.InvokeAsync<double>("PropsSI", 'H', 'P', P.Pascal, 'S', S.JoulePerKelvin, FluidName.ToString()).Result;
			Temperature = Temperature.FromKelvin(T);
			Density = Density.FromKilogramPerCubicMeter(D);
			Enthalpy = Enthalpy.FromJoulePerKilogram(S);
			Pressure = P;
			Entropy = S;
		}

		public void UpdatePT(Pressure P, Temperature T)
		{
			double S = _CoolpropJS.InvokeAsync<double>("PropsSI", 'S', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString()).Result;
			double D = _CoolpropJS.InvokeAsync<double>("PropsSI", 'D', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString()).Result;
			double H = _CoolpropJS.InvokeAsync<double>("PropsSI", 'H', 'P', P.Pascal, 'T', T.Kelvin, FluidName.ToString()).Result;
			Entropy = Entropy.FromJoulePerKelvin(S);
			Density = Density.FromKilogramPerCubicMeter(D);
			Enthalpy = Enthalpy.FromJoulePerKilogram(S);
			Pressure = P;
			Temperature = T;
		}
	}

}
