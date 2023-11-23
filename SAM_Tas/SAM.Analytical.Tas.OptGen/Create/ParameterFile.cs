namespace SAM.Analytical.Tas.OptGen
{
    public static partial class Create
    {
        public static ParameterFile ParameterFile(this CommandFile commandFile)
        {
            if(commandFile == null)
            {
                return null;
            }

            ParameterFile result = new ParameterFile(commandFile.Parameters);

            return result;
        }
    }
}