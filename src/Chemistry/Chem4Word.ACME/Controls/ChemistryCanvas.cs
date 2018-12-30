// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    public class ChemistryCanvas : Canvas
    {
        public ChemistryCanvas()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size size = new Size();

            try
            {
                foreach (UIElement element in this.InternalChildren)
                {
                    double left = Canvas.GetLeft(element);
                    double top = Canvas.GetTop(element);
                    left = double.IsNaN(left) ? 0 : left;
                    top = double.IsNaN(top) ? 0 : top;

                    //measure desired size for each child
                    element.Measure(constraint);

                    Size desiredSize = element.DesiredSize;
                    if (!double.IsNaN(desiredSize.Width) && !double.IsNaN(desiredSize.Height))
                    {
                        size.Width = Math.Max(size.Width, left + desiredSize.Width);
                        size.Height = Math.Max(size.Height, top + desiredSize.Height);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            // add margin
            size.Width += 10;
            size.Height += 10;
            return size;
        }
    }
}