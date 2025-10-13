using CycleCalculator.CycleModel.Exceptions;
using CycleCalculator.CycleModel.Model;
using CycleCalculator.CycleModel.Model.Interfaces;
using CycleCalculator.CycleModel.Model.IO;
using CycleCalculatorWeb.Utils;
using EngineeringUnits;
using HSG.Numerics;
using System.Diagnostics;

namespace CycleCalculator.CycleModel.Solver
{
    public static class Solver
    {
        private static Random _rng = new Random();

   //     public static List<CycleComponent> Layout1()
   //     {
   //         SimpleCompressor comp1 = new SimpleCompressor("Comp1")
			//{
			//	NominalMassFlow = MassFlow.FromKilogramPerSecond(3),
			//	DischargePressure = Pressure.FromBar(10)
			//};
			//SimpleCompressor comp2 = new SimpleCompressor("Comp2")
			//{
			//	NominalMassFlow = MassFlow.FromKilogramPerSecond(1),
			//	DischargePressure = Pressure.FromBar(5)
			//};
			//SimpleCompressor comp3 = new SimpleCompressor("Comp3")
			//{
			//	NominalMassFlow = MassFlow.FromKilogramPerSecond(5),
			//	DischargePressure = Pressure.FromBar(10)
			//};
			//TeeSection tee1 = new TeeSection("Tee1");
   //         TeeSection tee2 = new TeeSection("Tee2");
   //         TemperatureBoundary tb1 = new TemperatureBoundary("TB1")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(30)
			//};
			//TemperatureBoundary tb2 = new TemperatureBoundary("TB2")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(-8)
			//};
			//PRV PRV1 = new PRV("PRV1")
			//{
			//	OutletPressure = Pressure.FromBar(2)
			//};

   //         comp1.PortB.ConnectTo(tee1.PortB);
   //         tee1.PortA.ConnectTo(comp3.PortB);
   //         tee1.PortC.ConnectTo(tb1.PortA);
   //         tb1.PortB.ConnectTo(PRV1.PortA);
   //         PRV1.PortB.ConnectTo(tb2.PortA);
   //         tb2.PortB.ConnectTo(tee2.PortB);
   //         tee2.PortC.ConnectTo(comp2.PortA);
   //         tee2.PortA.ConnectTo(comp3.PortA);
   //         comp2.PortB.ConnectTo(comp1.PortA);

   //         List<CycleComponent> cycleComponents = new List<CycleComponent>() { comp1, comp2, comp3, tee1, tee2, tb1, tb2, PRV1 };

   //         return cycleComponents;
   //     }

   //     public static List<CycleComponent> Layout2()
   //     {
   //         SimpleCompressor comp1 = new SimpleCompressor("Comp1")
			//{
			//	NominalMassFlow = MassFlow.FromKilogramPerSecond(3),
			//	DischargePressure = Pressure.FromBar(60)
			//};
			//SimpleCompressor comp2 = new SimpleCompressor("Comp2")
			//{
			//	NominalMassFlow = MassFlow.FromKilogramPerSecond(2),
			//	DischargePressure = Pressure.FromBar(26)
			//};
			//TeeSection tee1 = new TeeSection("Tee1");
   //         TeeSection tee2 = new TeeSection("Tee2");
   //         TemperatureBoundary tb1 = new TemperatureBoundary("TB1")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(30)
			//};
			//TemperatureBoundary tb2 = new TemperatureBoundary("TB2")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(-6)
			//};
			//TemperatureBoundary tb3 = new TemperatureBoundary("TB3")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(-25)
			//};
			//PRV PRV1 = new PRV("PRV1")
			//{
			//	OutletPressure = Pressure.FromBar(26)
			//};
			//PRV PRV2 = new PRV("PRV2")
			//{
			//	OutletPressure = Pressure.FromBar(14)
			//};

			//comp1.PortB.ConnectTo(tb1.PortA);
   //         tb1.PortB.ConnectTo(tee1.PortB);
   //         tee1.PortA.ConnectTo(PRV1.PortA);
   //         tee1.PortC.ConnectTo(PRV2.PortA);
   //         PRV1.PortB.ConnectTo(tb2.PortA);
   //         PRV2.PortB.ConnectTo(tb3.PortA);
   //         tb3.PortB.ConnectTo(comp2.PortA);
   //         comp2.PortB.ConnectTo(tee2.PortA);
   //         tb2.PortB.ConnectTo(tee2.PortC);
   //         tee2.PortB.ConnectTo(comp1.PortA);

   //         List<CycleComponent> cycleComponents = new List<CycleComponent>() { comp1, comp2, tee1, tee2, tb1, tb2, tb3, PRV1, PRV2 };

   //         return cycleComponents;
   //     }

   //     public static List<CycleComponent> Layout3()
   //     {
   //         SimpleCompressor comp1 = new SimpleCompressor("Comp1")
			//{
			//	NominalMassFlow = MassFlow.FromKilogramPerSecond(3),
			//	DischargePressure = Pressure.FromBar(60)
			//};
			//SimpleCompressor comp2 = new SimpleCompressor("Comp2")
			//{
			//	NominalMassFlow = MassFlow.FromKilogramPerSecond(1),
			//	DischargePressure = Pressure.FromBar(60)
			//};
			//TeeSection tee1 = new TeeSection("Tee1");
   //         TeeSection tee2 = new TeeSection("Tee2");
   //         TeeSection tee3 = new TeeSection("Tee3");
   //         TeeSection tee4 = new TeeSection("Tee4");
   //         TeeSection tee5 = new TeeSection("Tee5");
   //         TeeSection tee6 = new TeeSection("Tee6");
   //         TeeSection tee7 = new TeeSection("Tee7");
   //         TeeSection tee8 = new TeeSection("Tee8");
   //         TemperatureBoundary tb1 = new TemperatureBoundary("TB1")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(30)
			//};
			//TemperatureBoundary tb2 = new TemperatureBoundary("TB2")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(30)
			//};
			//Pipe pipe1 = new Pipe("Pipe1", 400000);
   //         Pipe pipe2 = new Pipe("Pipe2", 800000);
   //         Pipe pipe3 = new Pipe("Pipe3", 400000);
   //         Pipe pipe4 = new Pipe("Pipe4", 400000);

   //         comp1.PortB.ConnectTo(tb1.PortA);
   //         tb1.PortB.ConnectTo(tee1.PortB);
   //         tee1.PortA.ConnectTo(pipe1.PortA);
   //         tee1.PortC.ConnectTo(tee2.PortB);
   //         tee2.PortA.ConnectTo(pipe2.PortA);
   //         tee2.PortC.ConnectTo(tee3.PortB);
   //         tee3.PortA.ConnectTo(pipe3.PortA);
   //         tee3.PortC.ConnectTo(tee4.PortB);
   //         tee4.PortC.ConnectTo(pipe4.PortA);
   //         pipe1.PortB.ConnectTo(tee5.PortA);
   //         pipe2.PortB.ConnectTo(tee6.PortA);
   //         pipe3.PortB.ConnectTo(tee7.PortA);
   //         pipe4.PortB.ConnectTo(tee8.PortB);
   //         tee5.PortC.ConnectTo(comp1.PortA);
   //         tee5.PortB.ConnectTo(tee6.PortC);
   //         tee6.PortB.ConnectTo(tee7.PortC);
   //         tee7.PortB.ConnectTo(tee8.PortC);
   //         tee8.PortA.ConnectTo(comp2.PortA);
   //         comp2.PortB.ConnectTo(tb2.PortA);
   //         tb2.PortB.ConnectTo(tee4.PortA);

   //         List<CycleComponent> cycleComponents = new List<CycleComponent>() { comp1, comp2, tee1, tee2, tee3, tee4, tee5, tee6, tee7, tee8, tb1, tb2, pipe1, pipe2, pipe3, pipe4 };

   //         return cycleComponents;
   //     }

   //     public static List<CycleComponent> Layout4()
   //     {
   //         SimpleCompressor comp1 = new SimpleCompressor("Comp1")
   //         {
   //             NominalMassFlow = MassFlow.FromKilogramPerSecond(3),
   //             DischargePressure = Pressure.FromBar(60)
   //         };
   //         SimpleCompressor comp2 = new SimpleCompressor("Comp2")
			//{
			//	NominalMassFlow = MassFlow.FromKilogramPerSecond(3),
			//	DischargePressure = Pressure.FromBar(60)
			//};
			//TeeSection tee1 = new TeeSection("Tee1");
   //         TeeSection tee2 = new TeeSection("Tee2");
   //         TeeSection tee3 = new TeeSection("Tee3");
   //         TeeSection tee4 = new TeeSection("Tee4");
   //         TeeSection tee5 = new TeeSection("Tee5");
   //         TeeSection tee6 = new TeeSection("Tee6");
   //         TeeSection tee7 = new TeeSection("Tee7");
   //         TeeSection tee8 = new TeeSection("Tee8");
   //         TemperatureBoundary tb1 = new TemperatureBoundary("TB1")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(30)
			//};
			//TemperatureBoundary tb2 = new TemperatureBoundary("TB2")
			//{
			//	Temperature = Temperature.FromDegreesCelsius(30)
			//};
			//Pipe pipe1 = new Pipe("Pipe1", 100);
   //         Pipe pipe2 = new Pipe("Pipe2", 100);
   //         Pipe pipe3 = new Pipe("Pipe3", 100);
   //         Pipe pipe4 = new Pipe("Pipe4", 100);
   //         Pipe pipe5 = new Pipe("Pipe5", 100);
   //         Pipe pipe6 = new Pipe("Pipe6", 100);
   //         Pipe pipe7 = new Pipe("Pipe7", 100);
   //         Pipe pipe8 = new Pipe("Pipe8", 100);
   //         Pipe pipe9 = new Pipe("Pipe9", 100);
   //         Pipe pipe10 = new Pipe("Pipe10", 100);

   //         comp1.PortB.ConnectTo(tb1.PortA);
   //         tb1.PortB.ConnectTo(tee1.PortB);
   //         tee1.PortA.ConnectTo(pipe1.PortA);
   //         tee1.PortC.ConnectTo(pipe5.PortA);
   //         pipe5.PortB.ConnectTo(tee2.PortB);
   //         tee2.PortA.ConnectTo(pipe2.PortA);
   //         tee2.PortC.ConnectTo(pipe6.PortA);
   //         pipe6.PortB.ConnectTo(tee3.PortB);
   //         tee3.PortA.ConnectTo(pipe3.PortA);
   //         tee3.PortC.ConnectTo(pipe7.PortA);
   //         pipe7.PortB.ConnectTo(tee4.PortB);
   //         tee4.PortC.ConnectTo(pipe4.PortA);
   //         pipe1.PortB.ConnectTo(tee5.PortA);
   //         pipe2.PortB.ConnectTo(tee6.PortA);
   //         pipe3.PortB.ConnectTo(tee7.PortA);
   //         pipe4.PortB.ConnectTo(tee8.PortB);
   //         tee5.PortC.ConnectTo(comp1.PortA);
   //         tee5.PortB.ConnectTo(pipe8.PortA);
   //         pipe8.PortB.ConnectTo(tee6.PortC);
   //         tee6.PortB.ConnectTo(pipe9.PortA);
   //         pipe9.PortB.ConnectTo(tee7.PortC);
   //         tee7.PortB.ConnectTo(pipe10.PortA);
   //         pipe10.PortB.ConnectTo(tee8.PortC);
   //         tee8.PortA.ConnectTo(comp2.PortA);
   //         comp2.PortB.ConnectTo(tb2.PortA);
   //         tb2.PortB.ConnectTo(tee4.PortA);

   //         List<CycleComponent> cycleComponents = new List<CycleComponent>() { comp1, comp2, tee1, tee2, tee3, tee4, tee5, tee6, tee7, tee8, tb1, tb2, pipe1, pipe2, pipe3, pipe4, pipe5, pipe6, pipe7, pipe8, pipe9, pipe10};

   //         return cycleComponents;
   //     }

        private static List<CycleComponent> _cycleComponents;

        public static void Reset()
        {
            if (_cycleComponents is null || _cycleComponents.Count == 0)
            {
                return;
            }
			foreach (CycleComponent component in _cycleComponents)
			{
				foreach (Port port in component.Ports.Values)
				{
					port.Pressure = Pressure.NaN;
					port.Temperature = Temperature.NaN;
					port.Enthalpy = Enthalpy.NaN;
					port.MassFlow = MassFlow.NaN;
				}
			}
		}

        public static void Solve()
        {
            _cycleComponents = LayoutBuilder.CycleComponents;
            List<IPressureSetter> pressureSetters = _cycleComponents.FindAll(c => c is IPressureSetter).Cast<IPressureSetter>().ToList();
            List<IMassFlowSetter> massFlowSetters = _cycleComponents.FindAll(c => c is IMassFlowSetter).Cast<IMassFlowSetter>().ToList();
            List<ITemperatureOrEnthalpySetter> temperatureOrEnthalpySetters = _cycleComponents.FindAll(c => c is ITemperatureOrEnthalpySetter).Cast<ITemperatureOrEnthalpySetter>().ToList();
            List<IHeatExchanger> heatExchangers = _cycleComponents.FindAll(c => c is IHeatExchanger).Cast<IHeatExchanger>().ToList();
            List<TeeSection> tees = _cycleComponents.FindAll(c => c is TeeSection).Cast<TeeSection>().ToList();
            List<Pipe> pipes = _cycleComponents.FindAll(c => c is Pipe).Cast<Pipe>().ToList();
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            try
            {
				CascadeKnownPressures(pressureSetters);
				CascadeKnownMassflows(massFlowSetters);
				PerformMassBalanceCalculations(massFlowSetters);
				CascadeInitialTemperaturesAndEnthalpies(temperatureOrEnthalpySetters);
				PerformPressureDropCalculations(pressureSetters);
				PerformHeatBalanceCalculations(temperatureOrEnthalpySetters);
				PerformHeatExchangerCalculattions(heatExchangers);
				
				CascadeKnownPressures(pressureSetters);
				CascadeKnownMassflows(massFlowSetters);
				PerformMassBalanceCalculations(massFlowSetters);
				CascadeInitialTemperaturesAndEnthalpies(temperatureOrEnthalpySetters);
				PerformPressureDropCalculations(pressureSetters);
				PerformHeatBalanceCalculations(temperatureOrEnthalpySetters);
				PerformHeatExchangerCalculattions(heatExchangers);
				
				CascadeKnownPressures(pressureSetters);
				CascadeKnownMassflows(massFlowSetters);
				PerformMassBalanceCalculations(massFlowSetters);
				CascadeInitialTemperaturesAndEnthalpies(temperatureOrEnthalpySetters);
				PerformPressureDropCalculations(pressureSetters);
				PerformHeatBalanceCalculations(temperatureOrEnthalpySetters);
				PerformHeatExchangerCalculattions(heatExchangers);
				
				CascadeKnownPressures(pressureSetters);
				CascadeKnownMassflows(massFlowSetters);
				PerformMassBalanceCalculations(massFlowSetters);
				CascadeInitialTemperaturesAndEnthalpies(temperatureOrEnthalpySetters);
				PerformPressureDropCalculations(pressureSetters);
				PerformHeatBalanceCalculations(temperatureOrEnthalpySetters);
				PerformHeatExchangerCalculattions(heatExchangers);
				
				CascadeKnownPressures(pressureSetters);
				CascadeKnownMassflows(massFlowSetters);
				PerformMassBalanceCalculations(massFlowSetters);
				CascadeInitialTemperaturesAndEnthalpies(temperatureOrEnthalpySetters);
				PerformPressureDropCalculations(pressureSetters);
				PerformHeatBalanceCalculations(temperatureOrEnthalpySetters);
				PerformHeatExchangerCalculattions(heatExchangers);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
				JsLogger.Log(ex.Message);
				JsLogger.Log(ex.StackTrace);
            }

            stopwatch.Stop();
            Debug.WriteLine(stopwatch.ElapsedMilliseconds);

            return;
        }

        private static void CascadeKnownPressures(List<IPressureSetter> pressureSetters)
        {
            //Cascade known values
            foreach (IPressureSetter pressureSetter in pressureSetters)
            {
                pressureSetter.InitializePressure();
            }
            foreach (IPressureSetter pressureSetter in pressureSetters)
            {
                pressureSetter.CascadePressureDownstream();
            }
        }

        private static void PerformHeatBalanceCalculations(List<ITemperatureOrEnthalpySetter> temperatureOrEnthalpySetters)
        {
            //Calculate heat balance
            foreach (ITemperatureOrEnthalpySetter temperatureOrEnthalpySetter in temperatureOrEnthalpySetters)
            {
                temperatureOrEnthalpySetter.StartHeatBalanceCalculation();
            }
        }

        private static void PerformHeatExchangerCalculattions(List<IHeatExchanger> heatExchangers)
        {
	        //Calculate heat exchangers
	        foreach (IHeatExchanger heatExchanger in heatExchangers)
	        {
		        heatExchanger.CalculateHeatExchanger();
	        }
        }

        private static void CascadeInitialTemperaturesAndEnthalpies(List<ITemperatureOrEnthalpySetter> temperatureOrEnthalpySetters)
        {
            //Cascade known values
            foreach (ITemperatureOrEnthalpySetter temperatureOrEnthalpySetter in temperatureOrEnthalpySetters)
            {
                temperatureOrEnthalpySetter.CascadeTemperatureAndEnthalpyDownstream();
            }
        }

        private static void PerformPressureDropCalculations(List<IPressureSetter> pressureSetters)
        {
            //Calculate pressure drops
            foreach (IPressureSetter pressureSetter in pressureSetters)
            {
                pressureSetter.StartPressureDropCalculation();
            }
        }

        private static void PerformMassBalanceCalculations(List<IMassFlowSetter> massFlowSetters)
        {
            //Calculate mass balance
            foreach (IMassFlowSetter massFlowSetter in massFlowSetters)
            {
                massFlowSetter.StartMassBalanceCalculation();
            }
        }

        private static void CascadeKnownMassflows(List<IMassFlowSetter> massFlowSetters)
        {
            //Cascade known values
            foreach (IMassFlowSetter massFlowSetter in massFlowSetters)
            {
                massFlowSetter.InitializeMassFlow();
            }
            foreach (IMassFlowSetter massFlowSetter in massFlowSetters)
            {
                massFlowSetter.CascadeMassFlowUpstream();
                massFlowSetter.CascadeMassFlowDownstream();
            }
        }
    }
}
