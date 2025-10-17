using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CycleCalculator.CycleModel.Model.Interfaces
{
    internal interface IMassFlowSetter
    {
        abstract void InitializeMassFlow();
        abstract void CascadeMassFlowDownstream();
        abstract void CascadeMassFlowUpstream();
        abstract void StartMassBalanceCalculation();
        abstract void StartHeatBalanceCalculation();
    }
}
