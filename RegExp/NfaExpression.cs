using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegExp
{
    class NfaExpression
    {
        public NfaExpression()
        {
            m_stateStart = new State();
            m_stateFinal = new State();

        }
        public NfaExpression(State stateFrom, State stateTo)
        {
            m_stateStart = stateFrom;
            m_stateFinal = stateTo;

        }

        State m_stateStart = null;
        State m_stateFinal = null;

        public State StartState()
        {
            return m_stateStart;
        }

        public State FinalState()
        {
            return m_stateFinal;
        }


    }
}
