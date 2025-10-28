using ASSY_CALLARM;
using Mitsu3E;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ASSY_CALLAMR
{
    public class Controller
    {
        public EqConfig Config = new EqConfig();
        private static string ConfigFile = "Config.xml";
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public List<Equipment> Equipments = new List<Equipment>();
        public Controller() 
        {
            Initial();
            SaveConfig(Config);
        }
        #region Initial Method
        public void Initial()
        {
            ConfigFile = Path.Combine(BaseDir, ConfigFile);
            Config = LoadConfig();
            Config = Config ?? new EqConfig();
            Config.PLCs = Config.PLCs ?? new List<PlcConfig>();
        }
        public void Start()
        {
            foreach (var plcConfig in Config.PLCs)
            {
                var equipment = new Equipment(plcConfig);
                equipment.Start();
                Equipments.Add(equipment);
            }
        }
        public void Stop()
        {
            foreach (var equipment in Equipments)
            {
                equipment.Stop();
            }
        }
        #endregion


        #region Public Method

        #endregion

        #region Private Method

        private void RequestAPI()
        {

        }
        private EqConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFile))
                    return new EqConfig();

                using (var reader = new StreamReader(ConfigFile))
                {
                    var serializer = new XmlSerializer(typeof(EqConfig));
                    var config = (EqConfig)serializer.Deserialize(reader);
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                    LogApp.Info("Config loaded:\n" + json);

                    return config;
                }
            }
            catch (Exception ex)
            {
                LogApp.Error("Load config error: " + ex.Message);
                return new EqConfig();
            }
        }
        private void SaveConfig(EqConfig config)
        {
            try
            {
                using (var writer = new StreamWriter(ConfigFile))
                {
                    var serializer = new XmlSerializer(typeof(EqConfig));
                    serializer.Serialize(writer, config);
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                    LogApp.Info("Config Save :\n" + json);
                }
            }
            catch (Exception ex)
            {

                LogApp.Error("Save config error: " + ex.Message);
            }
        }
        
        #endregion


    }


}
