using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASSY_CALLAMR
{
    public class PlcConfig
    {
        public string ID { get; set; } = "44";
        public string PlcIp { get; set; } = "192.168.1.10";
        public int PlcPort { get; set; } = 6000;
        public string PcIp { get; set; } = "192.168.1.100";

        public int TimeAlive { get; set; } = 1000;
        public int TimeOut { get; set; } = 5000;
        public string WIN { get; set; } = "D100";
        public string WOCode { get; set; } = "D200";
        public string WOResult { get; set; } = "D210";
        public string BOAlive { get; set; } = "M100";
        public string BOResult { get; set; } = "M110";
        public string BIAck { get; set; } = "M120";
        public string BIReset { get; set; } = "M130";
        public string BIStart { get; set; } = "M140";
    }
    public class APIConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:5000/api/";
        public string Endpoint { get; set; } = "api/v1/equipment/task";
        public string KeyNo { get; set; } = "ASSY_CALLARM_01";
        public string ApiKey { get; set; } = "";
        public string ApiSecret { get; set; } = "";

    }
    public class APIMessage
    {
        public string KeyNo { get; set; }
        public string Message { get; set; }
        public Action<TaskResponse> Callback { get; set; }
        public APIMessage(string keyNo, string message, Action<TaskResponse> callback)
        {
            KeyNo = keyNo;
            Message = message;                       
            Callback = callback;
        }
    }
    public class TaskResponse
    {
        [JsonProperty("resultCode")]
        public int ResultCode { get; set; }

        [JsonProperty("resultDate")]
        public string ResultDate { get; set; }

        [JsonProperty("resultMessage")]
        public string ResultMessage { get; set; }
    }
    public class EqConfig
    {
        public List<PlcConfig> PLCs { get; set; } = new List<PlcConfig>();
        public APIConfig API { get; set; } = new APIConfig();
    }
}
