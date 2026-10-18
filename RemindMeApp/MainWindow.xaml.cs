
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Threading;
    using Forms = System.Windows.Forms;
    using Drawing = System.Drawing;

    namespace RemindMeApp
    {
        public partial class MainWindow : Window
        {
            private readonly DatabaseHelper _database;
            private readonly List<ReminderTask> _tasks = new();
            private readonly HashSet<int> _shownTaskIds = new();

            private readonly Forms.NotifyIcon _trayIcon;
            private readonly DispatcherTimer _scheduler;

            private bool _allowExit;

            public MainWindow()
            {
                InitializeComponent();

                _database = new DatabaseHelper();

                TaskDatePicker.SelectedDate = DateTime.Today;

                // Create the system tray icon.
                var trayMenu = new Forms.ContextMenuStrip();

                trayMenu.Items.Add("Open RemindMe", null, (_, _) =>
                {
                    ShowMainWindow();
                });

                trayMenu.Items.Add("Exit", null, (_, _) =>
                {
                    ExitApplication();
                });

                _trayIcon = new Forms.NotifyIcon
                {
                    Text = "RemindMe - Task Companion",
                    Icon = Drawing.SystemIcons.Application,
                    ContextMenuStrip = trayMenu,
                    Visible = true
                };

                _trayIcon.DoubleClick += (_, _) =>
                {
                    ShowMainWindow();
                };

                // Closing the window hides the app instead.
                Closing += MainWindow_Closing;

                // Load saved tasks and start the scheduler.
                LoadTasks();

                _scheduler = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };

                _scheduler.Tick += Scheduler_Tick;
                _scheduler.Start();
            }

            private void LoadTasks()
            {
                _tasks.Clear();
                _tasks.AddRange(_database.GetTasks());

                TasksListBox.Items.Clear();

                foreach (var task in _tasks)
                {
                    string status = task.IsCompleted
                        ? "Completed"
                        : "Pending";

                    TasksListBox.Items.Add(
                        $"{task.Title} | " +
                        $"{task.DueAt:dd MMM yyyy, hh:mm tt} | " +
                        status);
                }
            }

            private void SaveTaskButton_Click(
                object sender, RoutedEventArgs e)
            {
                string title = TaskTitleBox.Text.Trim();
                string description = TaskDescriptionBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(title))
                {
                System.Windows.MessageBox.Show(("Please enter a task title."));
                    TaskTitleBox.Focus();
                    return;
                }

                if (TaskDatePicker.SelectedDate is not DateTime date)
                {
                System.Windows.MessageBox.Show(("Please select a reminder date."));
                    return;
                }

                if (!TimeSpan.TryParseExact(
                        TaskTimeBox.Text.Trim(),
                        new[] { @"hh\:mm", @"h\:mm" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        out TimeSpan time)
                    || time < TimeSpan.Zero
                    || time >= TimeSpan.FromDays(1))
                {
                System.Windows.MessageBox.Show((
                        "Enter a valid time, such as 15:00."));
                    TaskTimeBox.Focus();
                    return;
                }

                DateTime dueAt = date.Date.Add(time);

                if (dueAt <= DateTime.Now)
                {
                System.Windows.MessageBox.Show((
                        "Please select a future date and time."));
                    return;
                }

                var task = new ReminderTask
                {
                    Title = title,
                    Description = description,
                    DueAt = dueAt,
                    IsCompleted = false
                };

                try
                {
                    _database.SaveTask(task);
                    LoadTasks();
                    ClearForm();

                System.Windows.MessageBox.Show(("Task saved successfully!"));
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Could not save task:\n{ex.Message}");
                }
            }

            private void ClearButton_Click(
                object sender, RoutedEventArgs e)
            {
                ClearForm();
            }

            private void ClearForm()
            {
                TaskTitleBox.Clear();
                TaskDescriptionBox.Clear();
                TaskDatePicker.SelectedDate = DateTime.Today;
                TaskTimeBox.Text = "15:00";
                TaskTitleBox.Focus();
            }

            // Check for due tasks every five seconds.
            private void Scheduler_Tick(
                object? sender, EventArgs e)
            {
                try
                {
                    // Refresh from SQLite so the scheduler uses
                    // the latest task times and completion states.
                    _tasks.Clear();
                    _tasks.AddRange(_database.GetTasks());

                    foreach (var task in _tasks.ToList())
                    {
                        if (task.IsCompleted)
                            continue;

                        if (task.DueAt > DateTime.Now)
                            continue;

                        if (!_shownTaskIds.Add(task.Id))
                            continue;

                        try
                        {
                            var reminder = new ReminderWindow(
                                task,
                                MarkTaskDone,
                                SnoozeTask);

                            reminder.Show();
                        }
                        catch
                        {
                            _shownTaskIds.Remove(task.Id);
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _scheduler.Stop();

                    System.Windows.MessageBox.Show(
                        $"The reminder scheduler stopped:\n{ex.Message}\n\n" +
                        "Please restart RemindMe.",
                        "Reminder Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            private void MarkTaskDone(int taskId)
            {
                _database.MarkTaskCompleted(taskId);
                LoadTasks();
            }

            private void SnoozeTask(int taskId)
            {
                DateTime newDueAt = DateTime.Now.AddMinutes(5);

                _database.UpdateTaskDueAt(taskId, newDueAt);

                // Allow this task to trigger again at its new time.
                _shownTaskIds.Remove(taskId);

                LoadTasks();
            }

            // Clicking X hides the window without exiting the app.
            private void MainWindow_Closing(
                object? sender, CancelEventArgs e)
            {
                if (!_allowExit)
                {
                    e.Cancel = true;
                    Hide();
                }
            }

            private void ShowMainWindow()
            {
                Show();

                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;

                Activate();
            }

            private void ExitApplication()
            {
                _allowExit = true;

                _scheduler.Stop();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();

                Close();
            }
        }

    public class ReminderTask
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DueAt { get; set; }

        public bool IsCompleted { get; set; }
    }
}


