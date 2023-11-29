using System.Collections.Generic;

namespace SAM.Analytical.Tas.GenOpt
{
    public class ExecutableFile : GenOptFile
    {
        private string javaPath = null;
        private string className = "genopt.GenOpt";
        private string configFileName = "config.ini";

        public ExecutableFile(string javaPath, string configFileName)
        {
            this.javaPath = javaPath;
            this.configFileName = configFileName;
        }

        protected override string GetText()
        {
            if(string.IsNullOrWhiteSpace(javaPath) || string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(configFileName))
            {
                return null;
            }

            List<string> texts = new List<string>();
            texts.Add("@echo off");
            texts.Add(string.Format("java -classpath \\\"{0}\\\" {1} {2}", javaPath, className, configFileName));

            return string.Join("\n", texts);
        }
    }
}
