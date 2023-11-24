namespace SAM.Analytical.Tas.OptGen
{
    public class Script : OptGenObject
    {
        private string text;

        public Script(string text) 
        {
            this.text = text;
        }

        protected override string GetText() 
        { 
            return text; 
        }
    }
}
