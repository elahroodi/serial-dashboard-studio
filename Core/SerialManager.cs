using System;
using System.IO.Ports;
using System.Text;

namespace SerialDebugPanel.Core
{
    public class SerialManager
    {
        private SerialPort _serialPort;
        private StringBuilder _rxBuffer = new();

        public event Action<string, string>? DataReceived; // کلید، مقدار
        public event Action<string>? LogOccurred;

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public string[] GetAvailablePorts() => SerialPort.GetPortNames();

        public void Connect(string portName, int baudRate)
        {
            Disconnect();

            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            _serialPort.DataReceived += Serial_DataReceived;
            _serialPort.Open();
            LogOccurred?.Invoke($"Connected to {portName} at {baudRate} bps.");
        }

        public void Disconnect()
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.DataReceived -= Serial_DataReceived;
                    _serialPort.Close();
                }
                _serialPort.Dispose();
                _serialPort = null;
                LogOccurred?.Invoke("Serial connection closed.");
            }
        }

        public void SendData(string message)
        {
            if (IsOpen && _serialPort != null)
            {
                try
                {
                    _serialPort.WriteLine(message); // اضافه کردن \n اتوماتیک به انتهای فرامین ارسالی
                }
                catch (Exception ex)
                {
                    LogOccurred?.Invoke($"TX Error: {ex.Message}");
                }
            }
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                string incoming = _serialPort.ReadExisting();
                _rxBuffer.Append(incoming);

                string bufferStr = _rxBuffer.ToString();
                int newlineIndex;

                while ((newlineIndex = bufferStr.IndexOf('\n')) >= 0)
                {
                    string rawLine = bufferStr.Substring(0, newlineIndex).Trim();
                    bufferStr = bufferStr.Substring(newlineIndex + 1);
                    _rxBuffer.Clear();
                    _rxBuffer.Append(bufferStr);

                    ParseAndNotify(rawLine);
                }
            }
            catch (Exception ex)
            {
                LogOccurred?.Invoke($"RX Error: {ex.Message}");
            }
        }

        private void ParseAndNotify(string rawData)
        {
            // فرمت ورودی: v1=55
            if (string.IsNullOrWhiteSpace(rawData)) return;

            int separatorIndex = rawData.IndexOf('=');
            if (separatorIndex > 0)
            {
                string key = rawData.Substring(0, separatorIndex).Trim();
                string value = rawData.Substring(separatorIndex + 1).Trim();
                DataReceived?.Invoke(key, value);
            }
        }
    }
}