// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
//
using Chem4Word.Model.Geometry;
using Chem4Word.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;

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

        public EditViewModel ViewModel { get; }

        /// <summary>
        ///     Creates a new SnapGeometry
        /// </summary>
        /// <param name="startPoint">location of the angle where the bond swings from.</param>
        /// <param name="angleIncrement">Angle in degrees - must be a factor of 360</param>
        public SnapGeometry(Point startPoint, EditViewModel viewModel,  int angleIncrement = 15)
        {
            ViewModel = viewModel;
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
        public Point SnapBond(Point currentCoords, MouseEventArgs e, double startAngle = 0d)
        {
            Vector originalDisplacement = currentCoords - _startPoint;
            double angleInRads = 0.0;

            //snap the length if desired
            double bondLength = SnapLength(originalDisplacement, ViewModel.Model.XamlBondLength, KeyboardUtils.HoldingDownShift());

            //and then snap the angle
            angleInRads = SnapAngle(startAngle, originalDisplacement, KeyboardUtils.HoldingDownControl());

            //minus  for second parameter as the coordinates go down the page
            Vector offset = new Vector(bondLength * Math.Sin(angleInRads), -bondLength * Math.Cos(angleInRads));

            return Vector.Add(offset, _startPoint);
        }

        public double SnapAngle(double startAngle, Vector originalDisplacement, bool holdingDownControl = false)
        {

            int originalAngle =
                (int)GetBondAngle(startAngle, originalDisplacement);
            double angleInRads = 0.0;
            // Debug.WriteLine(newangle);
            //actually locks the angle to a multiple of the _lockAngle with a leeway of _lockangle/2 either way
            if (holdingDownControl)
            {
                //unlock the bond angle
                angleInRads = 2 * Math.PI * (originalAngle + startAngle) / 360;
            }

            else
            {
                 double newangle = NormalizeBondAngle(originalAngle, _lockAngle) + startAngle;
                 angleInRads = 2 * Math.PI * newangle / 360;
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

        private static double GetBondAngle(double startAngle, Vector originalDisplacement)
        {
            return Math.Floor(Vector.AngleBetween(BasicGeometry.ScreenNorth, originalDisplacement) - startAngle);
        }

        private static double NormalizeBondLength(Vector originalDisplacement, double defaultLength)
        {
            return (Math.Floor(originalDisplacement.Length / defaultLength) + 1) * defaultLength;
        }
    }
}