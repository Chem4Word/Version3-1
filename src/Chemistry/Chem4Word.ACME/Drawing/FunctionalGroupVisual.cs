// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System;
using System.Collections.Generic;

namespace Chem4Word.ACME.Drawing
{
    public class FunctionalGroupVisual : ChemicalVisual
    {
        private List<CustomTextSourceRun> ComponentRuns { get; }
        public FunctionalGroup ParentGroup { get; }
        public bool Flipped { get; set; }
        public FunctionalGroupVisual(FunctionalGroup fg)
        {
            ParentGroup = fg;
            ComponentRuns = new List<CustomTextSourceRun>();
        }

        private void GetTextRuns()
        {
            BuildTextRuns(ParentGroup, ComponentRuns);
        }

        private void BuildTextRuns(ElementBase parentGroup, List<CustomTextSourceRun> componentRuns)
        {
            if(parentGroup is FunctionalGroup fg && fg.ShowAsSymbol)
            {
                componentRuns.Add(new CustomTextSourceRun { Text = fg.Symbol });
            }
            else if (parentGroup is Element e)
            {
                componentRuns.Add(new CustomTextSourceRun { Text = e.Symbol });
            }
            else if (parentGroup is FunctionalGroup fg2)
            {
                if(fg2.Flippable)
                {
                   
                    for (int i = 0; i< fg2.Components.Count; i++)
                    {
                        var group = fg2.Components[i];
                        var component = group.Resolve();
                        int count = group.Count;

                        if (!Flipped)
                        {
                            if(count > 1 && component is FunctionalGroup fg3)
                            {
                                //need to draw brackets around it
                                componentRuns.Add(new CustomTextSourceRun { Text = "(" });
                                BuildTextRuns(fg3, componentRuns);
                                ComponentRuns.Add(new CustomTextSourceRun { Text = ")" });
                                ComponentRuns.Add(new CustomTextSourceRun { Text = count.ToString(), IsSubscript=true });

                            }
                            else if (count > 1 && component is Element e2)
                            {

                            }
                        }
                    }
                }
            }
        }

        public override void Render()
        {
            GetTextRuns();
        }
    }

    public class CustomTextSourceRun
    {
        public string Text;
        public bool IsSubscript;
        public bool IsEndParagraph;
        public int Length { get { return IsEndParagraph ? 1 : Text.Length; } }
    }
}