using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegExp
{
    internal class NfaToDfaHelper
    {
        /// <summary>
        /// array of of DfaStateRecord
        /// </summary>
        private Hashtable m_hashStateTable = new Hashtable();


        /// <summary>
        /// A nested calss.
        /// A row with three feilds. to store DFA states with two other attributes.
        /// </summary>
        private class DfaStateRecord
        {
            /// <summary>
            /// set of NFA state from which the DFA state was created
            /// </summary>
            public Set SetEclosure = null;
            /// <summary>
            /// a flag to indicate wheather or not this DFA state has been processed.
            /// See the Subset Construction algorithom for detail
            /// </summary>
            public bool Marked = false;

        }

        public NfaToDfaHelper()
        {

        }
        /// <summary>
        /// Simply adds newly created DFA state to the table
        /// </summary>
        /// <param name="stateDfa">the newly created DFA state</param>
        /// <param name="setEclosure">set of Eclosure that was used to create the DFA state</param>
        public void AddDfaState(State stateDfa, Set setEclosure)
        {
            DfaStateRecord stateRecord = new DfaStateRecord();
            stateRecord.SetEclosure = setEclosure;

            m_hashStateTable[stateDfa] = stateRecord;
        }

        /// <summary>
        /// finds a DFA state using a set of Eclosure state as search criteria.
        /// because all DFAs are constructed from a set of NFA state
        /// </summary>
        /// <param name="setNfaState">set of Eclosure state as search criteria</param>
        /// <returns>if found, returns the DFA state record, or returns null</returns>
        public State FindDfaStateByEclosure(Set setEclosure)
        {
            DfaStateRecord stateRecord = null;

            foreach (DictionaryEntry de in m_hashStateTable)
            {
                stateRecord = (DfaStateRecord)de.Value;
                if (stateRecord.SetEclosure.IsEqual(setEclosure) == true)
                {
                    return (State)de.Key;
                }
            }
            return null;

        }  // end of FindDfaStateByEclosure method

        public Set GetEclosureByDfaState(State state)
        {
            DfaStateRecord dsr = (DfaStateRecord)m_hashStateTable[state];

            if (dsr != null)
            {
                return dsr.SetEclosure;
            }
            return null;
        }
        public State GetNextUnmarkedDfaState()
        {
            DfaStateRecord stateRecord = null;

            foreach (DictionaryEntry de in m_hashStateTable)
            {
                stateRecord = (DfaStateRecord)de.Value;

                if (stateRecord.Marked == false)
                {
                    return (State)de.Key;
                }

            }

            return null;
        }
        public void Mark(State stateT)
        {
            DfaStateRecord stateRecord = (DfaStateRecord)m_hashStateTable[stateT];
            stateRecord.Marked = true;
        }

        /// <summary>
        /// checks to see if a set contains any state that is an accepting state.
        /// used in NFA to DFA conversion
        /// </summary>
        /// <param name="setState">set of state</param>
        /// <returns>true if set contains at least one accepting state, else false</returns>

        public Set GetAllDfaState()
        {
            Set setState = new Set();

            foreach (object objKey in m_hashStateTable.Keys)
            {
                setState.AddElement(objKey);
            }

            return setState;
        }

    }
}
