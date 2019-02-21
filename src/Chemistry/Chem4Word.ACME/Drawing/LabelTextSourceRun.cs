namespace Chem4Word.ACME.Drawing
{
    public class LabelTextSourceRun
    {
        public string Text;
        public bool IsAnchor { get; set; }
        public bool IsSubscript { get; set; }
        public bool IsEndParagraph;
        public int Length { get { return IsEndParagraph ? 1 : Text.Length; } }
    }
}