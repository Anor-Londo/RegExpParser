using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegExp
{
    class Program
    {
        static void Main(string[] args)
        {
            RegEx m_regEx = new RegEx();

            string txtRegEx = Console.ReadLine();


            StringBuilder sb = new StringBuilder(10000);
            try
            {
                ErrorCode errCode = m_regEx.CompileWithStats(txtRegEx, sb);
                if (errCode != ErrorCode.ERR_SUCCESS)
                {
                    Console.WriteLine($"your line is: {txtRegEx}");
                    string sErrSubstring = txtRegEx.Substring(m_regEx.GetLastErrorPosition(), m_regEx.GetLastErrorLength());
                    string sFormat = "Error occured during compilation.\nCode: {0}\nAt: {1}\nSubstring: {2}";
                    sFormat = String.Format(sFormat, errCode.ToString(), m_regEx.GetLastErrorPosition(), sErrSubstring);
                    Console.WriteLine(sFormat);
                    Console.ReadKey();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured during compilation.\n\n" + ex.Message);
                return;
            }
            Console.WriteLine(sb.ToString());

            string txtSearchString = Console.ReadLine();



        int nFoundStart = -1;
        int nFoundEnd = -1;
        int nStartAt = 0;
        int nMatchLength = -1;

            do
            {
                bool bFound = m_regEx.FindMatch(txtSearchString, nStartAt, txtSearchString.Length - 1, ref nFoundStart, ref nFoundEnd);
                if (bFound == true)
                {
                    string sSubstring = "{Empty String}";
                    nMatchLength = nFoundEnd - nFoundStart + 1;
                    if (nMatchLength > 0)
                    {
                        sSubstring = txtSearchString.Substring(nFoundStart, nMatchLength);
                    }

                    nStartAt = nFoundEnd + 1;
                }
                else
                {
                    break;
                }
            } while (nStartAt < txtSearchString.Length);

            Console.Write($"found at: {nFoundStart}\n");
            Console.WriteLine($"Length: {nMatchLength}\n");
            Console.ReadKey();


        }
    }
}

