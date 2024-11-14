using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Windows.Media;

namespace ToDoList
{
    public partial class MainWindow : Window
    {
        private List<TaskItem> tasks = new List<TaskItem>();
        private NotifyIcon trayIcon;
        private bool isEditing = false;
        private TaskItem taskToEdit = null;
        TaskDetail taskDetailWindow = new TaskDetail();


        public MainWindow()
        {
            InitializeComponent();
            tasks = new List<TaskItem>();
            LoadTasks();
        }

      
        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new instance of the TaskDetail window
            var taskDetailWindow = new TaskDetail();

            // Handle the TaskSaved event
            taskDetailWindow.TaskSaved += (task) =>
            {
                // Add the new task to the ObservableCollection
                TaskListBox.Items.Add(task);               
                SaveTasks();


            };
            

            // Show the TaskDetail window
            taskDetailWindow.ShowDialog();
        }



        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListBox.SelectedItem != null)
            {
                var result = System.Windows.MessageBox.Show("Are you sure you want to delete this task?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    tasks.Remove((TaskItem)TaskListBox.SelectedItem);
                    TaskListBox.Items.Remove(TaskListBox.SelectedItem);
                    SaveTasks();
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a task to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected task from the ListBox
            taskToEdit = (TaskItem)TaskListBox.SelectedItem;

            if (taskToEdit != null)
            {
                // Ensure the TaskDetail window is open, create a new one if necessary
                if (taskDetailWindow == null || !taskDetailWindow.IsLoaded)
                {
                    taskDetailWindow = new TaskDetail();  // Assuming TaskDetail is your window class
                }

                // Set the TaskToEdit property in TaskDetail to the selected task
                taskDetailWindow.TaskToEdit = taskToEdit;

                // Open the TaskDetail window and wait for it to close
                taskDetailWindow.ShowDialog();

                // After the task is saved/edited, update the ListBox or the task list
                if (taskDetailWindow.TaskToEdit != null) // If the task was edited and saved
                {
                    var index = TaskListBox.Items.IndexOf(taskToEdit); // Find the selected task in the ListBox
                    if (index >= 0)
                    {
                        TaskListBox.Items[index] = taskDetailWindow.TaskToEdit; // Update the task in the ListBox
                        TaskListBox.Items.Refresh();
                        SaveTasks(); // Save tasks to file
                    }
                }
            }
            else
            {
                // Show message if no task is selected
                System.Windows.MessageBox.Show("Please select a task to edit.");
            }
        }


        private void SetReminderButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListBox.SelectedItem != null)
            {
                var task = (TaskItem)TaskListBox.SelectedItem;
                if (task.DueDate.HasValue)
                {
                    var reminderTime = task.DueDate.Value.AddMinutes(-5); // Set reminder 5 minutes before the due date.

                    if (reminderTime > DateTime.Now)
                    {
                        var reminderTask = Task.Delay(reminderTime - DateTime.Now);
                        reminderTask.ContinueWith(_ => ShowReminder(task), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Reminder time has already passed.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a task to set a reminder.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowReminder(TaskItem task)
        {
            // Phát âm thanh thông báo
            System.Media.SoundPlayer player = new System.Media.SoundPlayer("Tieng-Ga-gay.wav");
            player.Play();


            // Hiển thị thông báo nhắc nhở
            System.Windows.MessageBox.Show($"Reminder: {task.Description} is due soon!", "Task Reminder", MessageBoxButton.OK, MessageBoxImage.Information);
            Debug.WriteLine($"Reminder triggered for task: {task.Description} at {DateTime.Now}");
        }

        private void SaveTasks()
        {
            var taskDescriptions = TaskListBox.Items.Cast<TaskItem>()
                                                   .Select(t => $"{t.Description},{t.Priority},{t.DueDate},{t.IsCompleted},{t.Status}");

            // Save tasks to file
            File.WriteAllLines("tasks.txt", taskDescriptions);
        }

        private void LoadTasks()
        {
            // Clear the ListBox and tasks list before loading tasks
            TaskListBox.Items.Clear();
            //tasks.Clear();

            if (File.Exists("tasks.txt"))
            {
                try
                {
                    var taskDescriptions = File.ReadAllLines("tasks.txt");
                    foreach (var line in taskDescriptions)
                    {
                        // Skip empty lines
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = line.Split(',');

                        if (parts.Length == 5)
                        {
                            try
                            {
                                var task = new TaskItem
                                {
                                    Description = parts[0],
                                    Priority = (Priority)Enum.Parse(typeof(Priority), parts[1]),
                                    DueDate = DateTime.TryParse(parts[2], out var dueDate) ? (DateTime?)dueDate : null,
                                    IsCompleted = bool.Parse(parts[3]),
                                    Status = (TaskStatus)Enum.Parse(typeof(TaskStatus), parts[4]) // Parse the Status field
                                };

                                // Add task to both the ListBox and the tasks list
                                TaskListBox.Items.Add(task);
                                tasks.Add(task);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show($"Error loading task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error reading tasks file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            trayIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("icon.ico"),
                Visible = true
            };

            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            // Tạo một menu chuột phải
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            // Tạo mục "Hiện cửa sổ"
            var showMenuItem = new System.Windows.Forms.ToolStripMenuItem("Hiện cửa sổ");
            showMenuItem.Click += (s, args) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            contextMenu.Items.Add(showMenuItem);

            // Tạo mục "Thoát"
            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("Thoát");
            exitMenuItem.Click += (s, args) =>
            {
                System.Windows.Application.Current.Shutdown();  // Thoát ứng dụng và dừng hệ thống
            };
            contextMenu.Items.Add(exitMenuItem);

            // Gắn menu chuột phải vào tray icon
            trayIcon.ContextMenuStrip = contextMenu;
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        protected override void OnClosed(EventArgs e)
        {
            trayIcon.Dispose();
            base.OnClosed(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Prevent the window from closing
            this.Hide(); // Hide the window instead
        }


        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchQuery = SearchTextBox.Text.ToLower(); // Lấy từ khóa tìm kiếm và chuyển thành chữ thường

            // Lọc danh sách task dựa trên tất cả các thuộc tính của task
            var filteredTasks = tasks.Where(task =>
                task.Description.ToLower().Contains(searchQuery) ||
                task.Priority.ToString().ToLower().Contains(searchQuery) ||
                (task.DueDate.HasValue && task.DueDate.Value.ToString("MM/dd/yyyy hh:mm tt").ToLower().Contains(searchQuery)) ||
                task.IsCompleted.ToString().ToLower().Contains(searchQuery) ||
                task.Status.ToString().ToLower().Contains(searchQuery)
            ).ToList();

            // Cập nhật lại ListBox với danh sách task đã lọc
            RefreshTaskList(filteredTasks);
           
        }


        private void RefreshTaskList(List<TaskItem> taskList = null)
        {
            // Clear current items in ListBox
            TaskListBox.Items.Clear();

            // If a filtered task list is provided, use that; otherwise, use the full task list
            var tasksToDisplay = taskList ?? tasks;

            // Add each task to the ListBox
            foreach (var task in tasksToDisplay)
            {
                // Create a string describing the task
                string taskDetails = $"{task.Description} - {task.Priority} -  {task.DueDate?.ToString("MM/dd/yyyy hh:mm tt") ?? "N/A"} -  {task.Status}";

                // Create a ListBoxItem with the task details
                var listItem = new ListBoxItem
                {
                    Content = taskDetails
                };

                // Add the item to the ListBox
                TaskListBox.Items.Add(listItem);
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? filter = ((ComboBoxItem)FilterComboBox.SelectedItem).Content.ToString();
            List<TaskItem> filteredTasks;

            switch (filter)
            {
                case "Completed":
                    filteredTasks = tasks.Where(task =>task.Status == TaskStatus.Completed).ToList();
                    break;
                case "In Progress":
                    filteredTasks = tasks.Where(task => !task.IsCompleted && task.Status == TaskStatus.InProgress).ToList();
                    break;
                case "Cancelled":
                    filteredTasks = tasks.Where(task => task.Status == TaskStatus.Cancelled).ToList();
                    break;
                default:
                    filteredTasks = tasks.ToList();
                    break;
            }

            // Cập nhật lại ListBox với các task đã lọc
            RefreshTaskList(filteredTasks);
        }

        private TaskStatus GetEnumFromDisplayName(string displayName)
        {
            switch (displayName)
            {
                case "In Progress":
                    return TaskStatus.InProgress;
                case "Completed":
                    return TaskStatus.Completed;
                case "Cancelled":
                    return TaskStatus.Cancelled;
                default:
                    throw new ArgumentException("Invalid display name");
            }
        }

        //private void UpdateStatusButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (TaskListBox.SelectedItem == null)
        //    {
        //        System.Windows.MessageBox.Show("Please select a task to update its status.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return;
        //    }

        //    var task = (TaskItem)TaskListBox.SelectedItem;
        //    var selectedStatusString = ((ComboBoxItem)StatusComboBox.SelectedItem)?.Content.ToString();

        //    try
        //    {

        //        TaskStatus selectedStatus = GetEnumFromDisplayName(selectedStatusString);
        //        task.Status = selectedStatus;
        //        task.IsCompleted = selectedStatus == TaskStatus.Completed;


        //        RefreshTaskList();


        //        SaveTasks();
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //    }
        //}

        private void ExportTasksButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ask the user to choose the status filter (this can be extended with a UI element)
                var statusFilter = System.Windows.MessageBox.Show("Do you want to filter by status? Click 'Yes' to filter, 'No' to export all.",
                                                    "Filter Tasks", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (statusFilter == MessageBoxResult.Cancel)
                {           
                    return;
                }

                // If the user clicks 'No', export all tasks without filtering
                if (statusFilter == MessageBoxResult.No)
                {
                    ExportTasksToJson(tasks); // Call a method to export without filtering
                    return;
                }

                // If the user clicks 'Yes', ask for status filter
                var status = System.Windows.MessageBox.Show("Select the status filter:\nYes. InProgress\nNo. Completed\nCancel. Cancelled",
                                                  "Choose Status", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                // If the user cancels, exit the method without doing anything
                if (status == MessageBoxResult.Cancel)
                {
                    return;
                }

                // Filter tasks based on the selected status
                IEnumerable<TaskItem> filteredTasks;
                switch (status)
                {
                    case MessageBoxResult.Yes:
                        filteredTasks = tasks.Where(t => t.Status == TaskStatus.InProgress);
                        break;
                    case MessageBoxResult.No:
                        filteredTasks = tasks.Where(t => t.Status == TaskStatus.Completed);
                        break;
                    case MessageBoxResult.Cancel:
                        filteredTasks = tasks.Where(t => t.Status == TaskStatus.Cancelled);
                        break;
                    default:
                        filteredTasks = tasks;
                        break;
                }

                // Export the filtered tasks to JSON
                ExportTasksToJson(filteredTasks);
            }
            catch (Exception ex)
            {
                // Handle any errors during export
                System.Windows.MessageBox.Show($"Error exporting tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Method to handle the JSON export logic
        private void ExportTasksToJson(IEnumerable<TaskItem> tasksToExport)
        {
            // Create a SaveFileDialog to specify the file path for exporting
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "tasks.json"
            };

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Serialize with options to ensure special characters are handled
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // For better readability
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Supports special characters
                };

                // Serialize the filtered tasks to JSON
                string json = JsonSerializer.Serialize(tasksToExport, options);

                // Write the serialized JSON to the file with UTF-8 encoding
                File.WriteAllText(saveFileDialog.FileName, json, Encoding.UTF8);

                // Notify the user of success
                System.Windows.MessageBox.Show("Tasks exported successfully!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }



        private void ExportTasksToCsvButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ask the user to choose the status filter (this can be extended with a UI element)
                var statusFilter = System.Windows.MessageBox.Show("Do you want to filter by status? Click 'Yes' to filter, 'No' to export all.",
                                                   "Filter Tasks", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (statusFilter == MessageBoxResult.Cancel)
                {
                    return;
                }

                // Filter tasks based on the user's choice
                IEnumerable<TaskItem> filteredTasks;

                if (statusFilter == MessageBoxResult.Yes)
                {
                    // You can replace this with a ComboBox or another method to let the user select the status
                    var status = System.Windows.MessageBox.Show("Select the status filter:\nYes. InProgress\nNo. Completed\nCancel. Cancelled",
                                                 "Choose Status", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    switch (status)
                    {
                        case MessageBoxResult.Yes:
                            filteredTasks = tasks.Where(t => t.Status == TaskStatus.InProgress);
                            break;
                        case MessageBoxResult.No:
                            filteredTasks = tasks.Where(t => t.Status == TaskStatus.Completed);
                            break;
                        case MessageBoxResult.Cancel:
                            filteredTasks = tasks.Where(t => t.Status == TaskStatus.Cancelled);
                            break;
                        default:
                            filteredTasks = tasks;
                            break;
                    }
                }
                else
                {
                    // If user doesn't want to filter, export all tasks
                    filteredTasks = tasks;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = "tasks.csv"
                };

                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Add the header row
                    var csvLines = new List<string>
            {
                "Description,Priority,DueDate,IsCompleted,Status"
            };

                    // Add the filtered tasks to the CSV
                    csvLines.AddRange(filteredTasks.Select(task =>
                        $"{task.Description},{task.Priority},{task.DueDate?.ToString("dd-MM-yyyy hh:mm:ss tt")},{task.IsCompleted},{task.Status}"
                    ));

                    // Write to the file with UTF-8 encoding to support special characters (e.g., Vietnamese)
                    File.WriteAllLines(saveFileDialog.FileName, csvLines, Encoding.UTF8);

                    // Notify the user of success
                    System.Windows.MessageBox.Show("Tasks exported to CSV successfully!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // Handle any errors during export
                System.Windows.MessageBox.Show($"Error exporting tasks to CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ImportTasksToCsvButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create an OpenFileDialog to select a file for importing
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv"
                };

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var lines = File.ReadAllLines(openFileDialog.FileName);
                    List<TaskItem> importedTasks = new List<TaskItem>();

                    // Skip the header row if it exists
                    bool isHeader = true;
                    foreach (var line in lines)
                    {
                        // Skip the first line if it's a header or skip any empty lines
                        if (isHeader || string.IsNullOrWhiteSpace(line))
                        {
                            isHeader = false;
                            continue;
                        }

                        var parts = line.Split(',');

                        if (parts.Length == 5)
                        {
                            try
                            {
                                var task = new TaskItem
                                {
                                    Description = parts[0],
                                    Priority = Enum.TryParse(parts[1], out Priority priority) ? priority : Priority.Low, // Default to Low if parsing fails
                                    DueDate = DateTime.TryParse(parts[2], out var dueDate) ? (DateTime?)dueDate : null,
                                    IsCompleted = bool.TryParse(parts[3], out var isCompleted) && isCompleted,
                                    Status = Enum.TryParse(parts[4], out TaskStatus status) ? status : TaskStatus.InProgress // Default to InProgress if parsing fails
                                };
                                importedTasks.Add(task);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show($"Error parsing CSV line: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }

                    // If tasks are imported, update the task list
                    if (importedTasks.Any())
                    {
                        tasks.Clear(); // Clear current tasks
                        tasks.AddRange(importedTasks); // Add imported tasks
                        TaskListBox.Items.Clear(); // Clear ListBox

                        foreach (var task in tasks)
                        {
                            TaskListBox.Items.Add(task); // Add tasks to ListBox
                        }

                        SaveTasks(); // Save tasks to file
                        System.Windows.MessageBox.Show("Tasks imported successfully from CSV!", "Import Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("No tasks found in the selected file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ImportJsonTasksButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create an OpenFileDialog to select a file for importing
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json"
                };

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string json = File.ReadAllText(openFileDialog.FileName);

                    // Configure JsonSerializer options to handle enums as strings
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }, // Add this line
                        PropertyNameCaseInsensitive = true // To ignore case when matching property names
                    };

                    var importedTasks = JsonSerializer.Deserialize<List<TaskItem>>(json, options);

                    if (importedTasks != null)
                    {
                        tasks.Clear(); // Clear current tasks
                        tasks.AddRange(importedTasks); // Add imported tasks
                        TaskListBox.Items.Clear(); // Clear ListBox

                        foreach (var task in tasks)
                        {
                            TaskListBox.Items.Add(task); // Add tasks to ListBox
                        }

                        SaveTasks(); // Save tasks to file
                        System.Windows.MessageBox.Show("Tasks imported successfully!", "Import Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("No tasks found in the selected file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the current tasks in the ListBox
            TaskListBox.Items.Clear();

            // Reload tasks from your data source (ObservableCollection or saved file)
            foreach (var task in tasks) // Assuming 'tasks' is your ObservableCollection<TaskItem>
            {
                TaskListBox.Items.Add(task);
            }
            
        }

    }
}