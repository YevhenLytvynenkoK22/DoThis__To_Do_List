using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Todo;
using Task = System.Threading.Tasks.Task;
namespace DoThis_Client.Models
{
    public partial class TaskViewModel : ObservableObject
    {
        private readonly ToDoService.ToDoServiceClient _client;
        private readonly ColumnViewModel _columnViewModel;

        public int Id { get; }
        public int ColumnId { get; set; }

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCompleted))]
        private State _state;

        public bool IsCompleted
        {
            get
            {
                return _state == Todo.State.Completed;
            }

            set
            {
                _state = value ? Todo.State.Completed : Todo.State.Pending;
            }
        }

        public TaskViewModel(ToDoService.ToDoServiceClient client, ColumnViewModel columnViewModel, int id, string title, string description, Todo.State state)
        {
            _client = client;
            _columnViewModel = columnViewModel;
            Id = id;
            Title = title;
            Description = description;
            _state = state;
            ColumnId = columnViewModel.Id;
        }

        [RelayCommand]
        private async Task EditTaskAsync()
        {
            var dialog = new EditTaskDialog(Title, Description, _state);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var request = new UpdateTaskRequest
                    {
                        TaskId = Id,
                        NewTitle = dialog.NewTitle,
                        NewDescription = dialog.NewDescription,
                        NewState = dialog.NewState
                    };
                    await _client.UpdateTaskAsync(request);
                    Title = dialog.NewTitle;
                    Description = dialog.NewDescription;
                    _state = dialog.NewState;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating task: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task ToggleTaskStateAsync()
        {
            try
            {
                var request = new ToggleTaskStateRequest { TaskId = Id };
                var updatedTask = await _client.ToggleTaskStateAsync(request);
                _state = updatedTask.State;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling task state: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DeleteTaskAsync()
        {
            try
            {
                var request = new TaskIdRequest { TaskId = Id };
                await _client.DeleteTaskAsync(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting task: {ex.Message}");
            }
        }
    }
}