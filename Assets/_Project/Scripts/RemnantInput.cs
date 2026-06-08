using UnityEngine;

public static class RemnantInput
{
    private const float DeadZone = 0.25f;
    private static Vector3 lastMousePosition;
    private static bool hasMousePosition;

    public static float MoveHorizontal()
    {
        float keyboard = 0f;
        if (Input.GetKey(KeyCode.D)) keyboard += 1f;
        if (Input.GetKey(KeyCode.A)) keyboard -= 1f;

        float stick = GetAxis("MoveHorizontal");
        if (Mathf.Abs(stick) > DeadZone)
            return stick;

        return keyboard;
    }

    public static float MoveVertical()
    {
        float keyboard = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyboard += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyboard -= 1f;

        float stick = GetAxis("MoveVertical");
        if (Mathf.Abs(stick) > DeadZone)
            return stick;

        return keyboard;
    }

    public static Vector2 AimDirection(int facingDirection, Vector3 origin)
    {
        Vector2 keyboardAim = Vector2.zero;

        if (Input.GetKey(KeyCode.RightArrow)) keyboardAim.x += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) keyboardAim.x -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) keyboardAim.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) keyboardAim.y -= 1f;

        Vector2 stickAim = new Vector2(GetAxis("AimHorizontal"), GetAxis("AimVertical"));
        if (stickAim.sqrMagnitude > DeadZone * DeadZone)
            return stickAim.normalized;

        if (keyboardAim.sqrMagnitude > 0.01f)
            return keyboardAim.normalized;

        Vector2 mouseAim = MouseAimDirection(origin);
        if (mouseAim.sqrMagnitude > 0.01f)
            return mouseAim;

        return new Vector2(facingDirection, 0f);
    }

    public static bool JumpDown()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    public static bool DashDown()
    {
        return Input.GetKeyDown(KeyCode.LeftShift)
            || Input.GetKeyDown(KeyCode.JoystickButton4);
    }

    public static bool ShootHeld()
    {
        return Input.GetKey(KeyCode.F)
            || Input.GetKey(KeyCode.JoystickButton2)
            || Input.GetMouseButton(0);
    }

    public static bool BombDown()
    {
        return Input.GetKeyDown(KeyCode.R)
            || Input.GetKeyDown(KeyCode.JoystickButton1);
    }

    public static bool ReloadDown()
    {
        return Input.GetKeyDown(KeyCode.T)
            || Input.GetKeyDown(KeyCode.JoystickButton3);
    }

    public static bool CrouchHeld()
    {
        return Input.GetKey(KeyCode.S)
            || Input.GetKey(KeyCode.DownArrow);
    }

    public static bool MeleeDown()
    {
        return Input.GetKeyDown(KeyCode.C)
            || Input.GetKeyDown(KeyCode.JoystickButton6);
    }

    public static bool RestartDown()
    {
        return Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.JoystickButton7);
    }

    public static bool MenuConfirmDown()
    {
        return RestartDown()
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.JoystickButton0)
            || Input.GetMouseButtonDown(0);
    }

    public static bool InteractDown()
    {
        return Input.GetKeyDown(KeyCode.E)
            || Input.GetKeyDown(KeyCode.JoystickButton5);
    }

    private static float GetAxis(string axisName)
    {
        try
        {
            return Input.GetAxisRaw(axisName);
        }
        catch
        {
            return 0f;
        }
    }

    private static Vector2 MouseAimDirection(Vector3 origin)
    {
        if (Camera.main == null)
            return Vector2.zero;

        bool mouseMoved = !hasMousePosition || (Input.mousePosition - lastMousePosition).sqrMagnitude > 0.01f;
        bool mouseActive = mouseMoved || Input.GetMouseButton(0) || Input.GetMouseButton(1);
        lastMousePosition = Input.mousePosition;
        hasMousePosition = true;

        if (!mouseActive)
            return Vector2.zero;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mouseWorld - origin;

        if (direction.sqrMagnitude <= 0.01f)
            return Vector2.zero;

        return direction.normalized;
    }
}
