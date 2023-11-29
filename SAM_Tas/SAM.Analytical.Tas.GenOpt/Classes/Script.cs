namespace SAM.Analytical.Tas.GenOpt
{
    public class Script : GenOptObject
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
