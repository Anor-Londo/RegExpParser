using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Data;
using System.Diagnostics;

namespace RegExp
{

    /// <summary>
    /// the regular expression recognizer class
    /// </summary>
    public class RegEx
    {

        static private RegExValidator m_reValidator = new RegExValidator();

        private int m_nLastErrorIndex = -1;

        
        private int m_nLastErrorLength = -1;
        
        private ErrorCode m_LastErrorCode = ErrorCode.ERR_SUCCESS;
        
        private bool m_bMatchAtStart = false;
        
        private bool m_bMatchAtEnd = false;
        
        private bool m_bGreedy = true;
        
        private State m_stateStartDfaM = null;

        public RegEx()
        {

        }


       
        private string ConvertToPostfix(string sInfixPattern)
        {
            Stack stackOperator = new Stack();
            Queue queuePostfix = new Queue();
            bool bEscape = false;

            for (int i = 0; i < sInfixPattern.Length; i++)
            {
                char ch = sInfixPattern[i];


                if (bEscape == false && ch == MetaSymbol.ESCAPE)
                {
                    queuePostfix.Enqueue(ch);
                    bEscape = true;
                    continue;
                }

                if (bEscape == true)
                {
                    queuePostfix.Enqueue(ch);
                    bEscape = false;
                    continue;
                }
                switch (ch)
                {
                    case MetaSymbol.OPEN_PREN:
                        stackOperator.Push(ch);
                        break;
                    case MetaSymbol.CLOSE_PREN:
                        while ((char)stackOperator.Peek() != MetaSymbol.OPEN_PREN)
                        {
                            queuePostfix.Enqueue(stackOperator.Pop());
                        }
                        stackOperator.Pop();  // pop the '('

                        break;
                    default:
                        while (stackOperator.Count > 0)
                        {
                            char chPeeked = (char)stackOperator.Peek();

                            int nPriorityPeek = GetOperatorPriority(chPeeked);
                            int nPriorityCurr = GetOperatorPriority(ch);

                            if (nPriorityPeek >= nPriorityCurr)
                            {
                                queuePostfix.Enqueue(stackOperator.Pop());
                            }
                            else
                            {
                                break;
                            }
                        }
                        stackOperator.Push(ch);
                        break;
                }

            }  // end of for..loop

            while (stackOperator.Count > 0)
            {
                queuePostfix.Enqueue((char)stackOperator.Pop());
            }
            StringBuilder sb = new StringBuilder(1024);
            while (queuePostfix.Count > 0)
            {
                sb.Append((char)queuePostfix.Dequeue());
            }


            return sb.ToString();
        }

       
        private int GetOperatorPriority(char chOpt)
        {
            switch (chOpt)
            {
                case MetaSymbol.OPEN_PREN:
                    return 0;
                case MetaSymbol.ALTERNATE:
                    return 1;
                case MetaSymbol.CONCANATE:
                    return 2;
                case MetaSymbol.ZERO_OR_ONE:
                case MetaSymbol.ZERO_OR_MORE:
                case MetaSymbol.ONE_OR_MORE:
                    return 3;
                case MetaSymbol.COMPLEMENT:
                    return 4;
                default:
                    return 5;

            }
        }


        public ErrorCode CompileWithStats(string sPattern, StringBuilder sbStats)
        {

            if (sbStats == null)
            {
                return Compile(sPattern);  // no statistics required
            }

            State.ResetCounter();

            int nLineLength = 0;

            ValidationInfo vi = m_reValidator.Validate(sPattern);

            UpdateValidationInfo(vi);

            if (vi.ErrorCode != ErrorCode.ERR_SUCCESS)
            {
                return vi.ErrorCode;
            }

            string sRegExPostfix = ConvertToPostfix(vi.FormattedString);

            sbStats.AppendLine("Original pattern:\t\t" + sPattern);
            sbStats.AppendLine("Pattern after formatting:\t" + vi.FormattedString);
            sbStats.AppendLine("Pattern after postfix:\t\t" + sRegExPostfix);
            sbStats.AppendLine();

            State stateStartNfa = CreateNfa(sRegExPostfix);
            sbStats.AppendLine();
            sbStats.AppendLine("NFA Table:");
            nLineLength = GetSerializedFsa(stateStartNfa, sbStats);
            sbStats.AppendFormat(("").PadRight(nLineLength, '*'));
            sbStats.AppendLine();

            State.ResetCounter();
            State stateStartDfa = ConvertToDfa(stateStartNfa);
            sbStats.AppendLine();
            sbStats.AppendLine("DFA Table:");
            nLineLength = GetSerializedFsa(stateStartDfa, sbStats);
            sbStats.AppendFormat(("").PadRight(nLineLength, '*'));
            sbStats.AppendLine();

            State stateStartDfaM = ReduceDfa(stateStartDfa);
            m_stateStartDfaM = stateStartDfaM;
            sbStats.AppendLine();
            sbStats.AppendLine("DFA M' Table:");
            nLineLength = GetSerializedFsa(stateStartDfaM, sbStats);
            sbStats.AppendFormat(("").PadRight(nLineLength, '*'));
            sbStats.AppendLine();

            return ErrorCode.ERR_SUCCESS;


        }


        public ErrorCode Compile(string sPattern)
        {
            ValidationInfo vi = m_reValidator.Validate(sPattern);

            UpdateValidationInfo(vi);

            if (vi.ErrorCode != ErrorCode.ERR_SUCCESS)
            {
                return vi.ErrorCode;
            }


            State.ResetCounter();
            string sRegExConcat = vi.FormattedString;

            string sRegExPostfix = ConvertToPostfix(sRegExConcat);

            State stateStartNfa = CreateNfa(sRegExPostfix);

            State.ResetCounter();
            State stateStartDfa = ConvertToDfa(stateStartNfa);
            m_stateStartDfaM = stateStartDfa;

            m_stateStartDfaM = ReduceDfa(stateStartDfa);

            return ErrorCode.ERR_SUCCESS;

        }

       
        private Set Eclosure(State stateStart)
        {
            Set setProcessed = new Set();
            Set setUnprocessed = new Set();

            setUnprocessed.AddElement(stateStart);

            while (setUnprocessed.Count > 0)
            {
                State state = (State)setUnprocessed[0];
                State[] arrTrans = state.GetTransitions(MetaSymbol.EPSILON);
                setProcessed.AddElement(state);
                setUnprocessed.RemoveElement(state);

                if (arrTrans != null)
                {
                    foreach (State stateEpsilon in arrTrans)
                    {
                        if (!setProcessed.ElementExist(stateEpsilon))
                        {
                            setUnprocessed.AddElement(stateEpsilon);
                        }
                    }
                }


            }

            return setProcessed;

        }

       
        private Set Eclosure(Set setState)
        {
            Set setAllEclosure = new Set();
            State state = null;
            foreach (object obj in setState)
            {
                state = (State)obj;

                Set setEclosure = Eclosure(state);
                setAllEclosure.Union(setEclosure);
            }
            return setAllEclosure;
        }

       
        private Set Move(Set setState, string sInputSymbol)
        {
            Set set = new Set();
            State state = null;
            foreach (object obj in setState)
            {
                state = (State)obj;
                Set setMove = Move(state, sInputSymbol);
                set.Union(setMove);
            }
            return set;
        }

       
        private Set Move(State state, string sInputSymbol)
        {
            Set set = new Set();

            State[] arrTrans = state.GetTransitions(sInputSymbol);

            if (arrTrans != null)
            {
                set.AddElementRange(arrTrans);
            }

            return set;

        }

        
        private State CreateNfa(string sRegExPosfix)
        {
            Stack stackNfa = new Stack();
            NfaExpression expr = null;
            NfaExpression exprA = null;
            NfaExpression exprB = null;
            NfaExpression exprNew = null;
            bool bEscape = false;

            foreach (char ch in sRegExPosfix)
            {
                if (bEscape == false && ch == MetaSymbol.ESCAPE)
                {
                    bEscape = true;
                    continue;
                }

                if (bEscape == true)
                {
                    exprNew = new NfaExpression();
                    exprNew.StartState().AddTransition(ch.ToString(), exprNew.FinalState());

                    stackNfa.Push(exprNew);

                    bEscape = false;
                    continue;
                }

                switch (ch)
                {
                    case MetaSymbol.ZERO_OR_MORE:  // A*  Kleene closure

                        exprA = (NfaExpression)stackNfa.Pop();
                        exprNew = new NfaExpression();

                        exprA.FinalState().AddTransition(MetaSymbol.EPSILON, exprA.StartState());
                        exprA.FinalState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());

                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprA.StartState());
                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());

                        stackNfa.Push(exprNew);

                        break;
                    case MetaSymbol.ALTERNATE:  // A|B
                        exprB = (NfaExpression)stackNfa.Pop();
                        exprA = (NfaExpression)stackNfa.Pop();

                        exprNew = new NfaExpression();

                        exprA.FinalState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());
                        exprB.FinalState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());

                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprA.StartState());
                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprB.StartState());

                        stackNfa.Push(exprNew);

                        break;

                    case MetaSymbol.CONCANATE:  // AB
                        exprB = (NfaExpression)stackNfa.Pop();
                        exprA = (NfaExpression)stackNfa.Pop();

                        exprA.FinalState().AddTransition(MetaSymbol.EPSILON, exprB.StartState());

                        exprNew = new NfaExpression(exprA.StartState(), exprB.FinalState());
                        stackNfa.Push(exprNew);

                        break;

                    case MetaSymbol.ONE_OR_MORE:  // A+ => AA* => A.A*

                        exprA = (NfaExpression)stackNfa.Pop();
                        exprNew = new NfaExpression();

                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprA.StartState());
                        exprNew.FinalState().AddTransition(MetaSymbol.EPSILON, exprA.StartState());
                        exprA.FinalState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());

                        stackNfa.Push(exprNew);

                        break;
                    case MetaSymbol.ZERO_OR_ONE:  // A? => A|empty  
                        exprA = (NfaExpression)stackNfa.Pop();
                        exprNew = new NfaExpression();

                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprA.StartState());
                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());
                        exprA.FinalState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());

                        stackNfa.Push(exprNew);

                        break;
                    case MetaSymbol.ANY_ONE_CHAR:
                        exprNew = new NfaExpression();
                        exprNew.StartState().AddTransition(MetaSymbol.ANY_ONE_CHAR_TRANS, exprNew.FinalState());
                        stackNfa.Push(exprNew);
                        break;

                    case MetaSymbol.COMPLEMENT:  // ^ 

                        exprA = (NfaExpression)stackNfa.Pop();

                        NfaExpression exprDummy = new NfaExpression();
                        exprDummy.StartState().AddTransition(MetaSymbol.DUMMY, exprDummy.FinalState());

                        exprA.FinalState().AddTransition(MetaSymbol.EPSILON, exprDummy.StartState());

                        NfaExpression exprAny = new NfaExpression();
                        exprAny.StartState().AddTransition(MetaSymbol.ANY_ONE_CHAR_TRANS, exprAny.FinalState());


                        exprNew = new NfaExpression();
                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprA.StartState());
                        exprNew.StartState().AddTransition(MetaSymbol.EPSILON, exprAny.StartState());

                        exprAny.FinalState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());
                        exprDummy.FinalState().AddTransition(MetaSymbol.EPSILON, exprNew.FinalState());

                        stackNfa.Push(exprNew);

                        break;
                    default:
                        exprNew = new NfaExpression();
                        exprNew.StartState().AddTransition(ch.ToString(), exprNew.FinalState());

                        stackNfa.Push(exprNew);

                        break;

                } 


            }  

            Debug.Assert(stackNfa.Count == 1);
            expr = (NfaExpression)stackNfa.Pop();  // pop the very last one. THERE SHOULD ONLY BE ONE LEFT AT THIS POINT
            expr.FinalState().AcceptingState = true;  // the very last state is the accepting state of the NFA

            return expr.StartState();  // retun the start state of NFA

        } 

       
        private State ConvertToDfa(State stateStartNfa)
        {
            Set setAllInput = new Set();
            Set setAllState = new Set();

            GetAllStateAndInput(stateStartNfa, setAllState, setAllInput);
            setAllInput.RemoveElement(MetaSymbol.EPSILON);

            NfaToDfaHelper helper = new NfaToDfaHelper();
            Set setMove = null;
            Set setEclosure = null;

            // first, we get Eclosure of the start state of NFA ( just following the algoritham)
            setEclosure = Eclosure(stateStartNfa);
            State stateStartDfa = new State();  

            
            if (IsAcceptingGroup(setEclosure) == true)
            {
                stateStartDfa.AcceptingState = true;
            }

            helper.AddDfaState(stateStartDfa, setEclosure);

            string sInputSymbol = String.Empty; // dummy


            State stateT = null;
            Set setT = null;
            State stateU = null;

            while ((stateT = helper.GetNextUnmarkedDfaState()) != null)
            {
                helper.Mark(stateT);   // flag it to indicate that we have processed this state.

               
                setT = helper.GetEclosureByDfaState(stateT);

                foreach (object obj in setAllInput)
                {
                    sInputSymbol = obj.ToString();

                    setMove = Move(setT, sInputSymbol);

                    if (setMove.IsEmpty() == false)
                    {
                        setEclosure = Eclosure(setMove);

                        stateU = helper.FindDfaStateByEclosure(setEclosure);

                        if (stateU == null) 
                        {
                            stateU = new State();
                            if (IsAcceptingGroup(setEclosure) == true)
                            {
                                stateU.AcceptingState = true;
                            }

                            helper.AddDfaState(stateU, setEclosure); 
                        }

                        stateT.AddTransition(sInputSymbol, stateU);
                    }

                }  

            }  

            return stateStartDfa;

        }  

        
        private State ReduceDfa(State stateStartDfa)
        {
            Set setInputSymbol = new Set();
            Set setAllDfaState = new Set();

            GetAllStateAndInput(stateStartDfa, setAllDfaState, setInputSymbol);


            State stateStartReducedDfa = null;   // start state of the Reduced DFA
            ArrayList arrGroup = null;  // master array of all possible partitions/groups

             
            arrGroup = PartitionDfaGroups(setAllDfaState, setInputSymbol);

            
            foreach (object objGroup in arrGroup)
            {
                Set setGroup = (Set)objGroup;

                bool bAcceptingGroup = IsAcceptingGroup(setGroup);  
                bool bStartingGroup = setGroup.ElementExist(stateStartDfa); 

                
                State stateRepresentative = (State)setGroup[0]; 
                
                if (bStartingGroup == true)
                {
                    stateStartReducedDfa = stateRepresentative;
                }
                
                if (bAcceptingGroup == true)
                {
                    stateRepresentative.AcceptingState = true;
                }

                if (setGroup.GetCardinality() == 1)
                {
                    continue;  
                }

                
                setGroup.RemoveElement(stateRepresentative);

                State stateToBeReplaced = null;  
                int nReplecementCount = 0;
                foreach (object objStateToReplaced in setGroup)
                {
                    stateToBeReplaced = (State)objStateToReplaced;

                    setAllDfaState.RemoveElement(stateToBeReplaced); 

                    foreach (object objState in setAllDfaState)
                    {
                        State state = (State)objState;
                        nReplecementCount += state.ReplaceTransitionState(stateToBeReplaced, stateRepresentative);
                    }

                   
                }
            }  

            
            int nIndex = 0;
            while (nIndex < setAllDfaState.Count)
            {
                State state = (State)setAllDfaState[nIndex];
                if (state.IsDeadState())
                {
                    setAllDfaState.RemoveAt(nIndex);
                    
                    continue;
                }
                nIndex++;
            }


            return stateStartReducedDfa;
        }

       
        private bool IsAcceptingGroup(Set setGroup)
        {
            State state = null;

            foreach (object objState in setGroup)
            {
                state = (State)objState;

                if (state.AcceptingState == true)
                {
                    return true;
                }
            }

            return false;
        }

   
        private ArrayList PartitionDfaGroups(Set setMasterDfa, Set setInputSymbol)
        {
            ArrayList arrGroup = new ArrayList();  
            Map map = new Map();   
            Set setEmpty = new Set();
            
            Set setAccepting = new Set(); 
            Set setNonAccepting = new Set();  

            foreach (object objState in setMasterDfa)
            {
                State state = (State)objState;

                if (state.AcceptingState == true)
                {
                    setAccepting.AddElement(state);
                }
                else
                {
                    setNonAccepting.AddElement(state);
                }
            }

            if (setNonAccepting.GetCardinality() > 0)
            {
                arrGroup.Add(setNonAccepting);  // add this newly created partition to the master list
            }

            // for accepting state, there should always be at least one state, if NOT then there must be something wrong somewhere
            arrGroup.Add(setAccepting);   // add this newly created partition to the master list


            // now we iterate through these two partitions and see if they can be further partioned.
            // we continuew the iteration until no further paritioning is possible.

            IEnumerator iterInput = setInputSymbol.GetEnumerator();

            iterInput.Reset();

            while (iterInput.MoveNext())
            {
                string sInputSymbol = iterInput.Current.ToString();

                int nPartionIndex = 0;
                while (nPartionIndex < arrGroup.Count)
                {
                    Set setToBePartitioned = (Set)arrGroup[nPartionIndex];
                    nPartionIndex++;

                    if (setToBePartitioned.IsEmpty() || setToBePartitioned.GetCardinality() == 1)
                    {
                        continue;   // because we can't partition a set with zero or one memeber in it
                    }

                    foreach (object objState in setToBePartitioned)
                    {
                        State state = (State)objState;
                        State[] arrState = state.GetTransitions(sInputSymbol.ToString());

                        if (arrState != null)
                        {
                            Debug.Assert(arrState.Length == 1);

                            State stateTransionTo = arrState[0];  // since the state is DFA state, this array should contain only ONE state

                            Set setFound = FindGroup(arrGroup, stateTransionTo);
                            map.Add(setFound, state);
                        }
                        else   // no transition exists, so transition to empty set
                        {
                            //setEmpty = new Set();
                            map.Add(setEmpty, state);  // keep a map of which states transtion into which group

                        }
                    }  // end of foreach (object objState in setToBePartitioned)

                    if (map.Count > 1)  // means some states transition into different groups
                    {
                        arrGroup.Remove(setToBePartitioned);
                        foreach (DictionaryEntry de in map)
                        {
                            Set setValue = (Set)de.Value;
                            arrGroup.Add(setValue);
                        }
                        nPartionIndex = 0;  // we want to start from the begining again
                        iterInput.Reset();  // we want to start from the begining again
                    }
                    map.Clear();
                }  // end of while..loop


            }  // end of foreach (object objString in setInputSymbol)

            return arrGroup;
        }  // end of PartitionDfaSet method

       
        private Set FindGroup(ArrayList arrGroup, State state)
        {
            foreach (object objSet in arrGroup)
            {
                Set set = (Set)objSet;

                if (set.ElementExist(state) == true)
                {
                    return set;
                }
            }

            return null;
        }

       
        /// <returns>Formatted string</returns>
        private string SetToString(Set set)
        {
            string s = "";
            foreach (object objState in set)
            {
                State state = (State)objState;
                s += state.Id.ToString() + ", ";
            }

            s = s.TrimEnd(new char[] { ' ', ',' });
            if (s.Length == 0)
            {
                s = "Empty";
            }
            s = "{" + s + "}";
            return s;
        }

       
        static internal void GetAllStateAndInput(State stateStart, Set setAllState, Set setInputSymbols)
        {
            Set setUnprocessed = new Set();

            setUnprocessed.AddElement(stateStart);

            while (setUnprocessed.Count > 0)
            {
                State state = (State)setUnprocessed[0];

                setAllState.AddElement(state);
                setUnprocessed.RemoveElement(state);

                foreach (object objToken in state.GetAllKeys())
                {
                    string sSymbol = (string)objToken;
                    setInputSymbols.AddElement(sSymbol);

                    State[] arrTrans = state.GetTransitions(sSymbol);

                    if (arrTrans != null)
                    {
                        foreach (State stateEpsilon in arrTrans)
                        {
                            if (!setAllState.ElementExist(stateEpsilon))
                            {
                                setUnprocessed.AddElement(stateEpsilon);
                            }
                        }  // end of inner foreach..loop
                    }

                }  // end of outer foreach..loop

            }  // end of outer while..loop      

        }
        static internal int GetSerializedFsa(State stateStart, StringBuilder sb)
        {
            Set setAllState = new Set();
            Set setAllInput = new Set();
            GetAllStateAndInput(stateStart, setAllState, setAllInput);
            return GetSerializedFsa(stateStart, setAllState, setAllInput, sb);
        }
        static internal int GetSerializedFsa(State stateStart, Set setAllState, Set setAllSymbols, StringBuilder sb)
        {
            int nLineLength = 0;
            int nMinWidth = 6;
            string sLine = String.Empty;
            string sFormat = String.Empty;
            setAllSymbols.RemoveElement(MetaSymbol.EPSILON);
            setAllSymbols.AddElement(MetaSymbol.EPSILON); // adds it at the end;

            // construct header row and format string
            object[] arrObj = new object[setAllSymbols.Count + 1];// the extra one becuase of the first State column
            arrObj[0] = "State";
            sFormat = "{0,-8}";
            for (int i = 0; i < setAllSymbols.Count; i++)
            {
                string sSymbol = setAllSymbols[i].ToString();
                arrObj[i + 1] = sSymbol;

                sFormat += " | ";
                sFormat += "{" + (i + 1).ToString() + ",-" + Math.Max(Math.Max(sSymbol.Length, nMinWidth), sSymbol.ToString().Length) + "}";
            }
            sLine = String.Format(sFormat, arrObj);
            nLineLength = Math.Max(nLineLength, sLine.Length);
            sb.AppendLine(("").PadRight(nLineLength, '-'));
            sb.AppendLine(sLine);
            sb.AppendLine(("").PadRight(nLineLength, '-'));


            // construct the rows for transtion
            int nTransCount = 0;
            foreach (object objState in setAllState)
            {
                State state = (State)objState;
                arrObj[0] = (state.Equals(stateStart) ? ">" + state.ToString() : state.ToString());

                for (int i = 0; i < setAllSymbols.Count; i++)
                {
                    string sSymbol = setAllSymbols[i].ToString();

                    State[] arrStateTo = state.GetTransitions(sSymbol);
                    string sTo = String.Empty;
                    if (arrStateTo != null)
                    {
                        nTransCount += arrStateTo.Length;
                        sTo = arrStateTo[0].ToString();

                        for (int j = 1; j < arrStateTo.Length; j++)
                        {
                            sTo += ", " + arrStateTo[j].ToString();
                        }
                    }
                    else
                    {
                        sTo = "--";
                    }
                    arrObj[i + 1] = sTo;
                }

                sLine = String.Format(sFormat, arrObj);
                sb.AppendLine(sLine);
                nLineLength = Math.Max(nLineLength, sLine.Length);
            }

            sFormat = "State Count: {0}, Input Symbol Count: {1}, Transition Count: {2}";
            sLine = String.Format(sFormat, setAllState.Count, setAllSymbols.Count, nTransCount);
            nLineLength = Math.Max(nLineLength, sLine.Length);
            sb.AppendLine(("").PadRight(nLineLength, '-'));
            sb.AppendLine(sLine);
            nLineLength = Math.Max(nLineLength, sLine.Length);
            setAllSymbols.RemoveElement(MetaSymbol.EPSILON);

            return nLineLength;

        }

        public bool FindMatch(string sSearchIn,
                         int nSearchStartAt,
                         int nSearchEndAt,
                         ref int nFoundBeginAt,
                         ref int nFoundEndAt)
        {

            if (m_stateStartDfaM == null)
            {
                return false;
            }

            if (nSearchStartAt < 0)
            {
                return false;
            }

            State stateStart = m_stateStartDfaM;

            nFoundBeginAt = -1;
            nFoundEndAt = -1;

            bool bAccepted = false;
            State toState = null;
            State stateCurr = stateStart;
            int nIndex = nSearchStartAt;
            int nSearchUpTo = nSearchEndAt;


            while (nIndex <= nSearchUpTo)
            {

                if (m_bGreedy && IsWildCard(stateCurr) == true)
                {
                    if (nFoundBeginAt == -1)
                    {
                        nFoundBeginAt = nIndex;
                    }
                    ProcessWildCard(stateCurr, sSearchIn, ref nIndex, nSearchUpTo);
                }

                char chInputSymbol = sSearchIn[nIndex];

                toState = stateCurr.GetSingleTransition(chInputSymbol.ToString());

                if (toState == null)
                {
                    toState = stateCurr.GetSingleTransition(MetaSymbol.ANY_ONE_CHAR_TRANS);
                }

                if (toState != null)
                {
                    if (nFoundBeginAt == -1)
                    {
                        nFoundBeginAt = nIndex;
                    }

                    if (toState.AcceptingState)
                    {
                        if (m_bMatchAtEnd && nIndex != nSearchUpTo)  // then we ignore the accepting state
                        {
                            //toState = stateStart ;
                        }
                        else
                        {
                            bAccepted = true;
                            nFoundEndAt = nIndex;
                            if (m_bGreedy == false)
                            {
                                break;
                            }
                        }
                    }

                    stateCurr = toState;
                    nIndex++;
                }
                else
                {
                    if (!m_bMatchAtStart && !bAccepted)  // we reset everything
                    {
                        nIndex = (nFoundBeginAt != -1 ? nFoundBeginAt + 1 : nIndex + 1);

                        nFoundBeginAt = -1;
                        nFoundEndAt = -1;
                        //nIndex++;
                        stateCurr = stateStart;  // start from begining
                    }
                    else
                    {
                        break;
                    }
                }
            }  // end of while..loop 

            if (!bAccepted)
            {
                if (stateStart.AcceptingState == false)
                {
                    return false;
                }
                else // matched an empty string
                {
                    nFoundBeginAt = nSearchStartAt;
                    nFoundEndAt = nFoundBeginAt - 1;
                    return true;
                }
            }


            return true;
        }


        private bool IsWildCard(State state)
        {
            return (state == state.GetSingleTransition(MetaSymbol.ANY_ONE_CHAR_TRANS));
        }

       
        private void ProcessWildCard(State state, string sSearchIn, ref int nCurrIndex, int nSearchUpTo)
        {
            State toState = null;
            int nIndex = nCurrIndex;

            while (nIndex <= nSearchUpTo)
            {
                char ch = sSearchIn[nIndex];

                toState = state.GetSingleTransition(ch.ToString());

                if (toState != null)
                {
                    nCurrIndex = nIndex;
                }
                nIndex++;
            }

        }

        
        public bool IsReady()
        {
            return (m_stateStartDfaM != null);
        }
        
        public int GetLastErrorPosition()
        {
            return m_nLastErrorIndex;
        }
        
        public ErrorCode GetLastErrorCode()
        {
            return m_LastErrorCode;

        }

        public int GetLastErrorLength()
        {
            return m_nLastErrorLength;

        }

     
        public bool UseGreedy
        {
            get
            {
                return m_bGreedy;
            }
            set
            {
                m_bGreedy = value;
            }

        }

       
        private void UpdateValidationInfo(ValidationInfo vi)
        {
            if (vi.ErrorCode == ErrorCode.ERR_SUCCESS)
            {
                m_bMatchAtEnd = vi.MatchAtEnd;
                m_bMatchAtStart = vi.MatchAtStart;
            }

            m_LastErrorCode = vi.ErrorCode;
            m_nLastErrorIndex = vi.ErrorStartAt;
            m_nLastErrorLength = vi.ErrorLength;
        }
    }

}


