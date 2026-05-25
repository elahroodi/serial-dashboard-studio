# 🚀 Universal Serial Dashboard (USD)

A powerful, highly-configurable, and interactive serial terminal for embedded systems and IoT. Design, build, and customize real-time control and monitoring dashboards on the fly! Written in C# with a modern WPF interface.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Framework](https://img.shields.io/badge/Framework-.NET%208.0%20%7C%20WPF-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)]()

---

## 🌟 Key Features

*   **Dynamic UI Generation:** No hardcoding! The entire dashboard is rendered on the fly from simple `.json` configuration files inside the `configs/` folder.
*   **Real-Time Data Visualization:** Monitor your hardware using high-performance charts, gauges, custom tables, LED registers, status alarms, and rich terminal logs.
*   **Interactive Controls:** Send precise commands using custom switches, sliders (integer/float), text/number inputs, dropdowns, color pickers, and synchronized control-loop faders.
*   **Zero-Overhead Parsing:** Ultra-fast serial stream parsing optimized for high-baudrate communication (up to 921600 bps and beyond) without UI freezing.
*   **WPF-Powered UX:** Modern, clean, and hardware-accelerated user interface.

---

## 🛠 Dynamic UI Architecture (`configs/config.json`)

The application automatically reads its UI structure from a JSON file. Below is a complete configuration template demonstrating all available components:

```json
{
  "controls": [
    { "type": "toggle", "label": "LED1", "command": "led1", "default": false },
    { "type": "slider", "label": "PWM Duty", "command": "pwm2", "min": 0, "max": 100, "defaultInt": 50, "unit": "%" },
    { "type": "slider", "label": "Voltage Set", "command": "volt", "unit": "V", "minFloat": 0, "maxFloat": 5, "defaultFloat": 3.3, "step": 0.1, "decimals": 1 },
    { "type": "button", "label": "Reset Device", "command": "reset", "buttonText": "Reset" },
    { "type": "input", "label": "Device Name", "command": "name", "defaultText": "STM32" },
    { "type": "select", "label": "Operation Mode", "command": "mode", "options": [{ "label": "Auto", "value": "auto" }, { "label": "Manual", "value": "manual" }], "defaultOption": "auto" },
    { "type": "number", "label": "Voltage Setpoint", "command": "voltage", "unit": "V", "minFloat": 0, "maxFloat": 5, "defaultFloat": 3.3 },
    { "type": "color", "label": "RGB LED", "command": "rgb", "defaultColor": "#FF0000" },
    { "type": "sync", "label": "Motor Speed", "command": "motor", "unit": "RPM", "variable": "motor_fb" }
  ],
  "monitors": [
    { "type": "log", "label": "Debug Log", "variable": "log" },
    { "type": "alarm", "label": "System Alarms Panel", "variable": "Alarm", "alarms": { "1": "Over Temp", "2": "Over Volt", "3": "Low Batt", "4": "Fan Fail", "5": "Sensor Err" } },
    { "type": "chart", "label": "ADC Value", "variable": "adc", "unit": "mV" },
    { "type": "text", "label": "Temperature", "variable": "temp", "unit": "°C", "warningThreshold": 45, "criticalThreshold": 60 },
    { "type": "led", "label": "GPIO Register Port A", "variable": "ready", "names": { "0": "PWR", "1": "RUN", "2": "ERR", "3": "TX", "4": "RX", "5": "U1", "6": "U2", "7": "U3" } },
    { "type": "gauge", "label": "Battery Level", "variable": "battery" },
    { "type": "table", "label": "IMU Data", "variable": "imu", "columns": [ "ax", "ay", "az" ] }
  ]
}

---

## 📡 Serial Protocol Specifications

Communication is entirely text-based (`ASCII`), using a simple `key=value\n` format.

### 1. Control Protocol (Host to Device)
When you interact with the UI, the application sends the following payloads over the serial port:

| UI Control | Type | Output Format (Sent over Serial) | Example |
| :--- | :--- | :--- | :--- |
| **Toggle** | Boolean | `[command]=[1 or 0]\n` | `led1=1\n` |
| **Slider (Int)** | Integer | `[command]=[integer]\n` | `pwm2=33\n` |
| **Slider (Float)** | Float | `[command]=[float]\n` | `volt=3.3\n` |
| **Button** | Trigger | `[command]=1\n` | `reset=1\n` |
| **Input** | String | `[command]=[text]\n` | `name=STM32\n` |
| **Select** | Dropdown | `[command]=[option_value]\n` | `mode=auto\n` |
| **Number** | Float/Int | `[command]=[value]\n` | `voltage=3.3\n` |
| **Color** | Hex String| `[command]=[HEX]\n` | `rgb=#FF0000\n` |
| **Sync** | Slider + FB| `[command]=[value]\n` | `motor=1500\n` |

### 2. Monitoring Protocol (Device to Host)
Your embedded microcontroller (STM32, ESP32, Arduino, PIC, etc.) must stream data in the following formats:

*   **System Log:**
    ```text
    log=System Initialized Successfully\n
    ```
*   **Alarms (Bitmask or ID activation):**
    ```text
    Alarm=3\n  (Triggers Alarm index 3: "Low Batt")
    ```
*   **Chart Stream:**
    ```text
    adc=2048\n
    ```
*   **Text Indicators (with warning levels):**
    ```text
    temp=36.4\n
    ```
*   **GPIO Register / Bitfield LEDs:**
    ```text
    ready=3\n  (Binary 00000011 -> Turns on PWR and RUN LEDs)
    ```
*   **Analog Gauge:**
    ```text
    battery=85\n
    ```
*   **Multi-axis Table Data:**
    ```text
    imu=0.12,-0.85,9.81\n  (Mapped automatically to columns ax, ay, az)
    ```

---

## 🚀 Getting Started

### Prerequisites
*   Windows 10 / 11
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or higher (to build)

### Installation & Run
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/universal-serial-dashboard.git
   cd universal-serial-dashboard
   ```
2. Place your UI configuration file in `configs/config.json`.
3. Build and Run:
   ```bash
   dotnet build
   dotnet run --project YourProjectName
   ```

---

## 🤝 Contributing
Contributions are welcome! If you want to add new widgets (like 3D attitude renderers, map widgets, etc.), please open an issue or submit a Pull Request.

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.