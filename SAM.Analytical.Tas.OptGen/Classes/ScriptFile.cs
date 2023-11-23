namespace SAM.Analytical.Tas.OptGen
{
    public class ScriptFile : OptGenObject, IOptGenFile
    {
        private string text;

        public ScriptFile(string text) 
        {
            this.text = text;
        }

        protected override string GetText() 
        { 
            return text; 
        }
    }
}
