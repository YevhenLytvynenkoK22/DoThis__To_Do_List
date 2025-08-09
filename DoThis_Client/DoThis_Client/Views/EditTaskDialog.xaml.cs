using System.Windows;
using Todo;

namespace DoThis_Client
{
    public partial class EditTaskDialog : Window
    {
        public string NewTitle { get; private set; }
        public string NewDescription { get; private set; }
        public Todo.State NewState { get; private set; }

        public EditTaskDialog(string currentTitle, string currentDescription, Todo.State currentState)
        {
            InitializeComponent();
            TitleTextBox.Text = currentTitle;
            DescriptionTextBox.Text = currentDescription;
            NewState = currentState;
            StateToggleButton.IsChecked = (currentState == Todo.State.Completed);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            NewTitle = TitleTextBox.Text;
            NewDescription = DescriptionTextBox.Text;
            if (StateToggleButton.IsChecked.HasValue && StateToggleButton.IsChecked.Value)
            {
                NewState = Todo.State.Completed;
            }
            else
            {
                NewState = Todo.State.Pending;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}