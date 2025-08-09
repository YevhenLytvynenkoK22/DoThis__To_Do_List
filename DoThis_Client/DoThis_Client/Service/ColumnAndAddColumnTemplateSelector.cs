using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DoThis_Client.Models;

namespace DoThis_Client.Service
{
    public class ColumnAndAddColumnTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ColumnTemplate { get; set; }
        public DataTemplate AddColumnTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if( item is ColumnViewModel)
            {
                return ColumnTemplate;
            }
            if (item is string)
            {
               return AddColumnTemplate;             
            }
            return base.SelectTemplate(item, container);
        }
    }
}
