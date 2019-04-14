using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegExp
{
    internal class State
    {

        // the one and only Id tracker
        static private int m_nStateId = 0;

        // Set of transition objects
        public Map m_map = new Map();

        private int m_nId = 0;

        public State()
        {
            m_nId = m_nStateId++;
        }

        public int Id
        {
            get
            {
                return m_nId;
            }
        }

        public void AddTransition(string sInputSymbol, State stateTo)
        {
            m_map.Add(sInputSymbol, stateTo);


        }

        public State[] GetTransitions(string sInputSymbol)
        {
            if (m_map.Contains(sInputSymbol) == true)
            {
                Set set = (Set)m_map[sInputSymbol];
                return (State[])set.ToArray(typeof(State));
            }
            return null;
        }

        public State GetSingleTransition(string sInputSymbol)
        {
            if (m_map.Contains(sInputSymbol) == true)
            {
                Set set = (Set)m_map[sInputSymbol];
                return (State)set[0];
            }
            return null;
        }
        public void RemoveTransition(string sInputSymbol)
        {
            m_map.Remove(sInputSymbol);
        }

        public int ReplaceTransitionState(State stateOld, State stateNew)
        {
            int nReplacementCount = 0;
            Set setTrans = null;
            foreach (DictionaryEntry de in m_map)
            {
                setTrans = (Set)de.Value;
                if (setTrans.ElementExist(stateOld) == true)
                {
                    setTrans.RemoveElement(stateOld);
                    setTrans.AddElement(stateNew);
                    nReplacementCount++;
                }
            }
            return nReplacementCount;
        }

        private bool m_bAcceptingState = false;

        public bool AcceptingState
        {
            get { return m_bAcceptingState; }
            set { m_bAcceptingState = value; }
        }


        public bool IsDeadState()
        {
            if (m_bAcceptingState)
            {
                return false;
            }
            if (m_map.Count == 0)
            {
                return false;
            }
            Set setToState = null;
            foreach (DictionaryEntry de in m_map)
            {
                setToState = (Set)de.Value;

                State state = null;
                foreach (object objState in setToState)  // in a DFA, it should only iterate once
                {
                    state = (State)objState;
                    if (state.Equals(this) == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static public void ResetCounter()
        {
            m_nStateId = 0;
        }
        public ICollection GetAllKeys()
        {
            return m_map.Keys;
        }
        public override string ToString()
        {
            string s = "s" + this.Id.ToString();
            if (this.AcceptingState)
            {
                s = "{" + s + "}";
            }
            return s;
        }


    }
}
