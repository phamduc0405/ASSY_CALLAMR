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
using System.Windows.Threading;
using System.Xml.Serialization;

namespace ASSY_CALLAMR
{
    public class Controller
    {
        public EqConfig Config = new EqConfig();
        private static string ConfigFile = "Config.xml";
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public List<Equipment> Equipments = new List<Equipment>();
        private EquipmentApiService _apiService;
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
            _apiService = new EquipmentApiService(Config.API.BaseUrl);
        }
        public void Start()
        {
            foreach (var plcConfig in Config.PLCs)
            {
                var equipment = new Equipment(plcConfig);
                equipment.RequestApiEvent += Equipment_RequestApiEvent;
                equipment.Start();
                Equipments.Add(equipment);
            }
        }

        private void Equipment_RequestApiEvent(object sender, APIMessage mess)
        {
            var eq = (Equipment)sender;
            LogApp.Info($"[{eq.Config.ID}] bắn event: {mess}");
            string keyNo = mess.KeyNo; // ← Copy ra biến
            string message = mess.Message;
            var apiMsg = new APIMessage(
         keyNo: keyNo,
         message: message,
         callback: result =>
         {
             LogApp.Info($"API → {keyNo}: [{result.ResultCode}] {result.ResultMessage}");
             if (result.ResultCode == 200)
             {
                 eq.ResponseAPI(result);
             }
         });

            _apiService.EnqueueRequest(apiMsg);
        }

        public void SendApiMessage(APIMessage mess,string id)
        {
            
            LogApp.Info($"[{id}] bắn event: {mess}");
            string keyNo = mess.KeyNo;
            string message = mess.Message;
            var apiMsg = new APIMessage(
         keyNo: keyNo,
         message: message,
         callback: result =>
         {
             LogApp.Info($"API → {keyNo}: [{result.ResultCode}] {result.ResultMessage}");
             
         });

            _apiService.EnqueueRequest(apiMsg);
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
