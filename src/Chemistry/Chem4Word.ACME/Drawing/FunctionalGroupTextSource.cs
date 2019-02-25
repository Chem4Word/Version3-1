using System;
using System.Collections.Generic;

using System.Windows.Media.TextFormatting;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Drawing
{
    partial class FunctionalGroupTextSource : TextSource
    {
        public List<LabelTextSourceRun> Runs = new List<LabelTextSourceRun>();

        public bool AnchoredAtBeginning
        {
            get { return Runs[0].IsAnchor; }
        }

        

        public FunctionalGroupTextSource(string descriptor)
        {
            ParseTheGroup(descriptor);
        }

        public FunctionalGroupTextSource(FunctionalGroup parentGroup, bool isFlipped = false)
        {
        
            
            string result = "";

            if (parentGroup.ShowAsSymbol)
            {
                Runs.Add(new LabelTextSourceRun() {IsAnchor = true, IsEndParagraph = false, Text = parentGroup.Symbol});
            }
            else
            {
                if (isFlipped && parentGroup.Flippable)
                {
                    for (int i = parentGroup.Components.Count - 1; i >= 0; i--)
                    {
                      

                        Append(parentGroup.Components[i], isAnchor: i ==0);

                        
                    }
                }
                else
                {
                    int ii = 0;
                    foreach (var component in parentGroup.Components)
                    {  
                        Append(component, isAnchor: ii == 0);              
                        ii++;
                    }
                }
            }

            // Local Function
            void Append(Group component, bool isAnchor)
            {
                ElementBase elementBase;
                var ok = Group.TryParse(component.Component, out elementBase);
                if (ok)
                {
                    if (elementBase is Element)
                    {
                        Runs.Add(new LabelTextSourceRun
                            {IsAnchor = isAnchor, IsEndParagraph = false, Text = component.Component});
                        if (component.Count > 1)
                        {
                            Runs.Add(new Su
                                { IsAnchor = isAnchor, IsEndParagraph = false, Text = component.Component });
                        }
                    }
                    if (elementBase is FunctionalGroup fg)
                    {
                        if (fg.ShowAsSymbol)
                        {
                            if (component.Count == 1)
                            {
                                result += $"{component.Component}";
                            }
                            else
                            {
                                result += $"({component.Component}){component.Count}";
                            }
                        }
                        else
                        {
                            result += fg.Expand(reverse);
                        }
                    }
                }
                else
                {
                    result += "?";
                }
            }
        }
        private void ParseTheGroup(string descriptor)
        {
            var matches = descParser.Matches(descriptor);

            var groupnames = descParser.GetGroupNames();
            var groupNumbers = descParser.GetGroupNumbers();
            bool isAnchor = false;
            foreach (Match match in matches)
            {
                string val = match.Value;
                GroupCollection gc = match.Groups;
                if (val.StartsWith("["))
                {
                    isAnchor = true;
                }

                var normaltext = match.Groups["normal"];
                
               
                Runs.Add(new LabelTextSourceRun()
                {
                    IsSubscript = false,
                    IsAnchor = isAnchor,
                    IsEndParagraph = false,
                    Text = normaltext.Value
                });

                string substring="";
                try
                {
                    var subtext = match.Groups["subscript"];
                    substring = subtext.Value;
                    if(substring!="")

                    { Runs.Add(new LabelTextSourceRun()
                        {
                            IsSubscript = true,
                            IsAnchor = isAnchor,
                            IsEndParagraph = false,
                            Text = substring
                        });

                    }
                }
                catch (Exception e)
                {
                  
                }
                
            }

            Runs.Add(new LabelTextSourceRun() {IsEndParagraph = true});
        }

        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            int pos = 0;
            foreach (var currentRun in Runs)
            {
                if (textSourceCharacterIndex < pos + currentRun.Length)
                {
                    if (currentRun.IsEndParagraph)
                    {
                        return new TextEndOfParagraph(1);
                    }

                    TextRunProperties props;
                    if (currentRun.IsSubscript)
                    {
                        props = new SubscriptTextRunProperties();
                    }
                    else
                    {
                        props = new LabelTextRunProperties();
                    }
                        

                    return new TextCharacters(
                        currentRun.Text,
                        textSourceCharacterIndex - pos,
                        currentRun.Length - (textSourceCharacterIndex - pos),
                        props);


                }
                pos += currentRun.Length;
            }

            // Return an end-of-paragraph if no more text source.
            return new TextEndOfParagraph(1);
        }

        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Length
        {
            get
            {
                int r = 0;
                foreach (var currentRun in Runs)
                {
                    r += currentRun.Length;
                }
                return r;
            }
        }
    }
}