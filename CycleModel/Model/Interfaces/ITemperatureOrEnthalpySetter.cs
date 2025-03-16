using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CycleCalculator.CycleModel.Model.Interfaces
{
    public interface ITemperatureOrEnthalpySetter
    {
        abstract void CascadeTemperatureAndEnthalpyDownstream();
        abstract void StartHeatBalanceCalculation();
    }
}
