using System.ComponentModel;
using System.Globalization;

namespace Vamoriq.Extensions
{

    [ContentProperty(nameof(Key))]
    public class LocalizeExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; } = string.Empty;

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding
            {
                Mode = BindingMode.OneWay,
                Path = $"[{Key}]",
                Source = LocalizationResourceManager.Instance
            };
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ProvideValue(serviceProvider);
        }
    }

    public class LocalizationResourceManager : INotifyPropertyChanged
    {
        private static LocalizationResourceManager? _instance;

        public static LocalizationResourceManager Instance => _instance ??= new LocalizationResourceManager();

        public event PropertyChangedEventHandler? PropertyChanged;

        private LocalizationResourceManager()
        {
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    return Resources.Localization.AppResources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
                }
                catch
                {
                    return key;
                }
            }
        }

        public void Invalidate()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public void SetCulture(CultureInfo culture)
        {
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;

            Invalidate();
        }
    }
}
