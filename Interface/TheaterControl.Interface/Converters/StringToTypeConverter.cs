using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheaterControl.Interface.Converters
{
    using System.Globalization;
    using System.Windows.Data;
    using TheaterControl.Interface.ViewModels;

    class StringToTypeConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConfigurationViewModel.DeviceTypes.Find(x => x.FullName.EndsWith((string)value));
        }
    }
}
