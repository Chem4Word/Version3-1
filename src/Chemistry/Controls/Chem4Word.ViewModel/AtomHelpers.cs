using System;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model;
using Chem4Word.Model.Geometry;

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
        public static string GetChargeString(int? charge)
        {
            string chargestring = "";

            if ((charge ?? 0) > 0)
            {
                chargestring = "+";
            }
            if ((charge ?? 0) < 0)
            {
                chargestring = "-";
            }
            int abscharge = 0;
            if ((abscharge = Math.Abs(charge ?? 0)) > 1)
            {
                chargestring = abscharge.ToString() + chargestring;
            }
            return chargestring;
        }

        public static CompassPoints GetDefaultHOrientation(Atom parent)
        {
            if (parent.ImplicitHydrogenCount >= 1)
            {
                System.Windows.Media.Geometry hGeometry;
                if (parent.Bonds.Count == 0)
                {
                    return CompassPoints.East;
                }
                else if (parent.Bonds.Count == 1)
                {
                    if (Vector.AngleBetween(BasicGeometry.ScreenNorth,
                        parent.Bonds[0].OtherAtom(parent).Position - parent.Position) > 0)
                    //the bond is on the right
                    {
                        
                        return CompassPoints.West;
                    }
                    else
                    {
                        //default to any old rubbish for now
                        return CompassPoints.East;
                        
                    }
                }
                else
                {
                    double baFromNorth = Vector.AngleBetween(BasicGeometry.ScreenNorth,
                        parent.BalancingVector);

                    return BasicGeometry.SnapTo4NESW(baFromNorth);
                    
                }
 
            }
            return CompassPoints.East;
        }
    }
}
