namespace Klavier.UI.Input.Mapping;

public static class LayoutNameValidator
{
    public static bool TryValidate(string? name, out string? reason)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            reason = "Name cannot be empty.";
            return false;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            reason = "Name contains invalid characters.";
            return false;
        }

        reason = null;
        return true;
    }
}
