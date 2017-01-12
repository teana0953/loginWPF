using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace LoginWpf
{
    [Serializable]
    public class Setting
    {
        public LogIn logInWay { get; set; }
        public string GoogleAccessToken { get; set; }
        public string FbAccessToken { get; set; }

        public static void SaveSettingConfig(string fileName, Setting setting)
        {
            try
            {
                using (Stream output = File.Create(fileName + ".config"))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(output, setting);
                }
            }
            catch (Exception ee)
            {

            }
        }

        public static Setting LoadSettingConfig(string filePath)
        {
            Setting setting = new Setting();
            try
            {
                using (Stream input = File.OpenRead(filePath))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    setting = (Setting)formatter.Deserialize(input);
                }
            }
            catch (SerializationException)
            {
                //System.Windows.Forms.MessageBox.Show("Unable to read this file");
            }
            catch (Exception)
            {

            }

            return setting;
        }
    }
}
