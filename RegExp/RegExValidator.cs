using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegExp
{
    public enum ErrorCode
    {
        ERR_SUCCESS,        // the patten is in correct format
        ERR_PREN_MISMATCH,  // "(A(D*)"
        ERR_EMPTY_PREN,     // "()"
        ERR_EMPTY_BRACKET,  // "[]"
        ERR_BRACKET_MISMATCH,  // "["
        ERR_OPERAND_MISSING,  // "A|"
        ERR_INVALID_ESCAPE,   // "\A"
        ERR_INVALID_RANGE,  // "[C-A]" 
        ERR_EMPTY_STRING,   // ""
    }

    internal class ValidationInfo
    {
        public ErrorCode ErrorCode = ErrorCode.ERR_SUCCESS;
        public int ErrorStartAt = -1;
        public int ErrorLength = -1;
        public string FormattedString = String.Empty;
        public bool MatchAtStart = false;
        public bool MatchAtEnd = false;
    }

    internal class MetaSymbol
    {
        public const char CONCANATE = '.';
        public const char ALTERNATE = '|';
        public const char ZERO_OR_MORE = '*';
        public const char ONE_OR_MORE = '+';
        public const char ZERO_OR_ONE = '?';
        public const char OPEN_PREN = '(';
        public const char CLOSE_PREN = ')';
        public const char COMPLEMENT = '^';
        public const char ANY_ONE_CHAR = '_';  
        public const string ANY_ONE_CHAR_TRANS = "AnyChar";  
        public const char ESCAPE = '\\';
        public const string EPSILON = "epsilon";
        public const char CHARSET_START = '[';
        public const char CHARSET_END = ']';
        public const char RANGE = '-';
        public const string DUMMY = "Dummy";  
        public const char MATCH_START = '^';  
        public const char MATCH_END = '$';  
        public const char NEW_LINE = 'n';
        public const char TAB = 't';
    }


    internal class RegExValidator
    {

        private bool m_bConcante = false;
        private bool m_bAlternate = false;

        private const char m_chNull = '\0'; // null symbol;
        private char m_chSym = m_chNull;

        private int m_nPatternLength = -1;
        private string m_sPattern = String.Empty;
        private int m_nCurrPos = -1;
        private StringBuilder m_sb = null;

        private ValidationInfo m_validationInfo = null;

        public RegExValidator()
        {

        }

        public ValidationInfo Validate(string sPattern)
        {
            m_chSym = m_chNull;
            m_bConcante = false;
            m_bAlternate = false;
            m_sb = new StringBuilder(1024);
            m_nCurrPos = -1;
            m_sPattern = sPattern;
            m_nPatternLength = m_sPattern.Length;

            m_validationInfo = new ValidationInfo();

            if (sPattern.Length == 0)
            {
                m_validationInfo.ErrorCode = ErrorCode.ERR_EMPTY_STRING;
                return m_validationInfo;
            }

            GetNextSymbol();

            string sLit1 = MetaSymbol.MATCH_START.ToString();
            string sLit2 = MetaSymbol.MATCH_END.ToString();
            string sLit3 = sLit1 + sLit2;

            if (!(sPattern.CompareTo(sLit1) == 0 || sPattern.CompareTo(sLit2) == 0 || sPattern.CompareTo(sLit3) == 0))
            {
                if (sPattern[0] == MetaSymbol.MATCH_START)
                {
                    m_validationInfo.MatchAtStart = true;
                    Accept(MetaSymbol.MATCH_START);
                }
                if (m_sPattern[m_nPatternLength - 1] == MetaSymbol.MATCH_END)
                {
                    m_validationInfo.MatchAtEnd = true;
                    m_nPatternLength--;
                }
            }

            try
            {
                while (m_nCurrPos < m_nPatternLength)
                {
                    switch (m_chSym)
                    {
                        case MetaSymbol.ALTERNATE:
                        case MetaSymbol.ONE_OR_MORE:
                        case MetaSymbol.ZERO_OR_MORE:
                        case MetaSymbol.ZERO_OR_ONE:
                            Abort(ErrorCode.ERR_OPERAND_MISSING, m_nCurrPos, 1);
                            break;
                        case MetaSymbol.CLOSE_PREN:
                            Abort(ErrorCode.ERR_PREN_MISMATCH, m_nCurrPos, 1);
                            break;
                        default:
                            Expression();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            m_validationInfo.FormattedString = m_sb.ToString();

            return m_validationInfo;
        }
        private void GetNextSymbol()
        {
            m_nCurrPos++;
            if (m_nCurrPos < m_nPatternLength)
            {
                m_chSym = m_sPattern[m_nCurrPos];
            }
            else
            {
                m_chSym = m_chNull;
            }
        }
        private bool Accept(char ch)
        {
            if (m_chSym == ch)
            {
                GetNextSymbol();
                return true;
            }
            return false;
        }
        private bool AcceptPostfixOperator()
        {
            switch (m_chSym)
            {
                case MetaSymbol.ONE_OR_MORE:
                case MetaSymbol.ZERO_OR_MORE:
                case MetaSymbol.ZERO_OR_ONE:
                    m_sb.Append(m_chSym);
                    return Accept(m_chSym);
                default:
                    return false;
            }
        }
        private bool AcceptNonEscapeChar()
        {
            switch (m_chSym)
            {
                case MetaSymbol.ALTERNATE:
                case MetaSymbol.CHARSET_START:
                case MetaSymbol.CLOSE_PREN:
                case MetaSymbol.ESCAPE:
                case MetaSymbol.ONE_OR_MORE:
                case MetaSymbol.OPEN_PREN:
                case MetaSymbol.ZERO_OR_MORE:
                case MetaSymbol.ZERO_OR_ONE:
                case MetaSymbol.CONCANATE:
                case m_chNull:
                    return false;
                default:
                    AppendConate();
                    m_sb.Append(m_chSym);
                    Accept(m_chSym);
                    break;
            }
            return true;
        }
        private bool Expect(char ch)
        {
            if (Accept(ch))
            {
                return true;
            }
            return false;
        }
        private bool ExpectEscapeChar()
        {
            switch (m_chSym)
            {
                case MetaSymbol.ALTERNATE:
                case MetaSymbol.ANY_ONE_CHAR:
                case MetaSymbol.CHARSET_START:
                case MetaSymbol.CLOSE_PREN:
                case MetaSymbol.COMPLEMENT:
                case MetaSymbol.ESCAPE:
                case MetaSymbol.ONE_OR_MORE:
                case MetaSymbol.OPEN_PREN:
                case MetaSymbol.ZERO_OR_MORE:
                case MetaSymbol.ZERO_OR_ONE:
                    m_sb.Append(MetaSymbol.ESCAPE);
                    m_sb.Append(m_chSym);
                    Accept(m_chSym);
                    break;
                case MetaSymbol.NEW_LINE:
                    m_sb.Append('\n');
                    Accept(m_chSym);
                    break;
                case MetaSymbol.TAB:
                    m_sb.Append('\t');
                    Accept(m_chSym);
                    break;
                default:
                    return false;
            }
            return true;
        }
        private void Abort(ErrorCode errCode, int nErrPosition, int nErrLen)
        {
            m_validationInfo.ErrorCode = errCode;
            m_validationInfo.ErrorStartAt = nErrPosition;
            m_validationInfo.ErrorLength = nErrLen;

            throw new Exception("Syntex error.");
        }
        private void AppendConate()
        {
            if (m_bConcante)
            {
                m_sb.Append(MetaSymbol.CONCANATE);
                m_bConcante = false;
            }
        }
        private void AppendAlternate()
        {
            if (m_bAlternate)
            {
                m_sb.Append(MetaSymbol.ALTERNATE);
                m_bAlternate = false;
            }
        }
        private void Expression()
        {
            while (Accept(MetaSymbol.ESCAPE))
            {
                AppendConate();
                if (!ExpectEscapeChar())
                {
                    Abort(ErrorCode.ERR_INVALID_ESCAPE, m_nCurrPos - 1, 1);
                }
                AcceptPostfixOperator();
                m_bConcante = true;
            }

            while (Accept(MetaSymbol.CONCANATE))
            {
                AppendConate();
                m_sb.Append(MetaSymbol.ESCAPE);
                m_sb.Append(MetaSymbol.CONCANATE);
                AcceptPostfixOperator();
                m_bConcante = true;
            }

            while (Accept(MetaSymbol.COMPLEMENT))
            {
                AppendConate();
                m_sb.Append(MetaSymbol.ESCAPE);
                m_sb.Append(MetaSymbol.COMPLEMENT);
                AcceptPostfixOperator();
                m_bConcante = true;
            }

            while (AcceptNonEscapeChar())
            {
                AcceptPostfixOperator();
                m_bConcante = true;
                Expression();
            }

            if (Accept(MetaSymbol.OPEN_PREN))
            {
                int nEntryPos = m_nCurrPos - 1;
                AppendConate();
                m_sb.Append(MetaSymbol.OPEN_PREN);
                Expression();
                if (!Expect(MetaSymbol.CLOSE_PREN))
                {
                    Abort(ErrorCode.ERR_PREN_MISMATCH, nEntryPos, m_nCurrPos - nEntryPos);
                }
                m_sb.Append(MetaSymbol.CLOSE_PREN);

                int nLen = m_nCurrPos - nEntryPos;
                if (nLen == 2)
                {
                    Abort(ErrorCode.ERR_EMPTY_PREN, nEntryPos, m_nCurrPos - nEntryPos);
                }

                AcceptPostfixOperator();
                m_bConcante = true;
                Expression();
            }


            if (Accept(MetaSymbol.CHARSET_START))
            {
                int nEntryPos = m_nCurrPos - 1;
                bool bComplement = false;

                AppendConate();

                if (Accept(MetaSymbol.COMPLEMENT))
                {
                    bComplement = true;
                }

                string sTmp = m_sb.ToString();

                m_sb = new StringBuilder(1024);
                m_bAlternate = false;
                CharecterSet();

                if (!Expect(MetaSymbol.CHARSET_END))
                {
                    Abort(ErrorCode.ERR_BRACKET_MISMATCH, nEntryPos, m_nCurrPos - nEntryPos);
                }

                int nLen = m_nCurrPos - nEntryPos;

                if (nLen == 2)  // "[]"
                {
                    Abort(ErrorCode.ERR_EMPTY_BRACKET, nEntryPos, m_nCurrPos - nEntryPos);
                }
                else if (nLen == 3 && bComplement == true) // "[^]"  - treat the complement as literal
                {
                    m_sb = new StringBuilder(1024);
                    m_sb.Append(sTmp);
                    m_sb.Append(MetaSymbol.OPEN_PREN);
                    m_sb.Append(MetaSymbol.ESCAPE);
                    m_sb.Append(MetaSymbol.COMPLEMENT);
                    m_sb.Append(MetaSymbol.CLOSE_PREN);
                }
                else
                {
                    string sCharset = m_sb.ToString();
                    m_sb = new StringBuilder(1024);
                    m_sb.Append(sTmp);
                    if (bComplement)
                    {
                        m_sb.Append(MetaSymbol.COMPLEMENT);
                    }
                    m_sb.Append(MetaSymbol.OPEN_PREN);
                    m_sb.Append(sCharset /*ExpandRange(sCharset, nEntryPos) */   );
                    m_sb.Append(MetaSymbol.CLOSE_PREN);
                }

                AcceptPostfixOperator();

                m_bConcante = true;

                Expression();
            }


            if (Accept(MetaSymbol.ALTERNATE))
            {
                int nEntryPos = m_nCurrPos - 1;
                m_bConcante = false;
                m_sb.Append(MetaSymbol.ALTERNATE);
                Expression();
                int nLen = m_nCurrPos - nEntryPos;
                if (nLen == 1)
                {
                    Abort(ErrorCode.ERR_OPERAND_MISSING, nEntryPos, m_nCurrPos - nEntryPos);
                }
                Expression();
            }


        }
        private string ExpectEscapeCharInBracket()
        {
            char ch = m_chSym;

            switch (m_chSym)
            {
                case MetaSymbol.CHARSET_END:
                case MetaSymbol.ESCAPE:
                    //AppendAlternate();
                    //m_sb.Append(MetaSymbol.ESCAPE);
                    //m_sb.Append(m_chSym);
                    //return Accept(m_chSym);
                    Accept(m_chSym);
                    return MetaSymbol.ESCAPE.ToString() + ch.ToString();
                case MetaSymbol.NEW_LINE:
                    //AppendAlternate();
                    //m_sb.Append(Environment.NewLine);
                    //return Accept(m_chSym);
                    Accept(m_chSym);
                    //return Environment.NewLine;
                    return ('\n').ToString();
                case MetaSymbol.TAB:
                    //AppendAlternate();
                    //m_sb.Append('\t');
                    //return Accept(m_chSym);
                    Accept(m_chSym);
                    return ('\t').ToString();
                default:
                    return String.Empty;
            }
        }
        private string AcceptNonEscapeCharInBracket()
        {
            char ch = m_chSym;

            switch (ch)
            {
                case MetaSymbol.CHARSET_END:
                case MetaSymbol.ESCAPE:
                case m_chNull:
                    return String.Empty;
                case MetaSymbol.ALTERNATE:
                case MetaSymbol.ANY_ONE_CHAR:
                case MetaSymbol.CLOSE_PREN:
                case MetaSymbol.COMPLEMENT:
                case MetaSymbol.ONE_OR_MORE:
                case MetaSymbol.OPEN_PREN:
                case MetaSymbol.ZERO_OR_MORE:
                case MetaSymbol.ZERO_OR_ONE:
                case MetaSymbol.CONCANATE:
                    Accept(m_chSym);
                    return MetaSymbol.ESCAPE.ToString() + ch.ToString();
                default:
                    Accept(m_chSym);
                    return ch.ToString();
            }
        }
        private void CharecterSet()
        {
            int nRangeFormStartAt = -1;
            int nStartAt = -1;
            int nLength = -1;

            // xx-xx form
            string sLeft = String.Empty;
            string sRange = String.Empty;
            string sRight = String.Empty;


            string sTmp = String.Empty;

            while (true)
            {
                sTmp = String.Empty;

                nStartAt = m_nCurrPos;

                if (Accept(MetaSymbol.ESCAPE))
                {
                    if ((sTmp = ExpectEscapeCharInBracket()) == String.Empty)
                    {
                        Abort(ErrorCode.ERR_INVALID_ESCAPE, m_nCurrPos - 1, 1);
                    }
                    nLength = 2;
                }

                if (sTmp == String.Empty)
                {
                    sTmp = AcceptNonEscapeCharInBracket();
                    nLength = 1;
                }

                if (sTmp == String.Empty)
                {
                    break;
                }

                if (sLeft == String.Empty)
                {
                    nRangeFormStartAt = nStartAt;
                    sLeft = sTmp;
                    AppendAlternate();
                    m_sb.Append(sTmp);
                    m_bAlternate = true;
                    continue;
                }

                if (sRange == String.Empty)
                {
                    if (sTmp != MetaSymbol.RANGE.ToString())
                    {
                        nRangeFormStartAt = nStartAt;
                        sLeft = sTmp;
                        AppendAlternate();
                        m_sb.Append(sTmp);
                        m_bAlternate = true;
                        continue;
                    }
                    else
                    {
                        sRange = sTmp;
                    }
                    continue;
                }

                sRight = sTmp;


                bool bOk = ExpandRange(sLeft, sRight);

                if (bOk == false)
                {
                    int nSubstringLen = (nStartAt + nLength) - nRangeFormStartAt;

                    Abort(ErrorCode.ERR_INVALID_RANGE, nRangeFormStartAt, nSubstringLen);
                }
                sLeft = String.Empty;
                sRange = String.Empty;
                sRange = String.Empty;
            }

            if (sRange != String.Empty)
            {
                AppendAlternate();
                m_sb.Append(sRange);
                m_bAlternate = true;
            }

        }
        private bool ExpandRange(string sLeft, string sRight)
        {
            char chLeft = (sLeft.Length > 1 ? sLeft[1] : sLeft[0]);
            char chRight = (sRight.Length > 1 ? sRight[1] : sRight[0]);

            if (chLeft > chRight)
            {
                return false;
            }

            chLeft++;
            while (chLeft <= chRight)
            {
                AppendAlternate();

                switch (chLeft)
                {
                    case MetaSymbol.ALTERNATE:
                    case MetaSymbol.ANY_ONE_CHAR:
                    case MetaSymbol.CLOSE_PREN:
                    case MetaSymbol.COMPLEMENT:
                    case MetaSymbol.CONCANATE:
                    case MetaSymbol.ESCAPE:
                    case MetaSymbol.ONE_OR_MORE:
                    case MetaSymbol.ZERO_OR_MORE:
                    case MetaSymbol.ZERO_OR_ONE:
                    case MetaSymbol.OPEN_PREN:
                        m_sb.Append(MetaSymbol.ESCAPE);
                        break;
                    default:
                        break;
                }

                m_sb.Append(chLeft);
                m_bAlternate = true;
                chLeft++;
            }

            return true;

        }




    }
}
