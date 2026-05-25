using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
// اضافه شدن این ریفرنس برای دسترسی به متدهای سطل آشغال ویندوز
using Microsoft.VisualBasic.FileIO;

namespace SerialDebugPanel
{
    public partial class ProjectManagerWindow : Window
    {
        public class ProjectItem
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }

        public ObservableCollection<ProjectItem> Projects { get; set; } = new();
        public string SelectedProjectPath { get; private set; }

        // هندل کردن مستقیم دستور حذف از داخل ListBox دیتاتمپلیت
        public RelayCommand<ProjectItem> DeleteCommand { get; set; }

        // اصلاح مسیر به پوشه Configs در ریشه اجرای برنامه
        private readonly string _profilesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");

        public ProjectManagerWindow()
        {
            InitializeComponent();
            DataContext = this;
            DeleteCommand = new RelayCommand<ProjectItem>(DeleteProject);

            // ایجاد پوشه در صورتی که وجود نداشته باشد
            if (!Directory.Exists(_profilesFolder))
            {
                Directory.CreateDirectory(_profilesFolder);
            }

            LoadProfilesList();
        }

        private void LoadProfilesList()
        {
            Projects.Clear();
            if (Directory.Exists(_profilesFolder))
            {
                var files = Directory.GetFiles(_profilesFolder, "*.json");
                foreach (var file in files)
                {
                    Projects.Add(new ProjectItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file
                    });
                }
            }
            ProjectsListBox.ItemsSource = Projects;
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListBox.SelectedItem is ProjectItem selected)
            {
                SelectedProjectPath = selected.Path;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a project configuration first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ProjectsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoadBtn_Click(sender, e);
        }

        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Import Widget Configuration"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string targetPath = Path.Combine(_profilesFolder, Path.GetFileName(openFileDialog.FileName));
                try
                {
                    File.Copy(openFileDialog.FileName, targetPath, overwrite: true);
                    LoadProfilesList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteProject(ProjectItem project)
        {
            if (project == null) return;

            var result = MessageBox.Show($"Are you sure you want to send profile '{project.Name}' to Recycle Bin?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (File.Exists(project.Path))
                    {
                        // استفاده از کتابخانه مایکروسافت برای انتقال امن فایل به سطل آشغال ویندوز (Recycle Bin)
                        FileSystem.DeleteFile(
                            project.Path,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin
                        );

                        Projects.Remove(project);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not move file to Recycle Bin: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LocateFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(_profilesFolder))
                {
                    // باز کردن مستقیم پوشه در اکسپلورر ویندوز با فوکوس روی آن
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _profilesFolder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    MessageBox.Show("Configs folder does not exist yet.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            // دکمه خروج تعبیه شده در هدر بالا به علت حذف هدر پیش‌فرض سیستم‌عامل
            this.DialogResult = false;
            this.Close();
        }

    }

    // اضافه کردن ردیف زیر در متدهای رویداد (Events) درون کلاس:




    // یک کلاس کمکی برای هندل کردن Command ها
    public class RelayCommand<T> : System.Windows.Input.ICommand
    {
        private readonly Action<T> _execute;
        public RelayCommand(Action<T> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute((T)parameter);
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }
}
