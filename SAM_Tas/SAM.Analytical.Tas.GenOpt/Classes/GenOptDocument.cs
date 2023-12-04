using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SAM.Analytical.Tas.GenOpt
{
    public class GenOptDocument
    {
        private string configFileName = "Config.ini";
        private string executableFileName = "GenOpt.bat";
        private string outputFileName = "TasOutputs.txt";

        private string directory = null;

        private Script script = new Script(string.Empty);
        private Files files = Create.Files();
        private List<IParameter> parameters = new List<IParameter>();
        private Algorithm algorithm = new GoldenSectionAlgorithm();
        private OptimizationSettings optimizationSettings = new OptimizationSettings();
        private ObjectiveFunctionLocation objectiveFunctionLocation = new ObjectiveFunctionLocation();
        private IO iO = new IO();
        private SimulationError simulationError = new SimulationError();
        private SimulationStart simulationStart = new SimulationStart();

        public GenOptDocument(string directory)
        {
            this.directory = directory;
        }

        private string GetDirectory()
        {
            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                return null;
            }

            return directory;
        }

        public bool Run()
        {
            if (!IsValid())
            {
                return false;
            }

            string directory = GetDirectory();
            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                return false;
            }

            string command = Create.Command(directory);
            if(string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            if(simulationStart == null)
            {
                simulationStart = new SimulationStart();
            }

            simulationStart.Command = command;

            ScriptFile scriptFile = ScriptFile;
            if(scriptFile != null)
            {
                string path = System.IO.Path.Combine(directory, "Script.txt");
                if(!string.IsNullOrWhiteSpace(path))
                {
                    scriptFile.Save(path);
                }
            }
            
            CommandFile commandFile = CommandFile;
            if(commandFile != null)
            {
                string path = GetPath(FileType.Command);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    commandFile.Save(path);
                }
            }

            ConfigFile configFile = ConfigFile;
            if (configFile != null)
            {
                string path = System.IO.Path.Combine(directory, configFileName);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    configFile.Save(path);
                }
            }

            SimulationConfigFile simulationConfigFile = SimulationConfigFile;
            if (simulationConfigFile != null)
            {
                string path = GetPath(FileType.Configuration);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    simulationConfigFile.Save(path);
                }
            }

            TemplateFile templateFile = TemplateFile;
            if (templateFile != null)
            {
                string path = GetPath(FileType.Template);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    templateFile.Save(path);
                }
            }

            ParameterFile parameterFile = ParameterFile;
            if (parameterFile != null)
            {
                string path = GetPath(FileType.Input);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    parameterFile.Save(path);
                }
            }

            OutputFile outputFile = OutputFile;
            if (outputFile != null)
            {
                string path = System.IO.Path.Combine(directory, outputFileName);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    outputFile.Save(path);
                }
            }

            bool result = false;

            Core.Tas.Modify.SetProjectDirectory(directory);

            ExecutableFile executableFile = ExecutableFile;
            if(executableFile != null)
            {
                string path = System.IO.Path.Combine(directory, executableFileName);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    executableFile.Save(path);

                    ProcessStartInfo processStartInfo = new ProcessStartInfo(path);
                    Process process = Process.Start(processStartInfo);
                    process.WaitForExit();
                    process.Close();

                    result = true;
                }
            }

            return result;
        }

        bool IsValid()
        {
            if(string.IsNullOrWhiteSpace(script?.Text))
            {
                return false;
            }

            return true;
        }

        public string Directory
        {
            get
            {
                return directory;
            }

            set
            {
                directory = value;
            }
        }

        public ExecutableFile ExecutableFile
        {
            get
            {
                string directory = GetDirectory();
                if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
                {
                    return null;
                }

                return new ExecutableFile(Query.TasGenOptJavaPath(), System.IO.Path.Combine(directory, configFileName));
            }
        }

        public CommandFile CommandFile
        {
            get
            {
                return new CommandFile() { OptimizationSettings = optimizationSettings, Algorithm = algorithm, Parameters = parameters };
            }
        }

        public ConfigFile ConfigFile
        {
            get
            {
                Simulation simulation = new Simulation()
                {
                    Files = new Files(files[FileType.Template], files[FileType.Input], files[FileType.Log], files[FileType.Output], files[FileType.Configuration]),
                    ObjectiveFunctionLocation = objectiveFunctionLocation,
                };

                Optimization optimization = new Optimization()
                {
                    Files = new Files(files[FileType.Command]),
                };

                return new ConfigFile() { Simulation = simulation, Optimization = optimization };
            }
        }

        public SimulationConfigFile SimulationConfigFile
        {
            get
            {
                return new SimulationConfigFile()
                {
                    SimulationError = simulationError,
                    IO = iO,
                    SimulationStart = simulationStart,
                };
            }
        }

        public TemplateFile TemplateFile
        {
            get
            {
                return new TemplateFile(parameters);
            }
        }

        public ParameterFile ParameterFile
        {
            get
            {
                return new ParameterFile(parameters);
            }
        }

        public ScriptFile ScriptFile
        {
            get
            {
                return new ScriptFile(script);
            }
        }

        public OutputFile OutputFile
        {
            get
            {
                return new OutputFile(objectiveFunctionLocation);
            }
        }

        public string GetPath(FileType fileType)
        {
            string directory = GetDirectory();
            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                return null;
            }

            string name = files?[fileType]?.Names?.FirstOrDefault();
            if(string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return System.IO.Path.Combine(directory, name);

        }

        public List<string> GetPaths(FileType fileType)
        {
            string directory = GetDirectory();
            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                return null;
            }

            IEnumerable<string> names = files?[fileType]?.Names;
            if(names == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach(string name in names)
            {
                result.Add(System.IO.Path.Combine(directory, name));
            }

            return result;
        }

        public bool SetFileName(FileType fileType, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            files?.Clear(fileType);

            return AddFileName(fileType, fileName);
        }
        
        public bool AddFileName(FileType fileType, string fileName)
        {
            if(string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            if(files == null)
            {
                files = new Files();
            }

            return files.Add(fileType, fileName);
        }

        public bool AddParameter(string name, double initial, double min, double max, double step)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if(parameters == null)
            {
                parameters = new List<IParameter>();
            }

            IParameter parameter = parameters.Find(x => x.Name == name);
            if (parameter != null)
            {
                parameters.Remove(parameter);
            }

            parameters.Add(new NumberParameter() { Name = name, Initial = initial, Min = min, Max = max, Step = step });
            return true;
        }

        public bool AddParameter(IParameter parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter?.Name))
            {
                return false;
            }

            if (parameters == null)
            {
                parameters = new List<IParameter>();
            }

            IParameter parameter_Existing = parameters.Find(x => x.Name == parameter.Name);
            if (parameter_Existing != null)
            {
                parameters.Remove(parameter_Existing);
            }

            parameters.Add(parameter);
            return true;
        }

        public bool AddObjective(Objective objective)
        {
            if (string.IsNullOrWhiteSpace(objective?.Name))
            {
                return false;
            }

            if (objectiveFunctionLocation == null)
            {
                objectiveFunctionLocation = new ObjectiveFunctionLocation();
            }

            return objectiveFunctionLocation.Add(objective);
        }

        public bool AddObjective(string name, string delimiter)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (objectiveFunctionLocation == null)
            {
                objectiveFunctionLocation = new ObjectiveFunctionLocation();
            }

            return objectiveFunctionLocation.Add(name, delimiter);
        }

        public bool AddObjective(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (objectiveFunctionLocation == null)
            {
                objectiveFunctionLocation = new ObjectiveFunctionLocation();
            }

            return objectiveFunctionLocation.Add(name);
        }

        public bool AddScript(string text)
        {
            script = new Script(text);

            return true;
        }

        public Algorithm Algorithm
        {
            get
            {
                return algorithm;
            }

            set
            {
                algorithm = value;
            }
        }

        public string ErrorMessage
        {
            get
            {
                return simulationError?.ErrorMessage;
            }

            set
            {
                if(simulationError == null)
                {
                    simulationError = new SimulationError();
                }

                simulationError.ErrorMessage = value;
            }
        }

        public NumberFormat? NumberFormat
        {
            get
            {
                return iO?.NumberFormat;
            }

            set
            {
                if(value == null || !value.HasValue)
                {
                    return;
                }

                if(iO == null)
                {
                    iO = new IO();
                }

                iO.NumberFormat = value.Value;

            }
        }

        public string Command
        {
            get
            {
                return simulationStart?.Command;
            }

            set
            {
                if(simulationStart == null)
                {
                    simulationStart = new SimulationStart();
                }

                simulationStart.Command = value;
            }
        }

        public bool? WriteInputFileExtension
        {
            get
            {
                return simulationStart?.WriteInputFileExtension;
            }

            set
            {
                if(value == null || !value.HasValue)
                {
                    return;
                }

                if(simulationStart == null)
                {
                    simulationStart = new SimulationStart();
                }

                simulationStart.WriteInputFileExtension = value.Value;
            }
        }


    }
}
