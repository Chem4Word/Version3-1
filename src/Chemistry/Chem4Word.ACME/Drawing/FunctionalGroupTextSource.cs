using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
    partial class FunctionalGroupTextSource : TextSource
    {
        public List<LabelTextSourceRun> Runs = new List<LabelTextSourceRun>();

        public bool AnchoredAtBeginning
        {
            get { return Runs[0].IsAnchor; }
        }

        private Regex descParser = new Regex(@"(?<anchor>\[(?<normal>[()A-Za-z]+)(?<subscript>[0-9]*)\])|((?<normal>[()A-Za-z]+)(?<subscript>[0-9]*))", RegexOptions.ExplicitCapture);

        public FunctionalGroupTextSource(string descriptor)
        {
            ParseTheGroup(descriptor);
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