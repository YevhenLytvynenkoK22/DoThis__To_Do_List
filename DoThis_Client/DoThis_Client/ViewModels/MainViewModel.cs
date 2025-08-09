using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoThis_Client.Models;
using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.ObjectModel;
using System.Windows;
using Todo;
using Task = System.Threading.Tasks.Task;
namespace DoThis_Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private static string ServerIP = "26.225.145.107"; //Your local host IP (I use Radmin VPN for creating local netwokr)
        private static readonly GrpcChannel _channel = GrpcChannel.ForAddress($"http://{ServerIP}:50051");
        private readonly ToDoService.ToDoServiceClient _client = new ToDoService.ToDoServiceClient(_channel);
        public ObservableCollection<object> Columns { get; } = new ObservableCollection<object>();

        public MainViewModel()
        {

            LoadBoardAsync();
            SubscribeToSync();
        }

        [ObservableProperty]
        private string _newColumnName;


        [RelayCommand]
        private async Task AddColumnAsync()
        {
            NewColumnName = Microsoft.VisualBasic.Interaction.InputBox("Enter a new column name:", "Rename column");
            if (string.IsNullOrWhiteSpace(NewColumnName))
            {
                MessageBox.Show("The column name cannot be empty.");
                return;
            }

            try
            {
                var request = new AddColumnRequest { Name = NewColumnName };
                await _client.AddColumnAsync(request);
                NewColumnName = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding column: {ex.Message}");
            }
        }

        private async Task LoadBoardAsync()
        {
            try
            {
                var board = await _client.GetBoardAsync(new Empty());
                await Application.Current.Dispatcher.InvokeAsync(() => UpdateBoard(board));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading board: {ex.Message}");
            }
        }
        private async Task SubscribeToSync()
        {
            try
            {
                using (var call = _client.Sync(new Empty()))
                {
                    await foreach (var board in call.ResponseStream.ReadAllAsync())
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => UpdateBoard(board));
                    }
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { }
            catch (Exception ex)
            {
                MessageBox.Show($"Synchronization error: {ex.Message}");
            }
        }
        private void UpdateBoard(Board board)
        {
            Columns.Clear();

            foreach (var column in board.Columns)
            {
                var columnViewModel = new ColumnViewModel(_client, this, column.Id, column.Name);
                foreach (var task in column.Tasks)
                {
                    columnViewModel.Tasks.Add(new TaskViewModel(_client, columnViewModel, task.Id, task.Title, task.Description, task.State));
                }
                Columns.Add(columnViewModel);

            }
            Columns.Add("Add Column");
        }

        [RelayCommand]
        private async Task MoveTaskAsync(MoveTaskRequest request)
        {
            try
            {
                await _client.MoveTaskAsync(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving task: {ex.Message}");
            }
        }

      
    }
}
