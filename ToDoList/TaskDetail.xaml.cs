using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ToDoList
{
    public partial class TaskDetail : Window
    {
        private List<TaskItem> tasks;
        public TaskItem NewTask { get; set; }
        public TaskItem TaskToEdit { get; set; }  // Class-level variable to store the task being edited
        public event Action<TaskItem> TaskSaved;

        public TaskDetail()
        {
            InitializeComponent();
            tasks = new List<TaskItem>();
        }

        private void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            // Create or edit task
            TaskItem task;
            if (TaskToEdit == null)
            {
                task = new TaskItem
                {
                    Description = NewTaskTextBox.Text,
                    Priority = (Priority)Enum.Parse(typeof(Priority), ((ComboBoxItem)PriorityComboBox.SelectedItem).Content.ToString()),
                    DueDate = DueDatePicker.Value,  // Use Value for DateTimePicker
                    Status = (TaskStatus)Enum.Parse(typeof(TaskStatus), ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString())
                };
            }
            else
            {
                task = TaskToEdit;
                task.Description = NewTaskTextBox.Text;
                task.Priority = (Priority)Enum.Parse(typeof(Priority), ((ComboBoxItem)PriorityComboBox.SelectedItem).Content.ToString());
                task.DueDate = DueDatePicker.Value;  // Use Value for DateTimePicker
                task.Status = (TaskStatus)Enum.Parse(typeof(TaskStatus), ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString());
            }

            // Notify MainWindow that the task is saved
            TaskSaved?.Invoke(task);

            DialogResult = true;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (TaskToEdit != null)
            {
                // Populate fields with TaskToEdit values
                NewTaskTextBox.Text = TaskToEdit.Description;

                // Set the selected item in the Priority ComboBox
                PriorityComboBox.SelectedItem = PriorityComboBox.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString() == TaskToEdit.Priority.ToString());

                // Set the selected date in the Due Date Picker
                DueDatePicker.Value = TaskToEdit.DueDate;  // Use Value for DateTimePicker

                // Set the selected item in the Status ComboBox
                StatusComboBox.SelectedItem = StatusComboBox.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString() == TaskToEdit.Status.ToString());
            }
        }

        private bool ValidateInputs()
        {
            // Check if NewTaskTextBox is empty
            string description = NewTaskTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(NewTaskTextBox.Text))
            {
                MessageBox.Show("Description must be required", "Failed required", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (description.Length < 5)
            {
                MessageBox.Show("Description must be at least 5 characters long", "Description too short", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            description = textInfo.ToTitleCase(description.ToLower());
            NewTaskTextBox.Text = description;

            // Check if description contains only letters and digits
            string[] words = description.Split(' ');
            foreach (string word in words)
            {
                if (!Regex.IsMatch(word, @"^[A-Za-z0-9\s]+$"))
                {
                    MessageBox.Show("Description can only contain letters and digits", "Invalid characters", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            // Check if PriorityComboBox has a selected item
            if (PriorityComboBox.SelectedItem == null)
            {
                MessageBox.Show("Priority must be required", "Failed required", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Check if DueDatePicker has a value
            if (DueDatePicker.Value == null)
            {
                MessageBox.Show("DueDate must be required", "Failed required", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (DueDatePicker.Value < DateTime.Now) // Check if the date is in the past
            {
                MessageBox.Show("Due date cannot be in the past.", "Invalid Due Date", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Check if StatusComboBox has a selected item
            if (StatusComboBox.SelectedItem == null)
            {
                MessageBox.Show("Status must be required", "Failed required", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
