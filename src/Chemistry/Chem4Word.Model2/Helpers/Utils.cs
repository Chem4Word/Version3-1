﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Chem4Word.Model2.Helpers
{
    public static class Utils
    {
        public static bool AreAllH(this IEnumerable<Atom> atomlist)
        {
            return atomlist.All(a => (a.Element as Element) == Globals.PeriodicTable.H);
        }

        public static bool ContainNoH(this IEnumerable<Atom> atomList)
        {
            return atomList.All(a => ((a.Element as Element) != Globals.PeriodicTable.H & a.ImplicitHydrogenCount == 0));
        }

        public static Atom GetFirstNonH(this IEnumerable<Atom> atomList)
        {
            return atomList.FirstOrDefault(a => a.Element as Element != Globals.PeriodicTable.H);
        }

        public static int GetHCount(this IEnumerable<Atom> atomList)
        {
            return atomList.Count(a => a.Element as Element == Globals.PeriodicTable.H);
        }

        public static int GetNonHCount(this IEnumerable<Atom> atomList)
        {
            return atomList.Count() - atomList.GetHCount();
        }

        //collection utils

        public static void RemoveAll(this IList list)
        {
            while (list.Count > 0)
            {
                list.RemoveAt(list.Count - 1);
            }
        }

        public static T CloneExcept<T, S>(this T target, S source, string[] propertyNames)
        {
            if (source == null)
            {
                return target;
            }
            Type sourceType = typeof(S);
            Type targetType = typeof(T);
            BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

            PropertyInfo[] properties = sourceType.GetProperties();
            foreach (PropertyInfo sPI in properties)
            {
                if (!propertyNames.Contains(sPI.Name))
                {
                    PropertyInfo tPI = targetType.GetProperty(sPI.Name, flags);
                    if (tPI != null && tPI.PropertyType.IsAssignableFrom(sPI.PropertyType))
                    {
                        tPI.SetValue(target, sPI.GetValue(source, null), null);
                    }
                }
            }
            return target;
        }
    }
}