using UnityEngine;

public class AedenthornUtils
{
    public static bool IgnoreKeyPresses()
    {
        return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || InventoryGui.IsVisible() || Console.IsVisible() || StoreGui.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true;
    }
    public static bool CheckKeyDown(string value)
    {
        try
        {
            return Input.GetKeyDown(value.ToLower());
        }
        catch
        {
            return false;
        }
    }
}
