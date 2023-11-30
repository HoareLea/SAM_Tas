using System.Collections.Generic;

namespace SAM.Analytical.Tas.GenOpt
{
    public class ExecutableFile : GenOptFile
    {
        private string javaPath = null;
        private string className = "genopt.GenOpt";
        private string configFilePath = null;
        private bool pause = true;

        public ExecutableFile(string javaPath, string configFilePath, bool pause = true)
        {
            this.javaPath = javaPath;
            this.configFilePath = configFilePath;
            this.pause = pause;
        }

        protected override string GetText()
        {
            if(string.IsNullOrWhiteSpace(javaPath) || string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(configFilePath))
            {
                return null;
            }

            List<string> texts = new List<string>();
            texts.Add("@echo off");
            texts.Add(string.Format("java -classpath \"{0}\" {1} \"{2}\"", javaPath, className, configFilePath));
            if(pause)
            {
                texts.Add("pause");
            }

            return string.Join("\n", texts);
        }
    }
}
