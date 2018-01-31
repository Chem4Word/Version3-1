using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chem4Word.ViewModel
{
    public static class AtomHelpers
    {
        public static string GetSubText(int hCount = 0)
        {
            string mult = "";
            int i;
            if ((i = Math.Abs(hCount)) > 1)
            {
                mult = i.ToString();
            }

            return mult;
        }
        /// <summary>
        /// gets the charge annotation string for an atom symbol
        /// </summary>
        /// <param name="charge">Int contaioning the charge value</param>
        /// <returns></returns>
        public static string GetChargeString(int charge)
        {
            string chargestring = "";

            if (charge > 0)
            {
                chargestring = "+";
            }
            if (charge < 0)
            {
                chargestring = "-";
            }
            int abscharge = 0;
            if ((abscharge = Math.Abs(charge)) > 1)
            {
                chargestring = abscharge.ToString() + chargestring;
            }
            return chargestring;
        }
    }
}
