/// <summary>
/// Shared virtual button state.
/// Written each frame by TouchControlsOverlay; read by PlayerController.
/// One-frame signals (jumpDown, jumpUp, dashDown) are cleared automatically
/// by TouchControlsOverlay.Update() after PlayerController has consumed them.
/// </summary>
public static class TouchInput
{
    public static bool moveLeft;
    public static bool moveRight;

    public static bool jumpHeld;   // true while Jump button is held
    public static bool jumpDown;   // true for one frame when Jump is pressed
    public static bool jumpUp;     // true for one frame when Jump is released

    public static bool dashDown;   // true for one frame when Dash is pressed
}
