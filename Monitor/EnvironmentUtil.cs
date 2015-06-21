using Microsoft.Win32;

namespace JettyMonitor
{
    public static class EnvironmentUtil
    {
        /// <summary>
        /// 打开系统环境变量注册表
        /// </summary>
        /// <returns>RegistryKey</returns>
        private static RegistryKey OpenSysEnvironment()
        {
            RegistryKey key = Registry.LocalMachine;
            key = key.OpenSubKey("SYSTEM", true);//打开 HKEY_LOCAL_MACHINE 下的 SYSTEM 
            key = key.OpenSubKey("ControlSet001", true);//打开ControlSet001 
            key = key.OpenSubKey("Control", true);//打开 Control 
            key = key.OpenSubKey("Session Manager", true);//打开 Session Manager
            key = key.OpenSubKey("Environment", true);//打开 Environment

            return key;
        }

        /// <summary>
        /// 获取系统环境变量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetSysEnvironmentByName(string name)
        {
            string result = string.Empty;
            try
            {
                result = OpenSysEnvironment().GetValue(name).ToString();//读取
            }
            catch
            {
                return string.Empty;
            }
            return result;
        }

        /// <summary>
        /// 设置系统环境变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="strValue">值</param>
        public static void SetSysEnvironment(string name, string value)
        {
            OpenSysEnvironment().SetValue(name, value, RegistryValueKind.String);
        }

        /// <summary>
        /// 检测系统环境变量名称是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool CheckSysEnvironmentNameExist(string name)
        {
            if (!string.IsNullOrEmpty(GetSysEnvironmentByName(name)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 检测系统环境变量值是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool CheckSysEnvironmentValueExist(string name, string value)
        {
            if (CheckSysEnvironmentNameExist(name))
            {
                foreach (string v in GetSysEnvironmentByName(name).Split(';'))
                {
                    if (v == value)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 追加指定环境变量的值
        /// </summary>
        /// <param name="name">环境变量名</param>
        /// <param name="value">待追加的值</param>
        public static void AppendValue(string name, string value)
        {
            if (!CheckSysEnvironmentValueExist(name, value))
            {
                string values = GetSysEnvironmentByName(name);
                if (values.Substring(values.Length - 1, 1) == ";")
                {
                    SetSysEnvironment(name, values + value);
                }
                else
                {
                    SetSysEnvironment(name, values + ";" + value);
                }
            }
        }
    }
}