using DoThis_Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DoThis_Client.Service
{
    public class TaskAndAddButtonTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TaskTemplate { get; set; }
        public DataTemplate AddTaskButtonTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TaskViewModel)
            {
                return TaskTemplate;
            }
            if (item is string)
            {
                return AddTaskButtonTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
