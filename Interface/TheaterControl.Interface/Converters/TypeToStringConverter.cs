using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheaterControl.Interface.Converters
{
    using System.Globalization;
    using System.Windows.Data;
    using TheaterControl.Interface.Helper;
    using TheaterControl.Interface.ViewModels;

    class TypeToStringConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((List<Type>)value)?.Select(x => x.FullName.Split('.').Last());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
