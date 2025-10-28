using ASSY_CALLAMR;
using ASSY_CALLARM;
using Mitsu3E;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASSY_CALLAMR
{
    public class Equipment
    {
        private PlcConfig _config;
        private PlcHelper _plcH;
        private Thread _plcCheck = null;
        private Thread _plcRead = null;
        private bool _plcThreadRunning = false;
        private Stopwatch _stopwatch = new Stopwatch();

        public PlcHelper PLC
        { get { return _plcH; } }
        #region Event
        public delegate void RequestApiEventDelegate(APIMessage mess);
        public event RequestApiEventDelegate RequestApiEvent;
        #endregion
        public Equipment(PlcConfig config)
        {
            _config = config;
            _plcH = new PlcHelper();
            _plcH.ValueChangeEvent += _plcH_ValueChangeEvent;
            Initial();
            _plcThreadRunning = true;
            _plcCheck = new Thread(PLCAlive);
            _plcCheck.Start();
            _plcRead = new Thread(PLCRead);
            _plcRead.Start();
        }

        private void _plcH_ValueChangeEvent(string address, int value)
        {
            LogApp.Info($"PLC Changed: {address} = {value}");
            if (address == _config.BIReset && value == 1)
            {
                _plcH.SetInt(_config.WOResult, 0);
                _plcH.SetBit(_config.BOResult, false);
                _plcH.SetString(_config.WOCode, "", 50);
            }
            if (address == _config.BIAck && value == 1)
            {
                _plcH.SetInt(_config.WOResult, 0);
                _plcH.SetBit(_config.BOResult, false);
                _stopwatch.Stop();
                _stopwatch.Reset();
            }
            if (address == _config.BIStart && value == 1)
            {
                _plcH.SetInt(_config.WOResult, 0);
                _plcH.SetBit(_config.BOResult, false);
                _stopwatch.Stop();
                _stopwatch.Reset();
            }
        }

        public void Start()
        {
            _plcH.SetSlmp(_config);
            _plcH.Open();
        }
        public void Stop()
        {
            _plcThreadRunning = false;
            Thread.Sleep(100);
            if (_plcCheck != null && _plcCheck.IsAlive)
            {
                _plcCheck.Abort();
            }
            if (_plcRead != null && _plcRead.IsAlive)
            {
                _plcRead.Abort();
            }
            _plcH.Close();
        }
        private void Initial()
        {
            _plcH.RegisterInDevice(_config.BIStart, 0);
            _plcH.RegisterInDevice(_config.BIAck, 0);
            _plcH.RegisterInDevice(_config.BIReset, 0);
            _plcH.RegisterInDevice(_config.WIN, 0);
        }

        private void PLCAlive()
        {
            bool isOn = false;
            while (_plcThreadRunning)
            {

                isOn = !isOn;
                int result = _plcH.SetBit(_config.BOAlive, isOn);
                if (_stopwatch.Elapsed >= TimeSpan.FromSeconds(_config.TimeOut))
                {
                    LogApp.Warn($"Quá thời gian phản hồi ACK từ PLC, reset lại kết quả.");
                    _plcH.SetInt(_config.WOResult, 0);
                    _plcH.SetBit(_config.BOResult, false);
                    _stopwatch.Stop();
                    _stopwatch.Reset();
                }
                Thread.Sleep(_config.TimeAlive);
            }
        }
        private void PLCRead()
        {

            while (_plcThreadRunning)
            {
                if (_plcH.IsPlcConnected)
                {
                    int ret = _plcH.ReadDeviceRandom();
                }

                Thread.Sleep(10);
            }
        }
        public void ResponseAPI()
        {
            _plcH.SetInt(_config.WOResult, 1);
            _plcH.SetBit(_config.BOResult, true);
            _stopwatch.Restart();
        }
        #region EventHandle
        private void RequestApiEventHandle(APIMessage mess)
        {
            var handle = RequestApiEvent;
            if (handle != null)
            {
                handle(mess);
            }
        }
        #endregion
    }
}
