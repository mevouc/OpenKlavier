using Klavier.UI.Input.Mapping.Dto;

namespace Klavier.UI.Input.Mapping;

public interface IKeyboardMappingService
{
    event Action LayoutsChanged;

    string[] GetAvailableLayouts();
    KeyboardMapping Load(string layoutName);
    void Save(string name, KeyboardMappingDto dto);
    bool UserLayoutExists(string name);
}
