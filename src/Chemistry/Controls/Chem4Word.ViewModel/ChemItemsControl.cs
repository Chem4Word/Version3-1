using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Chem4Word.Model;

namespace Chem4Word.View

{

    public class ChemItemsControl : ItemsControl
    {

        //see http://www.dotnetcurry.com/wpf/1160/wpf-itemscontrol-fundamentals-part1
        //https://docs.microsoft.com/en-us/dotnet/framework/wpf/data/how-to-find-datatemplate-generated-elements
        //http://drwpf.com/blog/2007/11/05/itemscontrol-c-is-for-collection/

        private TChildItem FindVisualChild<TChildItem>(DependencyObject obj)
            where TChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is TChildItem)
                    return (TChildItem)child;
                else
                {
                    TChildItem childOfChild = FindVisualChild<TChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }


        private void GetAssociatedShape(object item)
        {
            ContentPresenter cp = this.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
            if (cp != null)
            {
                cp.ApplyTemplate();
                DataTemplate myDataTemplate = cp.ContentTemplate;
                if (myDataTemplate != null)
                {
                    Debug.WriteLine(myDataTemplate.DataType.ToString());

                    AtomShape atomShape = (AtomShape) myDataTemplate.FindName("AtomShape", cp);
                    Debug.WriteLine(atomShape?.ToString());

                    BondShape bondShape = (BondShape) myDataTemplate.FindName("BondShape", cp);
                    Debug.WriteLine(bondShape?.ToString());
                }
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            Debug.WriteLine("Items changed");
            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    GetAssociatedShape(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    GetAssociatedShape(item);
                }
            }

        }
        
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            //base.OnItemsSourceChanged(oldValue, newValue);

            //foreach (object coll in newValue)
            //{
            //    Debug.Assert(coll is CollectionContainer);
            //    foreach (var chemitem in ((CollectionContainer)coll).Collection)
            //    {
            //        if (chemitem is Atom | chemitem is Bond)
            //        {
            //            Debug.WriteLine(chemitem.ToString());
            //            GetAssociatedShape(chemitem);
            //        }
            //    }
            //}
        }

        public ChemItemsControl()
        {
            this.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;

        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                foreach (object chemitem in Items)
                {
                    if (chemitem is Atom | chemitem is Bond)
                    {
                        Debug.WriteLine(chemitem.ToString());
                        GetAssociatedShape(chemitem);
                    }
                }
            }
        }

        
    }
}
