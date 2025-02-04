using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CycleCalculatorWeb.CycleModel.Model.Interfaces
{
    public interface IPressureSetter
    {
        abstract void InitializePressure();
        abstract void CascadePressureDownstream();
        abstract void StartPressureDropCalculation();
    }
}
