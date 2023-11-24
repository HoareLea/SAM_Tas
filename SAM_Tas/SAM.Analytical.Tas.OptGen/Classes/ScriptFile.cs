namespace SAM.Analytical.Tas.OptGen
{
    public class ScriptFile : OptGenFile
    {
        private Script script;

        public ScriptFile(string text) 
        {
            this.script = new Script(text);
        }

        protected override string GetText() 
        { 
            return script.Text; 
        }
    }
}
