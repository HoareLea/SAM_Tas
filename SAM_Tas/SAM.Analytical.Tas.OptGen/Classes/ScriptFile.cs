namespace SAM.Analytical.Tas.OptGen
{
    public class ScriptFile : OptGenFile
    {
        private Script script;

        public ScriptFile(string text) 
        {
            this.script = new Script(text);
        }

        public ScriptFile(Script script)
        {
            this.script = script;
        }

        public Script Script
        {
            get
            {
                return script;
            }

            set
            {
                script = value;
            }
        }

        protected override string GetText() 
        { 
            return script.Text; 
        }
    }
}
