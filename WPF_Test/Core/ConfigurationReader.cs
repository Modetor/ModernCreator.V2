using System;
using System.Collections.Generic;
using System.Text;

namespace ModernCreator_Server.Core
{
    internal class ConfigurationReader
    {
        private ConfigurationReader(string p)
        {

        }





        private static ConfigurationReader instance = null;
        public static ConfigurationReader Read(string p)
        {
            if (instance == null)
                instance = new ConfigurationReader(p);
            return instance;
        }
    }
}
