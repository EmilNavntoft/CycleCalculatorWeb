using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CycleCalculator.CycleModel.Exceptions
{
	public class SolverException(string message) : Exception(message)
	{
	}
}
