# 🚀 Universal Serial Dashboard (USD)

A powerful, highly-configurable, and interactive serial terminal for embedded systems and IoT. **Design, build, customize, and edit** real-time control and monitoring dashboards on the fly using a fully visual creator interface—no manual JSON editing required!

Written in **C# / WPF (.NET 8)** with hardware acceleration and ultra-low latency parsing.

---

## 📸 Dashboards & Interface Preview

| Main Operating Dashboard | Visual UI Designer & Configurator |
| :---: | :---: |
| <img src="repo/main_dashboard.png" width="100%" alt="Main Dashboard Placeholder"/> | <img src="repo/configurator_window.png" width="100%" alt="UI Configurator Window Placeholder"/> |
| *Active control sliders, real-time gauges, charts, and table logs.* | *The live visual WYSIWYG editor loaded with custom controls.* |

---

## 🌟 Key Features

*   **⚡ On-The-Fly Visual Layout Builder:** Use the built-in **Configurator Tool** to add, remove, and edit widgets on a live list. No need to touch JSON files manually.
*   **🔄 Live Dynamic UI Parsing:** Double-click any active control or monitor in the designer to load its entire config back into the form, modify parameters, and update instantly.
*   **📈 Advanced Monitoring Widgets:**
    *   *Real-time charts* for ADC and sensor streams.
    *   *Radial & Linear Gauges* for battery, speed, or level tracking.
    *   *GPIO Bit-Mapped LEDs* to preview raw register states (e.g., active high/low bits).
    *   *Fault Alarms* triggered directly by microcontrollers using bitmasks or error codes.
    *   *Multi-column Tables* designed specifically for high-speed IMU ($a_x, a_y, a_z$) data.
*   **🎛 Extensive Hardware Controls:** Toggle switches, Integer/Float sliders (with customizable decimal step resolution), dropdown selectors, custom command triggers, and color pickers.
*   **🔗 Closed-Loop Sync Widgets:** Cross-linked widgets (`sync`) that automatically map outgoing commands with incoming variables for tight real-time control feedback loops.

---

## 🏗 System Architecture & Configuration Flow

```
┌────────────────────────┐         Reads / Saves         ┌────────────────────────┐
│  WPF Configurator UI   │ ────────────────────────────> │  configs/devices.json  │
│  (Add/Edit/Delete List)│ <──────────────────────────── │   (JSON Configuration) │
└────────────────────────┘                               └────────────────────────┘
            │                                                         │
            │ Modifies Layout                                         │ Renders Dashboard
            ▼                                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           Universal Serial Dashboard                            │
└─────────────────────────────────────────────────────────────────────────────────┘
         ▲                                                                 │
         │ Stream Parser (key=value\n)                                     │ Serial Command
         │                                                                 ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        Target Hardware (STM32, ESP32, etc.)                     │
└─────────────────────────────────────────────────────────────────────────────────┘

---

## 📡 Serial Protocol Reference

The terminal uses a robust, high-frequency, lightweight communication protocol over ASCII: `key=value\n`.

### 1. Host Control Transmission (WPF ➔ Hardware)

When interacting with dashboard widgets, the host controller writes ASCII frames directly to the Serial Ring Buffer:

| Component Type | Sub-type / Data Type | Serial Output Format | Example Payload |
| :--- | :--- | :--- | :--- |
| **Toggle** | Boolean State | `[command]=[1 or 0]\n` | `led1=1\n` |
| **Slider** | Integer Range | `[command]=[integer]\n` | `pwm2=33\n` |
| **Slider / Number** | Floating Point | `[command]=[float]\n` | `volt=3.30\n` |
| **Button** | Trigger Pulse | `[command]=1\n` | `reset=1\n` |
| **Input** | Custom Text | `[command]=[text]\n` | `name=STM32_NodeA\n` |
| **Select** | Dropdown Enum | `[command]=[value]\n` | `mode=manual\n` |
| **Color** | HEX Color Code | `[command]=[hex_code]\n` | `rgb=#FF0000\n` |
| **Sync** | Control Loop Fader | `[command]=[value]\n` | `motor=1200\n` |

### 2. Device Streaming Packet Parser (Hardware ➔ WPF)

Stream outputs from your microcontroller using serial print statements:

*   **Log Feed:**
    ```text
    log=System Core Clock: 168MHz\n
    ```
*   **Warning Alarms (Bitwise & Indexed Map):**
    ```text
    Alarm=3\n  (Triggers index 3 -> "Low Batt" inside UI panel)
    ```
*   **ADC Chart Stream:**
    ```text
    adc=2345\n
    ```
*   **Text Indicator with Warning Thresholds:**
    ```text
    temp=48.2\n  (Changes background to yellow warning threshold)
    ```
*   **LED Bits Port Register Map:**
    ```text
    ready=3\n  (Binary 00000011 -> Activates PWR and RUN pins)
    ```
*   **Analog Gauge:**
    ```text
    battery=89\n
    ```
*   **IMU Matrix Tables:**
    ```text
    imu=-0.04,1.01,0.08\n  (Maps directly into ax, ay, az columns)
    ```

---

## 🛠 Project Build & Structure

### Prerequisites
*   **OS:** Windows 10 / 11
*   **Runtime:** [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later
*   **IDE:** Visual Studio 2022

### Repository Structure
text
├── SerialDebugPanel.sln
├── repo/                      <-- Place screenshots here
│   ├── main_dashboard.png
│   └── configurator_window.png
├── Configs/                   <-- Dynamic JSON layouts live here
│   └── device_config.json
└── Source/
    ├── ConfiguratorWindow.xaml      <-- UI Builder & Live Editor View
    ├── ConfiguratorWindow.xaml.cs   <-- Controller logic, JSON Engine
    └── Models/
        ├── ControlItem.cs           <-- OOP structure for interactive commands
        └── MonitorItem.cs           <-- OOP structure for indicator sensors

### Running the Application
1. Clone the project:
   ```bash
   git clone https://github.com/yourusername/universal-serial-dashboard.git
   ```
2. Navigate to the solution folder and compile:
   ```bash
   dotnet build
   ```
3. Run the executable:
   ```bash
   dotnet run --project SerialDebugPanel
   ```

---

## 🤝 Contributing
Have ideas for complex widgets (like 3D Attitude indicators or customizable mapping tools)?
1. Fork the project.
2. Create your Feature Branch (`git checkout -b feature/NewWidget`).
3. Commit your changes and Open a Pull Request!

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
