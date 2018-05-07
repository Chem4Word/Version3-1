using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Chem4Word.Model;
using Chem4Word.Model.Geometry;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Utils
{
    /// <summary>
    ///     Allows locking of sprouting bonds to fixed lengths and orientations depending on the
    ///     mouse location.
    /// </summary>
    public class SnapGeometry
    {
        private readonly Point _startPoint;
        private readonly int _lockAngle;

        public EditViewModel ViewModel { get; set; }
        /// <summary>
        ///     Creates a new SnapGeometry
        /// </summary>
        /// <param name="startPoint">location of the angle where the bond swings from.</param>
        /// <param name="angleIncrement">Angle in degrees - must be a factor of 360</param>
        public SnapGeometry(Point startPoint, int angleIncrement = 30)
        {
            _startPoint = startPoint;
            if (360 % angleIncrement != 0)
            {
                throw new ArgumentException("Angle must divide into 360!");
            }
            _lockAngle = angleIncrement;
        }

        /// <summary>
        ///     Locks the bond to a standard multiple of the
        ///     bond length and of angle n x lockangle.
        ///     Hold down Shift to unlock the length, and Ctrl to unlock the angle
        /// </summary>
        /// <param name="currentCoords">Coordinates of the mouse pointer</param>
        /// <param name="startAngle">Optional angle to start the locking at</param>
        /// <returns></returns>
        public Point SnapBond(Point currentCoords, MouseEventArgs e, int startAngle = 0)
        {
            Vector originalDisplacement = currentCoords - _startPoint;
            double angleInRads = 0.0;

            //snap the length if desired
            double bondLength = SnapLength(originalDisplacement, ViewModel.Model.MeanBondLength,   KeyboardUtils.HoldingDownShift());

            //and then snap the angle
            angleInRads = SnapAngle(startAngle, originalDisplacement, KeyboardUtils.HoldingDownControl());

            //minus  for second parameter as the coordinates go down the page
            Vector offset = new Vector(bondLength * Math.Sin(angleInRads), -bondLength * Math.Cos(angleInRads));

            return Vector.Add(offset, _startPoint);
        }

        public double SnapAngle(int startAngle, Vector originalDisplacement, bool holdingDownControl = false)
        {
            int originalAngle =
                (int)GetBondAngle(startAngle, originalDisplacement);
            double newangle = NormalizeBondAngle(originalAngle, _lockAngle);
            double angleInRads = 2 * Math.PI * newangle / 360;
            // Debug.WriteLine(newangle);
            //actually locks the angle to a multiple of the _lockAngle with a leeway of _lockangle/2 either way
            if (holdingDownControl)
            {
                //unlock the bond angle
                angleInRads = 2 * Math.PI * originalAngle / 360;
            }
            return angleInRads;
        }

        public static double SnapLength(Vector originalDisplacement, double newbondLength, bool holdingDownShift = false)
        {
            double bondLength = NormalizeBondLength(originalDisplacement, newbondLength);

            if (holdingDownShift)
            {
                //unlock the bond length
                bondLength = originalDisplacement.Length;
            }
            return bondLength;
        }

        /// <summary>
        ///  add lockAngle/2 to the original angle to give a leeway of that either way
        /// integer-divide it and then multiply by the lockangle to get a whole multiple of it
        /// </summary>
        /// <param name="originalAngle"></param>
        /// <param name="lockAngle"></param>
        /// <returns></returns>
        private static double NormalizeBondAngle(int originalAngle, int lockAngle)
        {
            return Math.Floor((double)(originalAngle + lockAngle / 2) / lockAngle) * lockAngle;
        }

        private static double GetBondAngle(int startAngle, Vector originalDisplacement)
        {
            return Math.Floor(Vector.AngleBetween(BasicGeometry.ScreenNorth, originalDisplacement) + startAngle);
        }

        private static double NormalizeBondLength(Vector originalDisplacement, double defaultLength)
        {
            return (Math.Floor(originalDisplacement.Length / defaultLength) + 1) * defaultLength;
        }
    }
}
