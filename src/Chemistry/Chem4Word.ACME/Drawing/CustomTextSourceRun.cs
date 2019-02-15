namespace Chem4Word.ACME.Drawing
{
    public class CustomTextSourceRun
    {
        public string Text;
        public bool IsSubscript;
        public bool IsEndParagraph;
        public int Length { get { return IsEndParagraph ? 1 : Text.Length; } }
    }
}