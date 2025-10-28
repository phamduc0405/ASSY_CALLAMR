using Microsoft.SqlServer.Server;
using Mitsu3E;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASSY_CALLAMR
{
    public class DataQueue
    {
        public bool IsSendBlock { get; set; }
        public string Address { get; set; }
        public short[] Value { get; set; }
        public int length { get; set; }
    }

    public class PlcHelper
    {
        private IMitsubishiPlc _plcSlmp = null;
        public  ConcurrentDictionary<string, int> DeviceRead = new ConcurrentDictionary<string, int>();
        private  Queue<DataQueue> _queueData = new Queue<DataQueue>();
        private  Thread _threadWrite;
        private  bool _threadRunning;
        public  bool IsPlcConnected;
        public  event Action<string, int> ValueChangeEvent;
        private short[] _previousValues; // Thêm biến để lưu giá trị trước đó

        public PlcHelper()
        {
           
        }
        #region Public Method
        public void SetSlmp(PlcConfig config)
        {
            _plcSlmp = new McProtocolTcp(config.PlcIp, config.PlcPort, config.PcIp);
            _plcSlmp.ConnectedEvent += (Isconnect) =>
            {
                IsPlcConnected = Isconnect;
                Logger.Info("Kết nối SLMP: " + (IsPlcConnected ? "Thành công" : "Thất bại"));
            };
        }

        public int Open()
        {
            try
            {
                if (_threadWrite != null && _threadWrite.IsAlive)
                {
                    Close();
                }
                _threadRunning = true;
                _threadWrite = new Thread(WritePLC);
                _threadWrite.IsBackground = true;
                _threadWrite.Start();

                int result = 0;
                result = _plcSlmp.Open();

                Logger.Info($"Kết nối PLC: {(IsPlcConnected ? "Thành công" : "Thất bại")}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"Lỗi mở kết nối PLC: {ex.Message}");
                IsPlcConnected = false;
                return 1;
            }
        }

        public int Close()
        {
            try
            {
                _threadRunning = false;
                if (_threadWrite != null && _threadWrite.IsAlive)
                {
                    _threadWrite.Join(5000);
                    if (_threadWrite.IsAlive)
                    {
                        Logger.Warn("Thread write không dừng kịp, bỏ qua.");
                    }
                }

                _queueData.Clear();

                int result = 1;
                result = _plcSlmp.Close();


                IsPlcConnected = false; // Đặt lại trạng thái
                _threadWrite = null;
                Logger.Info($"Ngắt kết nối PLC: {(result == 0 ? "Thành công" : "Thất bại")}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"Lỗi đóng kết nối PLC: {ex.Message}");
                IsPlcConnected = false; // Đặt lại trạng thái khi có lỗi
                return 1;
            }
        }

        public int SetInt(string device, int value)
        {
            return SetDevice(device, value);
        }
        public int SetString(string Address, string value, int length = 0)
        {
            return SetWordFromString(Address, value, length);
        }
        public int SetBit(string device, bool value)
        {
           return SetDevice(device, value ? 1 : 0);
        }
        public void RegisterInDevice(string device, int value)
        {
            DeviceRead[device] = value;
        }
        public int ReadDeviceRandom()
        {
            int iReturnCode;
            String szDeviceName;
            int iNumberOfData;
            short[] arrDeviceValue;
            var keys = DeviceRead.Keys.ToArray();
            szDeviceName = String.Join("\n", keys);

            iNumberOfData = keys.Length;
            arrDeviceValue = new short[iNumberOfData];

            try
            {
                iReturnCode = 0;
                iReturnCode = _plcSlmp.ReadRandomDeviceBlock(keys, out arrDeviceValue);
                LogPlcValues(keys, arrDeviceValue, iReturnCode);
            }
            catch (Exception)
            {
                return 99;
            }

            if (iReturnCode == 0)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (DeviceRead[keys[i]] != arrDeviceValue[i])
                    {
                        ValueChangeEvent?.Invoke(keys[i], arrDeviceValue[i]);
                        DeviceRead[keys[i]] = arrDeviceValue[i];
                    }
                }
            }
            return iReturnCode;
        }
        #endregion

        #region Private Method
        private void WritePLC()
        {
            while (_threadRunning)
            {
                if (_queueData.Count > 0)
                {
                    DataQueue data = _queueData.Dequeue();
                    int iReturnCode = 1;

                    if (data.IsSendBlock)
                    {
                        try
                        {

                            iReturnCode = _plcSlmp.WriteDeviceBlock2(data.Address, data.length, data.Value);
                        }
                        catch (Exception)
                        {
                            // Không gán giá trị cụ thể cho iReturnCode
                        }
                    }
                    else
                    {
                        try
                        {

                            iReturnCode = _plcSlmp.SetDevice2(data.Address, data.Value[0]);

                        }
                        catch (Exception)
                        {
                            // Không gán giá trị cụ thể cho iReturnCode
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

       

        private int SetWordFromString(string Address, string value, int length = 0)
        {
            if (!IsPlcConnected) // Kiểm tra kết nối
            {
                return 0;
            }

            byte[] bytes = Encoding.ASCII.GetBytes(value);
            List<short> data = new List<short>();
            for (int n = 0; n < length * 2; n += 2)
            {
                short sample;
                if (n >= bytes.Length)
                {
                    sample = 0;
                }
                else
                {
                    sample = (short)((n + 1) >= bytes.Length ? (short)(bytes[n]) : (short)(bytes[n] | bytes[n + 1] << 8));
                }

                data.Add(sample);
            }
            var arshort = data.ToArray();
            DataQueue dataq = new DataQueue
            {
                Address = Address,
                length = length,
                Value = arshort,
                IsSendBlock = true
            };
            _queueData.Enqueue(dataq);
            return 0;
        }

        private int SetDevice(string device, int value)
        {
            if (!IsPlcConnected) // Kiểm tra kết nối
            {
                return 0;
            }

            short[] arshort = new short[1];
            arshort[0] = (short)value;
            DataQueue dataq = new DataQueue
            {
                Address = device,
                length = 1,
                Value = arshort,
                IsSendBlock = false
            };
            _queueData.Enqueue(dataq);

            return 0;
        }

        

        private void LogPlcValues(string[] keys, short[] arrDeviceValue, int iReturnCode)
        {
            if (_previousValues == null || _previousValues.Length != arrDeviceValue.Length)
            {
                _previousValues = (short[])arrDeviceValue.Clone();
                Logger.Info(string.Format("Read PLC SLMP: {0}, Dữ liệu khởi tạo: ", iReturnCode) + string.Join(", ", keys.Select((k, i) => string.Format("{0}: {1}", k, arrDeviceValue[i]))));
            }
            else
            {
                List<string> list = new List<string>();
                for (int j = 0; j < arrDeviceValue.Length; j++)
                {
                    if (arrDeviceValue[j] != _previousValues[j])
                    {
                        list.Add(string.Format("{0}: {1}", keys[j], arrDeviceValue[j]));
                        _previousValues[j] = arrDeviceValue[j];
                    }
                }
                if (list.Count > 0)
                {
                    Logger.Info("Thay đổi giá trị PLC: " + string.Join(", ", list));
                }
            }
        }
        #endregion

    }
}
