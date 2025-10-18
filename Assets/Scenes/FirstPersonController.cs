using UnityEngine;
using UnityEngine.UI; // Required for the Color Filter Image

public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Data Manager for the Feather Sense (Controls, Light, Sound).")]
    public FeatherDataManager featherDataManager;
    
    [Tooltip("The Data Manager for the Arduino Uno (Thermistor).")]
    public UnoThermistorReader unoThermistorReader;

    [Tooltip("The GameObject whose Transform controls the camera's X-axis (Pitch). Usually the Camera itself.")]
    public Transform cameraPitchTarget;
    [Tooltip("The GameObject whose Transform controls the camera's Y-axis (Yaw). Usually the player/parent root.")]
    public Transform playerYawTarget;
    
    [Tooltip("The Light component used as the Flashlight.")]
    public Light flashlight;
    
    [Tooltip("The main Directional Light (Simulates world sun/sky light).")]
    public Light directionalLight;

    [Tooltip("A full-screen UI Image component (e.g., a Canvas Panel) for the temp filter overlay.")]
    public Image colorFilterImage;

    [Header("Control Settings")]
    public float rotationSensitivity = 0.4f;
    public float pitchLimit = 80f; // Max rotation up/down

    public int joystickDeadZone = 50;

    [Header("Sensor Interpretation")]
    [Tooltip("Below this value (0-1023) is considered 'Cold' (Blue Filter).")]
    public int coldThreshold = 20;
    [Tooltip("Above this value (0-1023) is considered 'Hot' (Red Filter).")]
    public int hotThreshold = 20;
    [Tooltip("Max intensity of the color filter (alpha).")]
    public float maxFilterAlpha = 0.5f;

    [Tooltip("Max sound value (0-1023) to scale the camera shake.")]
    public int maxSoundLevel = 100;
    [Tooltip("Max camera shake displacement.")]
    public float maxShakeAmount = 0.05f;
    
    [Header("Photoresistor Settings")]
    [Tooltip("Min intensity for the directional light when it's dark (Photoresistor high).")]
    public float darkWorldIntensity = 0.1f;
    [Tooltip("Max intensity for the directional light when it's bright (Photoresistor low).")]
    public float brightWorldIntensity = 1.0f;
    [Tooltip("Value (0-1023) where the world is fully dark.")]
    public int photoresistorDarkPoint = 160;
    [Tooltip("Value (0-1023) where the world is fully bright.")]
    public int photoresistorBrightPoint = 20;


    // Internal state for pitch rotation
    private float currentPitch = 0f; 

    // Used for camera shake
    private Vector3 initialCameraPosition;

    void Start()
    {
        if (cameraPitchTarget != null)
        {
            // Store the initial local position of the camera for shake effects
            initialCameraPosition = cameraPitchTarget.localPosition;
        }

        // Lock cursor for a proper first-person experience
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (featherDataManager == null || unoThermistorReader == null || playerYawTarget == null || cameraPitchTarget == null)
        {
            Debug.LogError("Controller references are not set in the Inspector! Ensure both Feather Data Manager and Uno Thermistor Reader are assigned.");
            return;
        }

        // 1. Joystick Input for Camera Control (from Feather)
        HandleCameraRotation(featherDataManager.xAxis, featherDataManager.yAxis);

        // 2. Flashlight Control (Held Down) (from Feather)
        HandleFlashlight(featherDataManager.buttonState);

        // 3. Thermistor Filter (from Uno)
        HandleTemperatureFilter(unoThermistorReader.tempValue);

        // 4. Photoresistor World Light (from Feather)
        HandleWorldLight(featherDataManager.lightValue);

        // 5. Sound Sensor Camera Shake (from Feather)
        HandleCameraShake(featherDataManager.soundValue);
    }

    private void HandleCameraRotation(int x, int y)
    {
        // NEW: Apply a dead zone check for X-axis
        float normX = 0f;
        if (Mathf.Abs(x - 512) > joystickDeadZone)
        {
            normX = (x - 512f) / 512f;
        }

        // NEW: Apply a dead zone check for Y-axis
        float normY = 0f;
        if (Mathf.Abs(y - 512) > joystickDeadZone)
        {
            normY = (y - 512f) / 512f;
        }

        // Only rotate if there is meaningful input outside the dead zone
        if (Mathf.Abs(normX) > 0f)
        {
            float yawRotation = normX * rotationSensitivity;
            playerYawTarget.Rotate(Vector3.up, yawRotation, Space.World);
        }

        if (Mathf.Abs(normY) > 0f)
        {
            float pitchRotation = -normY * rotationSensitivity;
            currentPitch += pitchRotation;
            currentPitch = Mathf.Clamp(currentPitch, -pitchLimit, pitchLimit);
            cameraPitchTarget.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
        }
    }

    private void HandleFlashlight(int buttonState)
    {
        if (flashlight == null) return;
        // Button state 0 means pressed
        flashlight.enabled = (buttonState == 0); 
    }

    private void HandleTemperatureFilter(int tempValue)
    {
        if (colorFilterImage == null) return;

        Color targetColor = Color.clear;
        float intensity = 0f;

        if (tempValue > hotThreshold)
        {
            targetColor = Color.red;
            intensity = Mathf.InverseLerp(hotThreshold, 1023, tempValue) * maxFilterAlpha;
        }
        else if (tempValue < coldThreshold)
        {
            targetColor = Color.blue;
            intensity = Mathf.InverseLerp(coldThreshold, 0, tempValue) * maxFilterAlpha;
        }

        if (intensity > 0)
        {
            colorFilterImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, intensity);
        }
        else
        {
            colorFilterImage.color = Color.clear;
        }
    }

    private void HandleWorldLight(int lightValue)
    {
        if (directionalLight == null) return;

        // Map sensor value to a [0, 1] range for interpolation
        float t = Mathf.InverseLerp(photoresistorBrightPoint, photoresistorDarkPoint, lightValue);
        
        // Lerp directional light intensity
        directionalLight.intensity = Mathf.Lerp(brightWorldIntensity, darkWorldIntensity, t);
    }
    
    private void HandleCameraShake(int soundValue)
    {
        if (cameraPitchTarget == null) return;

        // Scale sound value (0 to maxSoundLevel) to shake percentage (0 to 1)
        float shakeT = Mathf.InverseLerp(0, maxSoundLevel, soundValue);
        
        // Create a quick, randomized wobble based on time and shake magnitude
        float shakeOffset = Mathf.PerlinNoise(Time.time * 20f, Time.time * 20f) * 2f - 1f; 
        shakeOffset *= maxShakeAmount * shakeT;

        // Apply shake relative to the camera's initial local position
        cameraPitchTarget.localPosition = initialCameraPosition + new Vector3(
            shakeOffset * 1f,
            shakeOffset,
            shakeOffset * 1f
        );
    }
}