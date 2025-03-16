
namespace CycleCalculator.CycleModel.Model.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ComponentParameter : Attribute
	{
		public string Name;
		public string Unit;

		public ComponentParameter(string name)
		{
			Name = name;
			Unit = "";
		}

		public ComponentParameter(string name, string unit)
		{
			Name = name;
			Unit = unit;
		}
	}
}
