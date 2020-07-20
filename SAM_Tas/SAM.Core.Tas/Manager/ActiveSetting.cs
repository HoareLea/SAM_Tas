﻿using System.Reflection;

namespace SAM.Core.Tas
{
    public static partial class ActiveSetting
    {
        public static class Name
        {
            public const string ParameterMap = "Parameter Map";
        }

        private static Setting setting = Load();

        private static Setting Load()
        {
            Setting setting = ActiveManager.GetSetting(Assembly.GetExecutingAssembly());
            if (setting == null)
                setting = GetDefault();

            return setting;
        }

        public static Setting Setting
        {
            get
            {
                return setting;
            }
        }

        public static Setting GetDefault()
        {
            Setting result = new Setting(Assembly.GetExecutingAssembly());


            return result;
        }
    }
}