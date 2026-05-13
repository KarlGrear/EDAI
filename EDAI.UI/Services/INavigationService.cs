namespace EDAI.UI.Services;

public interface INavigationService
{
    void ShowSettings(Action? onClosed = null);
    void ShowEventConfigurations();
    void ShowEventConfigEdit(int? configId, Action? onClosed = null);
    void ShowCategoryManagement(Action? onClosed = null);
    void ShowTest(int? configId = null);
    void ShowTheme();
    void ShowAbout();
    void ShowHistory();
}
