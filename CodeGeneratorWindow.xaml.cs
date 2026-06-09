// =========================================================
// FIXED + OPTIMIZED CODE GENERATOR
// سالم + سریع + بدون باگ return داخل parser
// پشتیبانی کامل monitor/control
// STM32 / Arduino / Generic
// =========================================================

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SerialDebugPanel
{
    public partial class CodeGeneratorWindow : Window
    {
        private readonly List<MonitorWidgetConfig> _monitors;
        private readonly List<ControlWidgetConfig> _controls;

        private string _currentCode = "";

        public CodeGeneratorWindow(
            List<MonitorWidgetConfig> monitors,
            List<ControlWidgetConfig> controls)
        {
            InitializeComponent();

            _monitors = monitors ?? new List<MonitorWidgetConfig>();
            _controls = controls ?? new List<ControlWidgetConfig>();

            Owner = Application.Current.MainWindow;
        }

        // =========================================================
        // UI
        // =========================================================

        private void ArduinoBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentCode = GenerateArduinoCode();

            CodePreview.Text = _currentCode;
            PlatformTitle.Text = "Arduino";
            SetStatus("Arduino code generated");
        }

        private void STM32Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentCode = GenerateSTM32Code();

            CodePreview.Text = _currentCode;
            PlatformTitle.Text = "STM32 HAL";
            SetStatus("STM32 code generated");
        }

        private void GenericBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentCode = GenerateGenericCode();

            CodePreview.Text = _currentCode;
            PlatformTitle.Text = "Generic C";
            SetStatus("Generic code generated");
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentCode))
                return;

            Clipboard.SetText(_currentCode);

            StatusText.Text = "Copied";
            StatusText.Foreground =
                new SolidColorBrush(Color.FromRgb(34, 197, 94));
        }

        private void SetStatus(string txt)
        {
            StatusText.Text = txt;
            StatusText.Foreground =
                new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }

        // =========================================================
        // ARDUINO
        // =========================================================

        private string GenerateArduinoCode()
        {
            var sb = new StringBuilder();

            sb.AppendLine("#include <Arduino.h>");
            sb.AppendLine("#include <string.h>");
            sb.AppendLine("#include <stdlib.h>");
            sb.AppendLine();

            AppendCommonDefines(sb);

            sb.AppendLine("char rx_buf[RX_BUF_SIZE];");
            sb.AppendLine("uint16_t rx_idx = 0;");
            sb.AppendLine("uint32_t lastSend = 0;");
            sb.AppendLine();

            AppendVariableDeclarations(
                sb,
                "float",
                "int32_t",
                "char",
                "bool");

            AppendControlDeclarations(sb);

            AppendArduinoSendFunctions(sb);

            AppendArduinoParser(sb);

            sb.AppendLine("void setup()");
            sb.AppendLine("{");
            sb.AppendLine("    Serial.begin(SERIAL_BAUDRATE);");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("void loop()");
            sb.AppendLine("{");
            sb.AppendLine("    processSerial();");
            sb.AppendLine();

            sb.AppendLine("    if(millis() - lastSend >= SEND_INTERVAL_MS)");
            sb.AppendLine("    {");
            sb.AppendLine("        sendAll();");
            sb.AppendLine("        lastSend = millis();");
            sb.AppendLine("    }");

            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        // =========================================================
        // STM32
        // =========================================================

        private string GenerateSTM32Code()
        {
            var sb = new StringBuilder();

            sb.AppendLine("#ifndef __DEBUG_INTERFACE_H__"); 
            sb.AppendLine("#define __DEBUG_INTERFACE_H__");
            sb.AppendLine(); sb.AppendLine("#ifdef __cplusplus");
            sb.AppendLine("extern \"C\" {"); 
            sb.AppendLine("#endif");
            sb.AppendLine();

            sb.AppendLine("#include \"main.h\"");
            sb.AppendLine("#include <stdio.h>");
            sb.AppendLine("#include <string.h>");
            sb.AppendLine("#include <stdlib.h>");
            sb.AppendLine("#include <stdbool.h>");
            sb.AppendLine("#include <stdarg.h>");
            sb.AppendLine();

            AppendCommonDefines(sb);

            sb.AppendLine("extern UART_HandleTypeDef DEBUG_UART;");
            sb.AppendLine();

            sb.AppendLine("static char rx_buf[RX_BUF_SIZE];");
            sb.AppendLine("static char tx_buf[TX_BUF_SIZE];");
            sb.AppendLine("static uint8_t rx_char;");
            sb.AppendLine("static uint16_t rx_idx = 0;");
            sb.AppendLine("static uint32_t lastSend = 0;");
            sb.AppendLine();

            sb.AppendLine("void uart_send(const char* s)");
            sb.AppendLine("{");
            sb.AppendLine("    HAL_UART_Transmit(&DEBUG_UART,");
            sb.AppendLine("        (uint8_t*)s,");
            sb.AppendLine("        strlen(s),");
            sb.AppendLine("        100);");
            sb.AppendLine("}");
            sb.AppendLine();

            AppendVariableDeclarations(
                sb,
                "float",
                "int32_t",
                "char",
                "bool");

            AppendLogFunctions(sb);

            AppendControlDeclarations(sb);

         

            AppendSTM32SendFunctions(sb);

            AppendSTM32Parser(sb);

            sb.AppendLine("void Debug_Init(void)");
            sb.AppendLine("{");
            sb.AppendLine("    HAL_UART_Receive_IT(&DEBUG_UART, &rx_char, 1);");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("void Debug_Update(void)");
            sb.AppendLine("{");

            foreach (var c in _controls)
            {
                if (c.Type == "button")
                {
                    sb.AppendLine($"    if({c.Command}_pending)");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        {c.Command}_pending = false;");
                    sb.AppendLine($"        {c.Command}_callback();");
                    sb.AppendLine("    }");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    if(HAL_GetTick() - lastSend >= SEND_INTERVAL_MS)");
            sb.AppendLine("    {");
            sb.AppendLine("        sendAll();");
            sb.AppendLine("        lastSend = HAL_GetTick();");
            sb.AppendLine("    }");

            sb.AppendLine("}");

            sb.AppendLine();

            sb.AppendLine("/*");
            sb.AppendLine("=========================================================");
            sb.AppendLine("Example usage in main.c");
            sb.AppendLine();
            sb.AppendLine("#define DEBUG_UART huart1");
            sb.AppendLine();
            sb.AppendLine("void HAL_UART_RxCpltCallback(UART_HandleTypeDef *huart)");
            sb.AppendLine("{");
            sb.AppendLine("    // Debug interface RX handler");
            sb.AppendLine("    Debug_RxCallback(huart);");
            sb.AppendLine();
            sb.AppendLine("    // Your other UART handlers here");
            sb.AppendLine("}");
            sb.AppendLine("=========================================================");
            sb.AppendLine("*/");
            sb.AppendLine();

            sb.AppendLine("#ifdef __cplusplus");
            sb.AppendLine("}");
            sb.AppendLine("#endif");
            sb.AppendLine();

            sb.AppendLine("#endif");

            return sb.ToString();
        }

        // =========================================================
        // GENERIC
        // =========================================================

        private string GenerateGenericCode()
        {
            var sb = new StringBuilder();

            sb.AppendLine("#include <stdint.h>");
            sb.AppendLine("#include <stdbool.h>");
            sb.AppendLine("#include <stdio.h>");
            sb.AppendLine("#include <string.h>");
            sb.AppendLine("#include <stdlib.h>");
            sb.AppendLine();

            AppendCommonDefines(sb);

            sb.AppendLine("extern void uart_send(const char* s);");
            sb.AppendLine("extern int uart_read(void);");
            sb.AppendLine("extern uint32_t millis(void);");
            sb.AppendLine();

            sb.AppendLine("static char rx_buf[RX_BUF_SIZE];");
            sb.AppendLine("static char tx_buf[TX_BUF_SIZE];");
            sb.AppendLine("static uint16_t rx_idx = 0;");
            sb.AppendLine("static uint32_t lastSend = 0;");
            sb.AppendLine();

            AppendVariableDeclarations(
                sb,
                "float",
                "int32_t",
                "char",
                "bool");

            AppendControlDeclarations(sb);

            AppendGenericSendFunctions(sb);

            AppendGenericParser(sb);

            sb.AppendLine("void debug_update(void)");
            sb.AppendLine("{");
            sb.AppendLine("    processSerial();");
            sb.AppendLine();

            sb.AppendLine("    if(millis() - lastSend >= SEND_INTERVAL_MS)");
            sb.AppendLine("    {");
            sb.AppendLine("        sendAll();");
            sb.AppendLine("        lastSend = millis();");
            sb.AppendLine("    }");

            sb.AppendLine("}");

       
         
            return sb.ToString();
        }

        // =========================================================
        // COMMON
        // =========================================================

        private void AppendCommonDefines(StringBuilder sb)
        {
            sb.AppendLine("#define RX_BUF_SIZE 128");
            sb.AppendLine("#define TX_BUF_SIZE 256");
            sb.AppendLine("#define SEND_INTERVAL_MS 500");
            sb.AppendLine();
        }

        // =========================================================
        // VARIABLES
        // =========================================================

        private void AppendVariableDeclarations(
            StringBuilder sb,
            string floatType,
            string intType,
            string charType,
            string boolType)
        {
            foreach (var m in _monitors)
            {
                if (string.IsNullOrWhiteSpace(m.Variable))
                    continue;

                switch (m.Type)
                {
                    case "text":
                    case "chart":
                        sb.AppendLine($"{floatType} {m.Variable} = 0;");
                        break;

                    case "gauge":
                        sb.AppendLine($"{intType} {m.Variable} = 0;");
                        break;

                    case "alarm":
                        sb.AppendLine(
                            $"{boolType} {m.Variable}[{Math.Max(1, m.Alarms?.Count ?? 1)}] = {{0}};");
                        break;

                    case "led":
                        sb.AppendLine(
                            $"{boolType} {m.Variable}[{Math.Max(1, m.Names?.Count ?? 1)}] = {{0}};");
                        break;

                    case "table":

                        if (m.Columns != null)
                        {
                            foreach (var c in m.Columns)
                            {
                                sb.AppendLine($"{floatType} {c} = 0;");
                            }
                        }

                        break;
                }
            }

            sb.AppendLine();
        }

        // =========================================================
        // CONTROLS
        // =========================================================

        private void AppendControlDeclarations(StringBuilder sb)
        {
            foreach (var c in _controls)
            {
                if (string.IsNullOrWhiteSpace(c.Command))
                    continue;

                switch (c.Type)
                {
                    case "toggle":

                        sb.AppendLine(
                            $"bool {c.Command} = {(c.Default == true ? "true" : "false")};");

                        break;

                    case "slider":
                    case "number":

                        if (c.DefaultFloat.HasValue)
                            sb.AppendLine(
                                $"float {c.Command} = {c.DefaultFloat.Value}f;");
                        else
                            sb.AppendLine(
                                $"int32_t {c.Command} = {c.DefaultInt ?? 0};");

                        break;

                    case "button":
                        sb.AppendLine($"volatile bool {c.Command}_pending = false;");
                        sb.AppendLine();
                        sb.AppendLine($"void {c.Command}_callback(void);");
                        sb.AppendLine();
                       break;

                    case "input":

                        sb.AppendLine(
                            $"char {c.Command}[32] = \"{c.DefaultText ?? ""}\";");

                        break;

                    case "select": if (c.Options != null && c.Options.Count > 0) { string enumName = $"{c.Command}_t";
                            sb.AppendLine($"typedef enum"); 
                            sb.AppendLine("{");
                            for (int i = 0; i < c.Options.Count; i++)
                            {
                                var opt = c.Options[i];
                                string enumItem = $"{c.Command.ToUpper()}_{opt.Value.ToUpper()}";
                                sb.AppendLine($" {enumItem} = {i},");
                            }
                            sb.AppendLine($"}} {enumName};");
                            sb.AppendLine();
                            string defaultEnum = $"{c.Command.ToUpper()}_{c.DefaultOption?.ToUpper()}";
                            sb.AppendLine($"{enumName} {c.Command} = {defaultEnum};");
                        }
                        break;

                    case "color":

                        sb.AppendLine(
                            $"uint32_t {c.Command} = 0x{c.DefaultColor?.TrimStart('#') ?? "000000"};");

                        break;

                    case "sync":

                        sb.AppendLine(
                            $"float {c.Command}_set = 0;");

                        break;
                }
            }

            sb.AppendLine();
        }

        // =========================================================
        // SEND FUNCTIONS
        // =========================================================

        private void AppendArduinoSendFunctions(StringBuilder sb)
        {
            sb.AppendLine("void sendVar(const char* name)");
            sb.AppendLine("{");

            foreach (var m in _monitors)
            {
                GenerateArduinoSendCase(sb, m);
            }

            sb.AppendLine("}");
            sb.AppendLine();

            AppendSendAll(sb);
        }

        private void AppendSTM32SendFunctions(StringBuilder sb)
        {
            sb.AppendLine("static void sendVar(const char* name)");
            sb.AppendLine("{");

            foreach (var m in _monitors)
            {
                GenerateSTM32SendCase(sb, m);
            }

            sb.AppendLine("}");
            sb.AppendLine();

            AppendSendAll(sb);
        }

        private void AppendGenericSendFunctions(StringBuilder sb)
        {
            sb.AppendLine("static void sendVar(const char* name)");
            sb.AppendLine("{");

            foreach (var m in _monitors)
            {
                GenerateGenericSendCase(sb, m);
            }

            sb.AppendLine("}");
            sb.AppendLine();

            AppendSendAll(sb);
        }

        private void AppendLogFunctions(StringBuilder sb)
        {
            foreach (var m in _monitors)
            {
                if (m.Type != "log")
                    continue;

                string func =
                    m.Variable
                     .Replace(" ", "_")
                     .Replace("-", "_");

                sb.AppendLine($"void {func}_Printf(const char* fmt, ...)");
                sb.AppendLine("{");
                sb.AppendLine("    va_list args;");
                sb.AppendLine("    va_start(args, fmt);");
                sb.AppendLine();

                sb.AppendLine("    int len = snprintf(tx_buf,");
                sb.AppendLine("                       TX_BUF_SIZE,");
                sb.AppendLine($"                       \"{m.Variable}=\");");
                sb.AppendLine();

                sb.AppendLine("    vsnprintf(tx_buf + len,");
                sb.AppendLine("              TX_BUF_SIZE - len,");
                sb.AppendLine("              fmt,");
                sb.AppendLine("              args);");
                sb.AppendLine();

                sb.AppendLine("    va_end(args);");
                sb.AppendLine();

                sb.AppendLine("    strncat(tx_buf, \"\\r\\n\", TX_BUF_SIZE - strlen(tx_buf) - 1);");
                sb.AppendLine();

                sb.AppendLine("    uart_send(tx_buf);");
                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        private void AppendSendAll(StringBuilder sb) 
        {
            sb.AppendLine("static void sendAll(void)");
            sb.AppendLine("{");

            foreach (var m in _monitors) {
                if (m.Type == "log") 
                    continue;
                if (!string.IsNullOrWhiteSpace(m.Variable)) 
                    sb.AppendLine($" sendVar(\"{m.Variable}\");");
            } 
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // =========================================================
        // PARSER
        // =========================================================

        private void AppendArduinoParser(StringBuilder sb)
        {
            GenerateParser(sb, false, false);
        }

        private void AppendSTM32Parser(StringBuilder sb)
        {
            GenerateParser(sb, true, false);
        }

        private void AppendGenericParser(StringBuilder sb)
        {
            GenerateParser(sb, false, true);
        }

        private void GenerateParser(
    StringBuilder sb,
    bool stm32,
    bool generic)
        {
            if (stm32)
            {
                
                sb.AppendLine("void Debug_RxCallback(UART_HandleTypeDef *huart)");
                sb.AppendLine("{");
                sb.AppendLine("    if(huart != &DEBUG_UART)");
                sb.AppendLine("        return;");
                sb.AppendLine();
                sb.AppendLine("    char c = rx_char;");
            }
            else
            {
                sb.AppendLine("void processSerial()");
                sb.AppendLine("{");

                if (generic)
                {
                    sb.AppendLine("    int ci;");
                    sb.AppendLine("    while((ci = uart_read()) != -1)");
                    sb.AppendLine("    {");
                    sb.AppendLine("        char c = (char)ci;");
                }
                else
                {
                    sb.AppendLine("    while(Serial.available())");
                    sb.AppendLine("    {");
                    sb.AppendLine("        char c = Serial.read();");
                }
            }

            sb.AppendLine();

            sb.AppendLine("        if(c == '\\r')");
            sb.AppendLine("            goto exit_irq;");
            sb.AppendLine();

            sb.AppendLine("        if(c == '\\n')");
            sb.AppendLine("        {");
            sb.AppendLine("            rx_buf[rx_idx] = 0;");
            sb.AppendLine();

            sb.AppendLine("            if(strcmp(rx_buf, \"getall\") == 0)");
            sb.AppendLine("            {");
            sb.AppendLine("                sendAll();");
            sb.AppendLine("            }");
            sb.AppendLine("            else");
            sb.AppendLine("            {");
            sb.AppendLine("                char* eq = strchr(rx_buf, '=');");
            sb.AppendLine();

            sb.AppendLine("                if(eq)");
            sb.AppendLine("                {");
            sb.AppendLine("                    *eq = 0;");
            sb.AppendLine();

            sb.AppendLine("                    char* key = rx_buf;");
            sb.AppendLine("                    char* val = eq + 1;");
            sb.AppendLine();

            foreach (var c in _controls)
            {
                if (string.IsNullOrWhiteSpace(c.Command))
                    continue;

                sb.AppendLine(
                    $"                    if(strcmp(key, \"{c.Command}\") == 0)");
                sb.AppendLine("                    {");

                switch (c.Type)
                {
                    case "toggle":

                        sb.AppendLine(
                            $"                        {c.Command} = atoi(val) != 0;");
                        break;

                    case "slider":
                    case "number":

                        if (c.DefaultFloat.HasValue)
                            sb.AppendLine(
                                $"                        {c.Command} = atof(val);");
                        else
                            sb.AppendLine(
                                $"                        {c.Command} = atoi(val);");

                        break;

                    case "button":

                        sb.AppendLine(
                            $"                        {c.Command}_pending = true;");

                        break;

                    case "input":
                        sb.AppendLine($" strncpy({c.Command}, val, sizeof({c.Command}) - 1);");
                        sb.AppendLine($" {c.Command}[sizeof({c.Command}) - 1] = 0;");
                        break;

                    case "select":

                        if (c.Options != null && c.Options.Count > 0)
                        {
                            for (int i = 0; i < c.Options.Count; i++)
                            {
                                var opt = c.Options[i];

                                string enumItem =
                                    $"{c.Command.ToUpper()}_{opt.Value.ToUpper()}";

                                if (i == 0)
                                    sb.AppendLine(
                                        $"                        if(strcmp(val, \"{opt.Value}\") == 0)");

                                else
                                    sb.AppendLine(
                                        $"                        else if(strcmp(val, \"{opt.Value}\") == 0)");

                                sb.AppendLine(
                                    $"                            {c.Command} = {enumItem};");
                            }
                        }

                        break;

                    case "color":

                        sb.AppendLine("                        if(val[0] == '#')");
                        sb.AppendLine("                            val++;");

                        sb.AppendLine(
                            $"                        {c.Command} = strtoul(val, NULL, 16);");

                        break;

                    case "sync":

                        sb.AppendLine(
                            $"                        {c.Command}_set = atof(val);");

                        break;
                }

                sb.AppendLine("                    }");
            }

            sb.AppendLine("                }");
            sb.AppendLine("            }");

            sb.AppendLine();

            sb.AppendLine("            rx_idx = 0;");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine("            if(rx_idx < RX_BUF_SIZE - 1)");
            sb.AppendLine("                rx_buf[rx_idx++] = c;");
            sb.AppendLine("            else");
            sb.AppendLine("                rx_idx = 0;");
            sb.AppendLine("        }");

            if (stm32)
            {
                sb.AppendLine();
                sb.AppendLine("exit_irq:");
                sb.AppendLine("    HAL_UART_Receive_IT(&DEBUG_UART, &rx_char, 1);");
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine("    }");
                sb.AppendLine("}");
            }

            sb.AppendLine();
        }

        // =========================================================
        // SEND CASES
        // =========================================================

        private void GenerateArduinoSendCase(
            StringBuilder sb,
            MonitorWidgetConfig m)
        {
            if (string.IsNullOrWhiteSpace(m.Variable))
                return;

            sb.AppendLine($"    if(strcmp(name, \"{m.Variable}\") == 0)");
            sb.AppendLine("    {");

            switch (m.Type)
            {
                case "text":
                case "chart":
                case "gauge":

                    sb.AppendLine($"        Serial.print(\"{m.Variable}=\");");
                    sb.AppendLine($"        Serial.println({m.Variable});");

                    break;

              
            }

            sb.AppendLine("        return;");
            sb.AppendLine("    }");
        }

        private void GenerateSTM32SendCase(
     StringBuilder sb,
     MonitorWidgetConfig m)
        {
            if (string.IsNullOrWhiteSpace(m.Variable))
                return;
            if (m.Type == "log")
                return;

            sb.AppendLine($"    if(strcmp(name, \"{m.Variable}\") == 0)");
            sb.AppendLine("    {");

            switch (m.Type)
            {
                case "text":
                case "chart":

                    sb.AppendLine(
                        $"        snprintf(tx_buf, TX_BUF_SIZE, \"{m.Variable}=%.2f\\r\\n\", (double){m.Variable});");

                    sb.AppendLine("        uart_send(tx_buf);");
                    break;

                case "gauge":

                    sb.AppendLine(
                        $"        snprintf(tx_buf, TX_BUF_SIZE, \"{m.Variable}=%ld\\r\\n\", (long){m.Variable});");

                    sb.AppendLine("        uart_send(tx_buf);");
                    break;

                case "alarm":

                    sb.AppendLine($"        int first = 1;");
                    sb.AppendLine($"        int pos = snprintf(tx_buf, TX_BUF_SIZE, \"{m.Variable}=\");");

                    int alarmCount = Math.Max(1, m.Alarms?.Count ?? 1);

                    sb.AppendLine($"        for(int i=0;i<{alarmCount};i++)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            if({m.Variable}[i])");
                    sb.AppendLine("            {");
                    sb.AppendLine("                if(!first)");
                    sb.AppendLine("                    pos += snprintf(tx_buf + pos, TX_BUF_SIZE - pos, \",\");");

                    sb.AppendLine("                pos += snprintf(tx_buf + pos, TX_BUF_SIZE - pos, \"%d\", i + 1);");
                    sb.AppendLine("                first = 0;");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");

                    sb.AppendLine($"        snprintf(tx_buf + strlen(tx_buf), TX_BUF_SIZE - strlen(tx_buf), \"\\r\\n\");");
                    sb.AppendLine("        uart_send(tx_buf);");
                    break;

                case "led":

                    int ledCount = Math.Max(1, m.Names?.Count ?? 1);

                    sb.AppendLine($"        int pos2 = snprintf(tx_buf, TX_BUF_SIZE, \"{m.Variable}=\");");

                    sb.AppendLine($"        for(int i=0;i<{ledCount};i++)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            pos2 += snprintf(tx_buf + pos2, TX_BUF_SIZE - pos2, \"%d\", {m.Variable}[i] ? 1 : 0);");

                    sb.AppendLine($"            if(i < {ledCount} - 1)");
                    sb.AppendLine("                pos2 += snprintf(tx_buf + pos2, TX_BUF_SIZE - pos2, \",\");");

                    sb.AppendLine("        }");

                    sb.AppendLine($"        snprintf(tx_buf + strlen(tx_buf), TX_BUF_SIZE - strlen(tx_buf), \"\\r\\n\");");

                    sb.AppendLine("        uart_send(tx_buf);");
                    break;

                case "table":

                    if (m.Columns != null && m.Columns.Count > 0)
                    {
                        sb.Append($"        snprintf(tx_buf, TX_BUF_SIZE, \"{m.Variable}=%.2f");

                        for (int i = 1; i < m.Columns.Count; i++)
                            sb.Append(",%.2f");

                        sb.AppendLine("\\r\\n\", ");

                        for (int i = 0; i < m.Columns.Count; i++)
                        {
                            string comma = (i < m.Columns.Count - 1) ? "," : "";

                            sb.AppendLine(
                                $"            (double){m.Columns[i]}{comma}");
                        }

                        sb.AppendLine("        );");
                        sb.AppendLine("        uart_send(tx_buf);");
                    }

                    break;
            }

            sb.AppendLine("        return;");
            sb.AppendLine("    }");
        }

        private void GenerateGenericSendCase(
            StringBuilder sb,
            MonitorWidgetConfig m)
        {
            GenerateSTM32SendCase(sb, m);
        }
    }

    // =========================================================
    // DATA CLASSES
    // =========================================================

    public class MonitorWidgetConfig
    {
        public string Type { get; set; }
        public string Label { get; set; }
        public string Variable { get; set; }
        public string Unit { get; set; }

        public double? WarningThreshold { get; set; }
        public double? CriticalThreshold { get; set; }

        public Dictionary<string, string> Alarms { get; set; }
        public Dictionary<string, string> Names { get; set; }

        public List<string> Columns { get; set; }
    }

    public class ControlWidgetConfig
    {
        public string Type { get; set; }
        public string Label { get; set; }
        public string Command { get; set; }

        public bool? Default { get; set; }

        public int? Min { get; set; }
        public int? Max { get; set; }

        public int? DefaultInt { get; set; }

        public string Unit { get; set; }

        public double? MinFloat { get; set; }
        public double? MaxFloat { get; set; }

        public double? DefaultFloat { get; set; }

        public double? Step { get; set; }
        public int? Decimals { get; set; }

        public string ButtonText { get; set; }

        public string DefaultText { get; set; }

        public List<OptionConfig> Options { get; set; }

        public string DefaultOption { get; set; }

        public string DefaultColor { get; set; }

        public string Variable { get; set; }
    }

    public class OptionConfig
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }
}