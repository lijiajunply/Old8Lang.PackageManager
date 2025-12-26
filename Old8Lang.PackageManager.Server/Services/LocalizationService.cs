using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 本地化服务接口
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// 获取带参数的本地化字符串
    /// </summary>
    string GetString(string key, params object[] arguments);

    /// <summary>
    /// 设置当前文化
    /// </summary>
    void SetCulture(string cultureName);

    /// <summary>
    /// 获取当前文化
    /// </summary>
    CultureInfo GetCurrentCulture();
}

/// <summary>
/// 本地化服务实现
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly IStringLocalizer _localizer;
    private CultureInfo _currentCulture;

    public LocalizationService(IStringLocalizerFactory factory)
    {
        var type = typeof(Resources.Messages);
        _localizer = factory.Create("Messages", type.Assembly.GetName().Name!);
        _currentCulture = CultureInfo.CurrentCulture;
    }

    public string GetString(string key)
    {
        return _localizer[key].Value;
    }

    public string GetString(string key, params object[] arguments)
    {
        return string.Format(_localizer[key].Value, arguments);
    }

    public void SetCulture(string cultureName)
    {
        _currentCulture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = _currentCulture;
        CultureInfo.CurrentUICulture = _currentCulture;
    }

    public CultureInfo GetCurrentCulture()
    {
        return _currentCulture;
    }
}
