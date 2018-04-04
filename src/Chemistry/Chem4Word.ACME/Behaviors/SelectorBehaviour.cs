// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using Chem4Word.View;

namespace Chem4Word.ACME.Behaviors
{
    public class SelectorBehaviour: BaseEditBehavior
    {
        private Point elementStartPosition;
        private Point mouseStartPosition;
        private TranslateTransform transform = new TranslateTransform();

        protected override void OnAttached()
        {
            base.OnAttached();

            Window parent = Application.Current.MainWindow;
            AssociatedObject.RenderTransform = transform;

            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            

        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) //single click
            {
               var hitTestResult = GetTarget(e);
                if (hitTestResult.VisualHit is AtomShape)
                {
                    var atom = (AtomShape) hitTestResult.VisualHit;
                    MessageBox.Show($"Hit Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");

                    ViewModel.SelectedItems.Add(atom);
                   
                }

                if (hitTestResult.VisualHit is BondShape)
                {
                    var bond = (BondShape)hitTestResult.VisualHit;
                    MessageBox.Show($"Hit Bond {bond.ParentBond.Id} at ({e.GetPosition(AssociatedObject).X},{e.GetPosition(AssociatedObject).Y})");

                    ViewModel.SelectedItems.Add(bond);

                }
            }
        }

        private HitTestResult GetTarget(MouseButtonEventArgs e)
        {
            return VisualTreeHelper.HitTest(AssociatedObject, e.GetPosition(AssociatedObject));
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            }
        }
    }
}
