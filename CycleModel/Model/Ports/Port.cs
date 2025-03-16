using CycleCalculator.CycleModel.Exceptions;
using EngineeringUnits;
using SharpFluids;

namespace CycleCalculator.CycleModel.Model.IO
{
    public class Port
    {
        public PortIdentifier Identifier { get; private set; }

        public int PIdent;
        public int MdotIdent;

        private Pressure _pressure = Pressure.NaN;
        public Pressure Pressure
        {
            get
            {
                return _pressure;
            }
            set
            {
                _pressure = value;
            }
        }

        private Temperature _temperature = Temperature.NaN;
        public Temperature Temperature
        {
            get
            {
                return _temperature;
            }
            set
            {
                _temperature = value;
            }
        }

        private MassFlow _massFlow = MassFlow.NaN;
        public MassFlow MassFlow
        {
            get
            {
                return _massFlow;
            }
            set
            {
                _massFlow = value;
            }
        }

        private Enthalpy _enthalpy = Enthalpy.NaN;
        public Enthalpy Enthalpy
        {
            get
            {
                return _enthalpy;
            }
            set
            {
                _enthalpy = value;
            }
        }

        private Port? _Connection;
        public Port Connection { 
            get
            {
                return _Connection;
            }
            private set
            {
                _Connection = value;
            }
        }
        public CycleComponent Component { get; private set; }
        public bool IsFixedMassFlow = false;

        public Port(PortIdentifier identifier, CycleComponent component)
        {
            Identifier = identifier;
            Component = component;
        }

        public void ConnectTo(Port connection)
        {
            Connection = connection;
            connection.Connection = this;
        }

		public void RemoveConnection()
		{
			Connection = null;
		}

		public void ReceiveState()
        {
            Pressure = Connection.Pressure;
            Temperature = Connection.Temperature;
            MassFlow = MassFlow.Zero - Connection.MassFlow;
            Enthalpy = Connection.Enthalpy;
        }
    }
}
