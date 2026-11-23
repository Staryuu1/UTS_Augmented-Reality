using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KopdesPlayer : MonoBehaviour
{
    public Camera view;
    public Light flashlight;
    public WorldBorder worldBorder;
    public float walkSpeed = 2.45f, runSpeed = 4.7f, sensitivity = 2f;
    CharacterController body;
    float pitch, fall;
    Vector3 spawn;
    void Start() { body = GetComponent<CharacterController>(); spawn = transform.position; Lock(true); }
    void Lock(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
    void OnDisable() { Lock(false); }
    void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Lock(false);
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) Lock(true);
        Vector2 input = Vector2.zero;
        bool run = false;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = mouse != null ? mouse.delta.ReadValue() * (sensitivity * .05f) : Vector2.zero;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -80, 80);
            if (view) view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            if (keyboard != null)
            {
                input = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                run = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                if (keyboard.fKey.wasPressedThisFrame && flashlight) flashlight.enabled = !flashlight.enabled;
            }
        }
        Move(Vector2.ClampMagnitude(input, 1), run, Time.deltaTime);
        if (transform.position.y < -8) { body.enabled = false; transform.position = spawn; body.enabled = true; fall = 0; }
    }
    void OnApplicationFocus(bool focused) { if (!focused) Lock(false); }
    public void Move(Vector2 input, bool run, float dt)
    {
        if (!body) body = GetComponent<CharacterController>();
        if (body.isGrounded && fall < 0) fall = -2;
        fall += -20 * dt;
        var motion = (transform.right * input.x + transform.forward * input.y) * (run ? runSpeed : walkSpeed);
        motion.y = fall;
        body.Move(motion * dt);
        if (worldBorder)
        {
            var contained = worldBorder.Contain(transform.position, body.radius + .05f);
            if (contained != transform.position) { body.enabled = false; transform.position = contained; body.enabled = true; }
        }
    }
    void OnGUI()
    {
        var small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        GUI.color = Color.white;
        GUI.Label(new Rect(28,Screen.height-39,700,30),"WASD  jalan     SHIFT  lari     MOUSE  lihat     F  senter     ESC  lepas kursor",small);
        if (worldBorder && worldBorder.NearEdge(transform.position)) GUI.Box(new Rect(Screen.width/2-100,Screen.height-80,200,30),"Batas area penjelajahan");
        if (Cursor.lockState != CursorLockMode.Locked) GUI.Box(new Rect(Screen.width/2-145,Screen.height/2-25,290,50),"Klik untuk menjelajah");
        else GUI.DrawTexture(new Rect(Screen.width/2-2,Screen.height/2-2,4,4),Texture2D.whiteTexture);
    }
}
