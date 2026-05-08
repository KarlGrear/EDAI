namespace EDAI.UI.Services;

public interface INavigationService
{
    void ShowSettings();
    void ShowEventConfigurations();
    void ShowEventConfigEdit(int? configId, Action? onClosed = null);
    void ShowCategoryManagement();
    void ShowTest(int? configId = null);
}
