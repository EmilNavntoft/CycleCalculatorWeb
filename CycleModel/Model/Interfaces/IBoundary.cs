using EngineeringUnits;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CycleCalculator.CycleModel.Model.Interfaces
{
	public interface IBoundary
	{
		[Editable(false)]
		public Power HeatFlowExchanged { get; set; }
	}
}
