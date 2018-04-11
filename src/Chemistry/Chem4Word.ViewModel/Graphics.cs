using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model;
using Chem4Word.ViewModel;
namespace Chem4Word.ViewModel
{
    public static class Graphics
    {
        /// <summary>
        /// Returns a ghost image of the molecule for dragging, resizing and rotating
        /// </summary>
        /// <param name="deformedCoords">optional List of coordinates by atoms that have become displaced by an operation</param>
        /// <returns></returns>
        public static System.Windows.Media.Geometry Ghost( this Molecule mol,  Dictionary<Atom, Point> deformedCoords = null)
        {
            Point? GetAdjustedAtomCoords(Atom startAtom, Dictionary<Atom, Point> coords)
            {
                if (coords != null && coords.ContainsKey(startAtom))
                    return coords[startAtom];
                else
                {
                    return startAtom?.Position;
                }
            }
            System.Windows.Media.GeometryGroup ghostGeometry = new GeometryGroup();
            foreach (Bond b in mol.Bonds)
            {

                ghostGeometry.Children.Add(
                    BondGeometry.SingleBondGeometry(
                    GetAdjustedAtomCoords(b.StartAtom, deformedCoords).Value,
                    GetAdjustedAtomCoords(b.EndAtom, deformedCoords).Value));
            }
            return ghostGeometry.GetFlattenedPathGeometry();


        }
    }
}
