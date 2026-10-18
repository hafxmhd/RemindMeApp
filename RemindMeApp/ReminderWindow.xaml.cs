
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RemindMeApp
{
    public partial class ReminderWindow : Window
    {
        private readonly ReminderTask _task;
        private readonly Action<int> _markDone;
        private readonly Action<int> _snooze;

        public ReminderWindow(
            ReminderTask task,
            Action<int> markDone,
            Action<int> snooze)
        {
            InitializeComponent();

            _task = task;
            _markDone = markDone;
            _snooze = snooze;

            TaskTitleText.Text = task.Title;
            TaskDescriptionText.Text = task.Description;
        }

        private void DoneButton_Click(
            object sender, RoutedEventArgs e)
        {
            _markDone(_task.Id);
            Close();
        }

        private void SnoozeButton_Click(
            object sender, RoutedEventArgs e)
        {
            _snooze(_task.Id);
            Close();
        }
        private void ReminderWindow_Loaded(
    object sender, RoutedEventArgs e)
    {
        var slide = new TranslateTransform(0, 35);
        RenderTransform = slide;
        Opacity = 0;

        var fade = new DoubleAnimation(
            0, 1, TimeSpan.FromMilliseconds(450));

        var movement = new DoubleAnimation(
            35, 0, TimeSpan.FromMilliseconds(450));

        Opacity = 0;
        BeginAnimation(Window.OpacityProperty, fade);

        slide.BeginAnimation(
            TranslateTransform.YProperty, movement);
    }

}
}
