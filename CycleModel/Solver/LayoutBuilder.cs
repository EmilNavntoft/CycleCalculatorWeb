using CycleCalculator.CycleModel.Model;
using CycleCalculator.CycleModel.Model.IO;

namespace CycleCalculator.CycleModel.Solver
{
	public static class LayoutBuilder
	{
		public static List<CycleComponent> CycleComponents = new List<CycleComponent>();

		public static void AddComponent(CycleComponent component) {
			CycleComponents.Add(component);
		}

		public static void AddConnection(CycleComponent component1, CycleComponent component2, PortIdentifier portIdentifier1, PortIdentifier portIdentifier2)
		{
			component1.Ports[portIdentifier1].ConnectTo(component2.Ports[portIdentifier2]);	
		}
		
		public static void RemoveConnection(CycleComponent component1, CycleComponent component2, PortIdentifier portIdentifier1, PortIdentifier portIdentifier2)
		{
			component1.Ports[portIdentifier1].RemoveConnection();
			component2.Ports[portIdentifier2].RemoveConnection();
		}

		public static void RemoveComponent(CycleComponent component)
		{
			CycleComponents.Remove(component);
		}
	}
}
