
namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string DefaultPath(this TasSettingParameter tasSettingParameter)
        {
            if(!ActiveSetting.Setting.TryGetValue(tasSettingParameter, out string name) || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string directory = ResourcesDirectory();
            if(string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            return global::System.IO.Path.Combine(directory, name);
        }
    }
}