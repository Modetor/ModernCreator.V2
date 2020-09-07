using System;
namespace Modetor.Net.Server
{
    class Settings
    {
        public static readonly string Path = string.Format(AppDomain.CurrentDomain.BaseDirectory + "base{0}settings.ini", System.IO.Path.DirectorySeparatorChar);

        public static bool Read()
        {
            try
            {
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

    }
}
