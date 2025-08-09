using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoThis_Client.ViewModels;
using DoThis_Client.Views;
using System.Collections.ObjectModel;
using System.Windows;
using Todo;
using Task = System.Threading.Tasks.Task;

namespace DoThis_Client.Models
{
    public partial class ColumnViewModel : ObservableObject
    {
        private readonly ToDoService.ToDoServiceClient _client;
        private readonly MainViewModel _mainViewModel;
        public int Id { get; set; }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _newTaskTitle;

        [ObservableProperty]
        private string _newTaskDescription;

        public ObservableCollection<object> Tasks { get; } = new ObservableCollection<object>();
        public ColumnViewModel(ToDoService.ToDoServiceClient client, MainViewModel mainViewModel, int id, string name)
        {
            _client = client;
            _mainViewModel = mainViewModel;
            Id = id;
            Name = name;
            Tasks.Add("Add Task");
        }
        public ColumnViewModel() {}

        public void AddTask(TaskViewModel task)
        {
            Tasks.Insert(Tasks.Count - 1, task);
        }
        [RelayCommand]
        private async Task AddTaskAsync()
        {
            var dialog = new NewNameDialog();
            if (dialog.ShowDialog() == true)
            {
                string newTaskTitle = dialog.NewTitle;
                try
                {
                    var request = new AddTaskToColumnRequest
                    {
                        ColumnId = Id,
                        Title = newTaskTitle,
                        Description = ""
                    };
                    await _client.AddTaskToColumnAsync(request);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding task: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task RenameColumnAsync()
        {
            var dialog = new NewNameDialog();
            if (dialog.ShowDialog() == true)
            {
                var newName = dialog.NewTitle;
                if (string.IsNullOrWhiteSpace(newName) || newName == Name)
                {
                    return;
                }

                try
                {
                    var request = new RenameColumnRequest
                    {
                        ColumnId = Id,
                        NewName = newName
                    };
                    await _client.RenameColumnAsync(request);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while renaming column: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteColumnAsync()
        {
          
            var result = MessageBox.Show(
                $"Are you sure you want to delete the column '{Name}'?",
                "Delete Column",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var request = new ColumnIdRequest { ColumnId = Id };
                await _client.DeleteColumnAsync(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting column: {ex.Message}");
            }
        }
    }
}
