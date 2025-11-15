using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusMux
{
    public partial class SliderTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double sliderValue)
            {
                TimeSpan time = TimeSpan.FromSeconds(sliderValue);
                if (time.Hours > 0)
                {
                    return time.ToString(@"hh\:mm\:ss");
                }

                return time.ToString(@"mm\:ss");
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
