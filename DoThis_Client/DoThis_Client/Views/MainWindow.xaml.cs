using DoThis_Client.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DoThis_Client.ViewModels;
using Todo;

namespace DoThis_Client
{
    public partial class MainWindow : Window
    {
        private TaskViewModel _draggedTask;
        private Point _startPoint;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
        private void Task_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            var taskBorder = FindParent<Border>(e.OriginalSource as DependencyObject);
            if (taskBorder != null)
            {
                _draggedTask = taskBorder.DataContext as TaskViewModel;
            }
            else
            {
                _draggedTask = null;
            }
        }
        private void Task_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedTask != null)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DataObject dragData = new DataObject();
                    dragData.SetData("TodoTask", _draggedTask);
                    dragData.SetData("SourceColumnId", _draggedTask.ColumnId);

                    DragDrop.DoDragDrop(FindParent<ItemsControl>(e.OriginalSource as DependencyObject), dragData, DragDropEffects.Move);
                    _draggedTask = null;
                }
            }
        }
        private void Common_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("TodoTask"))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        private void Column_DragOver(object sender, DragEventArgs e) => Common_DragOver(sender, e);
        private void Task_DragOver(object sender, DragEventArgs e) => Common_DragOver(sender, e);
        private void Column_DragEnter(object sender, DragEventArgs e) => Common_DragOver(sender, e);
        private void Task_DragEnter(object sender, DragEventArgs e) => Common_DragOver(sender, e);
        private void Column_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("TodoTask") && e.Data.GetDataPresent("SourceColumnId"))
            {
                var droppedTask = e.Data.GetData("TodoTask") as TaskViewModel;
                var sourceColumnId = (int)e.Data.GetData("SourceColumnId");
                var targetColumn = (sender as FrameworkElement)?.DataContext as ColumnViewModel;

                if (droppedTask != null && targetColumn != null && sourceColumnId != targetColumn.Id)
                {
                    ((MainViewModel)DataContext).MoveTaskCommand.Execute(new MoveTaskRequest
                    {
                        TaskId = droppedTask.Id,
                        FromColumnId = sourceColumnId,
                        ToColumnId = targetColumn.Id
                    });
                }
            }
        }
        private void Task_Drop(object sender, DragEventArgs e)
        {
            var parentBorder = FindParent<Border>(sender as DependencyObject);
            if (parentBorder != null)
            {
                Column_Drop(parentBorder, e);
            }
        }
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            while (parentObject != null && !(parentObject is T))
            {
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }
            return parentObject as T;
        }

       
    }
   
}
