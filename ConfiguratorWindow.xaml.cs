using DeviceConfigurator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SerialDebugPanel
{
    public partial class ConfiguratorWindow : Window
    {
        private ObservableCollection<ControlItem> controlList = new ObservableCollection<ControlItem>();
        private ObservableCollection<MonitorItem> monitorList = new ObservableCollection<MonitorItem>();

        // اصلاح آدرس فولدر به پوشه Configs
        private readonly string _configsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");

        // متغیرهای سراسری در سطح کلاس برای کنترل حالت ویرایش
        private object _editingItem = null; // نگه داشتن آبجکت در حال ویرایش
        private bool _isEditMode = false;   // آیا در حالت ویرایش هستیم؟

        public ConfiguratorWindow()
        {
            InitializeComponent();
            dgControls.ItemsSource = controlList;
            dgMonitors.ItemsSource = monitorList;

            if (!Directory.Exists(_configsFolder))
            {
                Directory.CreateDirectory(_configsFolder);
            }

            PopulateTypes();
            RefreshConfigFilesList();
        }

        // اسکن و نمایش فایل‌های موجود در پوشه Configs برای ویرایش
        private void RefreshConfigFilesList()
        {
            try
            {
                cmbConfigFiles.Items.Clear();
                if (Directory.Exists(_configsFolder))
                {
                    var files = Directory.GetFiles(_configsFolder, "*.json");
                    foreach (var file in files)
                    {
                        cmbConfigFiles.Items.Add(Path.GetFileName(file));
                    }
                }
                if (cmbConfigFiles.Items.Count > 0)
                {
                    cmbConfigFiles.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load existing configs list: {ex.Message}");
            }
        }

        private void PopulateTypes()
        {
            if (cmbType == null) return;
            cmbType.Items.Clear();

            if (cmbCategory.SelectedIndex == 0) // Controls
            {
                cmbType.Items.Add("toggle");
                cmbType.Items.Add("slider");
                cmbType.Items.Add("button");
                cmbType.Items.Add("input");
                cmbType.Items.Add("select");
                cmbType.Items.Add("number");
                cmbType.Items.Add("color");
                cmbType.Items.Add("sync");
            }
            else // Monitors
            {
                cmbType.Items.Add("text");
                cmbType.Items.Add("led");
                cmbType.Items.Add("gauge");
                cmbType.Items.Add("chart");
                cmbType.Items.Add("log");
                cmbType.Items.Add("table");
                cmbType.Items.Add("alarm");
            }
            cmbType.SelectedIndex = 0;
        }

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateTypes();
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbType.SelectedItem == null) return;

            string selectedType = cmbType.SelectedItem.ToString();

            panelCommand.Visibility = cmbCategory.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            panelVariable.Visibility = cmbCategory.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

            panelIntParams.Visibility = Visibility.Collapsed;
            panelFloatParams.Visibility = Visibility.Collapsed;
            panelCommonParams.Visibility = Visibility.Collapsed;
            panelTextButtonParams.Visibility = Visibility.Collapsed;
            panelDropdownParams.Visibility = Visibility.Collapsed;
            panelAlarmsParams.Visibility = Visibility.Collapsed;
            panelTableParams.Visibility = Visibility.Collapsed;

            if (selectedType == "slider")
            {
                panelIntParams.Visibility = Visibility.Visible;
                panelFloatParams.Visibility = Visibility.Visible;
                panelCommonParams.Visibility = Visibility.Visible;
            }
            else if (selectedType == "number")
            {
                panelFloatParams.Visibility = Visibility.Visible;
                panelCommonParams.Visibility = Visibility.Visible;
            }
            else if (selectedType == "button")
            {
                panelTextButtonParams.Visibility = Visibility.Visible;
            }
            else if (selectedType == "input")
            {
                panelTextButtonParams.Visibility = Visibility.Visible;
            }
            else if (selectedType == "select")
            {
                panelDropdownParams.Visibility = Visibility.Visible;
            }
            else if (selectedType == "sync")
            {
                panelVariable.Visibility = Visibility.Visible;
                panelCommonParams.Visibility = Visibility.Visible;
            }
            else if (selectedType == "text" || selectedType == "gauge" || selectedType == "chart")
            {
                panelCommonParams.Visibility = Visibility.Visible;
            }
            else if (selectedType == "alarm" || selectedType == "led")
            {
                panelAlarmsParams.Visibility = Visibility.Visible;
            }
            else if (selectedType == "table")
            {
                panelTableParams.Visibility = Visibility.Visible;
            }
        }

        // عملکرد بارگذاری یک فایل جهت ویرایش تعاملی
        private void BtnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            if (cmbConfigFiles.SelectedItem == null) return;

            string fileName = cmbConfigFiles.SelectedItem.ToString();
            string filePath = Path.Combine(_configsFolder, fileName);

            try
            {
                string jsonContent = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var root = JsonSerializer.Deserialize<RootConfig>(jsonContent, options);

                if (root != null)
                {
                    controlList.Clear();
                    monitorList.Clear();

                    foreach (var control in root.Controls)
                    {
                        controlList.Add(control);
                    }

                    foreach (var monitor in root.Monitors)
                    {
                        // تبدیل فیلدهای قدیمی به فرمت جدید در صورت نیاز حین بارگذاری
                        if (monitor.Type == "led" && monitor.Names == null && monitor.Alarms != null)
                        {
                            monitor.Names = monitor.Alarms;
                            monitor.Alarms = null;
                        }
                        monitorList.Add(monitor);
                    }

                    MessageBox.Show($"Configuration '{fileName}' loaded successfully for editing!", "Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse JSON file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ساخت نمونه ControlItem از روی اطلاعات کنونی فرم چپ
        private ControlItem CreateControlItemFromForm()
        {
            string selectedType = cmbType.SelectedItem.ToString();
            var newItem = new ControlItem
            {
                Type = selectedType,
                Label = txtLabel.Text,
                Command = txtCommand.Text,
                Unit = string.IsNullOrWhiteSpace(txtUnit.Text) ? null : txtUnit.Text
            };

            if (selectedType == "slider")
            {
                if (!string.IsNullOrEmpty(txtMinFloat.Text) && txtMinFloat.Text != "0.0")
                {
                    newItem.MinFloat = double.Parse(txtMinFloat.Text);
                    newItem.MaxFloat = double.Parse(txtMaxFloat.Text);
                    newItem.DefaultFloat = double.Parse(txtDefaultFloat.Text);
                    newItem.Step = double.Parse(txtStep.Text);
                    newItem.Decimals = int.Parse(txtDecimals.Text);
                }
                else
                {
                    newItem.Min = int.Parse(txtMin.Text);
                    newItem.Max = int.Parse(txtMax.Text);
                    newItem.DefaultInt = int.Parse(txtDefaultInt.Text);
                }
            }
            else if (selectedType == "number")
            {
                newItem.MinFloat = double.Parse(txtMinFloat.Text);
                newItem.MaxFloat = double.Parse(txtMaxFloat.Text);
                newItem.DefaultFloat = double.Parse(txtDefaultFloat.Text);
            }
            else if (selectedType == "toggle")
            {
                newItem.Default = false;
            }
            else if (selectedType == "button")
            {
                newItem.ButtonText = string.IsNullOrEmpty(txtCustomText.Text) ? "Click" : txtCustomText.Text;
            }
            else if (selectedType == "input")
            {
                newItem.DefaultText = string.IsNullOrEmpty(txtCustomText.Text) ? "" : txtCustomText.Text;
            }
            else if (selectedType == "color")
            {
                newItem.DefaultColor = "#FF0000";
            }
            else if (selectedType == "sync")
            {
                newItem.Variable = txtVariable.Text;
            }
            else if (selectedType == "select")
            {
                newItem.DefaultOption = txtDefaultOption.Text;
                newItem.Options = new List<DropdownOption>();
                string[] rawOptions = txtDropdownOptions.Text.Split(',');
                foreach (var raw in rawOptions)
                {
                    var parts = raw.Split(':');
                    if (parts.Length == 2)
                    {
                        newItem.Options.Add(new DropdownOption { Label = parts[0].Trim(), Value = parts[1].Trim() });
                    }
                }
            }

            return newItem;
        }

        // ساخت نمونه MonitorItem از روی اطلاعات کنونی فرم چپ
        private MonitorItem CreateMonitorItemFromForm()
        {
            string selectedType = cmbType.SelectedItem.ToString();
            var newItem = new MonitorItem
            {
                Type = selectedType,
                Label = txtLabel.Text,
                Variable = txtVariable.Text,
                Unit = string.IsNullOrWhiteSpace(txtUnit.Text) ? null : txtUnit.Text
            };

            if (selectedType == "led")
            {
                newItem.Names = new Dictionary<string, string>();
                string[] rawAlarms = txtAlarmsMap.Text.Split(',');
                foreach (var raw in rawAlarms)
                {
                    var parts = raw.Split(':');
                    if (parts.Length == 2)
                    {
                        newItem.Names[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            else if (selectedType == "alarm")
            {
                newItem.Alarms = new Dictionary<string, string>();
                string[] rawAlarms = txtAlarmsMap.Text.Split(',');
                foreach (var raw in rawAlarms)
                {
                    var parts = raw.Split(':');
                    if (parts.Length == 2)
                    {
                        newItem.Alarms[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            else if (selectedType == "table")
            {
                newItem.Columns = new List<string>();
                string[] rawCols = txtTableColumns.Text.Split(',');
                foreach (var col in rawCols)
                {
                    if (!string.IsNullOrWhiteSpace(col))
                        newItem.Columns.Add(col.Trim());
                }
            }

            return newItem;
        }

        // دکمه ثبت فرم (مدیریت دوگانه افزودن جدید / به‌روزرسانی ردیف ادیت شده)
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbType.SelectedItem == null) return;

                if (_isEditMode)
                {
                    // در حالت ویرایش: ردیف فعلی را به‌روزرسانی و جایگزین می‌کنیم
                    if (_editingItem is ControlItem oldCtrl)
                    {
                        int index = controlList.IndexOf(oldCtrl);
                        if (index >= 0)
                        {
                            controlList[index] = CreateControlItemFromForm();
                        }
                    }
                    else if (_editingItem is MonitorItem oldMon)
                    {
                        int index = monitorList.IndexOf(oldMon);
                        if (index >= 0)
                        {
                            monitorList[index] = CreateMonitorItemFromForm();
                        }
                    }

                    ResetFormState();
                    MessageBox.Show("Item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // حالت عادی (افزودن آیتم جدید)
                    if (cmbCategory.SelectedIndex == 0) // Controls
                    {
                        controlList.Add(CreateControlItemFromForm());
                    }
                    else // Monitors
                    {
                        monitorList.Add(CreateMonitorItemFromForm());
                    }

                    // پاک کردن موقت مقادیر پر تکرار برای آیتم بعدی
                    txtLabel.Clear();
                    txtCommand.Clear();
                    txtVariable.Clear();
                    txtUnit.Clear();
                    txtCustomText.Clear();
                    txtDropdownOptions.Clear();
                    txtAlarmsMap.Clear();
                }

                // ریلود نماها
                dgControls.Items.Refresh();
                dgMonitors.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Field Input Parsing Error: {ex.Message}", "Parsing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgControls.SelectedItem != null)
            {
                controlList.Remove((ControlItem)dgControls.SelectedItem);
            }
            else if (dgMonitors.SelectedItem != null)
            {
                monitorList.Remove((MonitorItem)dgMonitors.SelectedItem);
            }
        }

        private void BtnSaveJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var finalConfig = new RootConfig();
                foreach (var item in controlList) finalConfig.Controls.Add(item);
                foreach (var item in monitorList) finalConfig.Monitors.Add(item);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string jsonOutput = JsonSerializer.Serialize(finalConfig, options);

                // مقدار دهی اولیه آدرس پوشه Configs برای ذخیره
                string selectedFile = cmbConfigFiles.SelectedItem?.ToString() ?? "device_config.json";

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    InitialDirectory = _configsFolder,
                    Filter = "JSON Files (*.json)|*.json",
                    FileName = selectedFile
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, jsonOutput);
                    MessageBox.Show("Configuration successfully saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshConfigFilesList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // متدی برای بردن فرم به حالت ویرایش و لود کردن دیتا
        private void LoadItemToForm(object selectedItem)
        {
            if (selectedItem == null) return;

            _editingItem = selectedItem;
            _isEditMode = true;

            // تغییر وضعیت ظاهر دکمه‌ها و هدر فرم
            btnSubmit.Content = "Update Selected Item";
            btnCancelEdit.Visibility = Visibility.Visible;
            lblFormTitle.Text = "Edit Interface Item";
            lblFormSubtitle.Text = "Modify the active parameters below";

            if (selectedItem is ControlItem ctrl)
            {
                cmbCategory.SelectedIndex = 0; // Control Widget
                cmbType.SelectedItem = ctrl.Type; // انتخاب نوع ویجت در کمبوباکس
                txtLabel.Text = ctrl.Label;
                txtCommand.Text = ctrl.Command;
                txtUnit.Text = ctrl.Unit;

                // پر کردن فیلدهای عددی
                txtMin.Text = ctrl.Min?.ToString() ?? "0";
                txtMax.Text = ctrl.Max?.ToString() ?? "100";
                txtDefaultInt.Text = ctrl.DefaultInt?.ToString() ?? "50";

                txtMinFloat.Text = ctrl.MinFloat?.ToString() ?? "0.0";
                txtMaxFloat.Text = ctrl.MaxFloat?.ToString() ?? "5.0";
                txtDefaultFloat.Text = ctrl.DefaultFloat?.ToString() ?? "3.3";
                txtStep.Text = ctrl.Step?.ToString() ?? "0.1";
                txtDecimals.Text = ctrl.Decimals?.ToString() ?? "1";

                txtCustomText.Text = ctrl.ButtonText ?? ctrl.DefaultText ?? "";

                // دیکشنری دراپ‌دان به فرمت متنی (با استفاده از Label و Value در مدل DropdownOption شما)
                if (ctrl.Options != null)
                {
                    txtDropdownOptions.Text = string.Join(", ", ctrl.Options.Select(x => $"{x.Label}:{x.Value}"));
                }
                else
                {
                    txtDropdownOptions.Text = "";
                }
                txtDefaultOption.Text = ctrl.DefaultOption ?? "";
            }
            else if (selectedItem is MonitorItem mon)
            {
                cmbCategory.SelectedIndex = 1; // Monitor Widget
                cmbType.SelectedItem = mon.Type;
                txtLabel.Text = mon.Label;
                txtVariable.Text = mon.Variable;
                txtUnit.Text = mon.Unit;

                // پر کردن فیلدهای اختصاصی مانیتورها
                if (mon.Columns != null)
                {
                    txtTableColumns.Text = string.Join(",", mon.Columns);
                }
                else
                {
                    txtTableColumns.Text = "";
                }

                // دیکشنری آلارم‌ها یا نام‌های LED
                var targetDict = mon.Type == "led" ? mon.Names : mon.Alarms;
                if (targetDict != null)
                {
                    txtAlarmsMap.Text = string.Join(", ", targetDict.Select(x => $"{x.Key}:{x.Value}"));
                }
                else
                {
                    txtAlarmsMap.Text = "";
                }
            }
        }

        // خروج از حالت ویرایش و برگشت به حالت افزودن جدید
        private void ResetFormState()
        {
            _editingItem = null;
            _isEditMode = false;

            btnSubmit.Content = "Add to Configuration";
            btnCancelEdit.Visibility = Visibility.Collapsed;
            lblFormTitle.Text = "Add Interface Item";
            lblFormSubtitle.Text = "Configure dynamic widgets below";

            // پاک کردن موقت تکست باکس‌ها
            txtLabel.Text = "";
            txtCommand.Text = "";
            txtVariable.Text = "";
            txtUnit.Text = "";
            txtCustomText.Text = "";
            txtDropdownOptions.Text = "";
            txtAlarmsMap.Text = "";
            txtTableColumns.Text = "";
        }

        // هندلرهای رویداد دبل کلیک روی ردیف‌ها و دکمه انصراف:
        private void DgControls_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LoadItemToForm(dgControls.SelectedItem);
        }

        private void DgMonitors_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LoadItemToForm(dgMonitors.SelectedItem);
        }

        private void BtnEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCategory.SelectedIndex == 0)
                LoadItemToForm(dgControls.SelectedItem);
            else
                LoadItemToForm(dgMonitors.SelectedItem);
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ResetFormState();
        }
    }
}
